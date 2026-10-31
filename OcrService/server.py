import os
os.environ["PADDLE_PDX_ENABLE_MKLDNN_BYDEFAULT"] = "False"
os.environ["PADDLE_PDX_USE_PIR_TRT"] = "False"
os.environ["PADDLE_PDX_DISABLE_MODEL_SOURCE_CHECK"] = "True"
os.environ["FLAGS_enable_pir_api"] = "0"
os.environ["FLAGS_use_mkldnn"] = "0"
os.environ["FLAGS_enable_pir_in_executor"] = "0"

import time
import json
import re
import tempfile
import cv2
import numpy as np
import uvicorn
import paddle
from fastapi import FastAPI, File, UploadFile, HTTPException, Form
from fastapi.responses import RedirectResponse
from fastapi.middleware.cors import CORSMiddleware

try:
    paddle.set_flags({"FLAGS_enable_pir_api": 0, "FLAGS_use_mkldnn": 0})
except Exception:
    pass

from preprocess import preprocess_pipeline, crop_roi, image_to_base64
import importlib
import cleaners
import inference
import labels
import sections
import pipeline
import parser
from parser import (
    parse_form_with_ocr, clean_text, get_confidence_rating, load_template,
    recognize_crop, get_ov_recognizer, get_v5_recognizer,
    clean_field_value, is_printed_label_word, measure_checkbox_ink,
    detect_employment_status
)

def reload_engine_modules():
    """Dynamically reloads engine modules so disk edits take effect without restarting the server."""
    try:
        importlib.reload(cleaners)
        importlib.reload(inference)
        importlib.reload(labels)
        importlib.reload(sections)
        importlib.reload(pipeline)
        importlib.reload(parser)
    except Exception as e:
        print(f"[HotReload Note] {e}")

app = FastAPI(title="Solace OCR Engine", version="2.0")

# Enable CORS for desktop app integration
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Initialize Ultra-Fast Lightweight CPU PaddleOCR & Native OpenVINO Recognition Engine
ocr = None
ocr_init_error = None
try:
    from paddleocr import PaddleOCR
    ocr = PaddleOCR(
        use_doc_orientation_classify=False,
        use_doc_unwarping=False,
        use_textline_orientation=False,
        ocr_version="PP-OCRv4",
        lang="en"
    )
    print("[OCR Server SUCCESS] Lightweight PP-OCRv4 Mobile engine loaded and ready (< 1.5s CPU speed)!")

    # Pre-warm native OpenVINO Static 2-Bucket Recognizer on startup (0 cold-start latency)
    try:
        _warmup_ov = get_ov_recognizer()
        if _warmup_ov:
            print("[OpenVINO Engine SUCCESS] Native OpenVINO models pre-compiled and ready in RAM!")
    except Exception as _ov_err:
        print(f"[OpenVINO Pre-Warm Note]: {_ov_err}")

    # Pre-warm PP-OCRv5 Latin RepSVTR Recognizer on startup (0 cold-start latency)
    try:
        _warmup_v5 = get_v5_recognizer()
        if _warmup_v5:
            _dummy = np.full((48, 320, 3), 255, dtype=np.uint8)
            list(_warmup_v5.predict([_dummy]))
            print("[PP-OCRv5 Engine SUCCESS] RepSVTR Latin model pre-warmed and ready in RAM!")
    except Exception as _v5_err:
        print(f"[PP-OCRv5 Pre-Warm Note]: {_v5_err}")
except Exception as e:
    import traceback
    ocr_init_error = str(e)
    print(f"[OCR Server FATAL ERROR] Engine initialization failed:")
    traceback.print_exc()

@app.get("/")
def root():
    return RedirectResponse(url="/docs")

@app.get("/health")
def health_check():
    return {
        "status": "online",
        "ocr_engine_loaded": ocr is not None,
        "ocr_init_error": ocr_init_error,
        "form_template": "CSWDO-066-2",
        "version": "2.0"
    }

@app.get("/template")
def get_template():
    return load_template()

@app.post("/template")
async def save_template(template_data: dict):
    try:
        template_path = os.path.join(os.path.dirname(__file__), "cswdo_template.json")
        with open(template_path, "w", encoding="utf-8") as f:
            json.dump(template_data, f, indent=4)
        return {"status": "success", "message": "Template updated successfully"}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

def sanitize_for_json(obj):
    """Recursively converts numpy primitives and arrays to standard Python types for JSON serialization."""
    if isinstance(obj, dict):
        return {str(k): sanitize_for_json(v) for k, v in obj.items()}
    elif isinstance(obj, (list, tuple)):
        return [sanitize_for_json(v) for v in obj]
    elif isinstance(obj, (bool, np.bool_)):
        return bool(obj)
    elif isinstance(obj, (int, np.integer)):
        return int(obj)
    elif isinstance(obj, (float, np.floating)):
        return float(obj)
    elif isinstance(obj, np.ndarray):
        return obj.tolist()
    return obj

@app.post("/extract-form")
def extract_form(file: UploadFile = File(...), mode: str = "global", include_preview: bool = True):
    """
    Full document ingestion pipeline:
    mode='global': Single full-page detection + spatial bucketing (Default)
    mode='crop': Legacy field-by-field crop loop (Fallback)
    include_preview: Whether to return Base64 image in JSON (default True for live UI parity)
    """
    reload_engine_modules()
    print(f"\n=======================================================")
    print(f"[Server] >>> New Document Ingestion Request Received: {file.filename} (mode: {mode})")
    
    if ocr is None:
        error_msg = f"PaddleOCR engine is not loaded on server. Initialization failure: {ocr_init_error}"
        print(f"[Server ERROR] {error_msg}")
        raise HTTPException(status_code=500, detail=error_msg)
        
    t_start = time.time()
    try:
        contents = file.file.read()
        print(f"[Server] 1. Read {len(contents)} bytes from upload.")
        
        deskewed_color, enhanced_gray, deskewed_full = preprocess_pipeline(contents, return_full=True)
        if deskewed_color is None or enhanced_gray is None:
            print(f"[Server ERROR] Failed to decode image format.")
            raise HTTPException(status_code=400, detail="Invalid image file or format.")
            
        print(f"[Server] 2. Preprocessed image. Shape: {enhanced_gray.shape} (H, W)")
        
        # Save deskewed preview to temp for exact coordinate and visual parity across Picture Preview and Adjustable Boxes
        temp_deskewed = os.path.join(tempfile.gettempdir(), "solace_last_deskewed_preview.jpg")
        try:
            cv2.imwrite(temp_deskewed, deskewed_color)
        except Exception:
            pass

        result = parse_form_with_ocr(ocr, deskewed_color, mode=mode, raw_image=deskewed_full)
        elapsed = round(time.time() - t_start, 2)
        result["execution_time_seconds"] = elapsed
        result["preview_image_path"] = temp_deskewed
        print(f"[Server] 3. OCR Extraction ({mode}) finished in {elapsed}s. Status: {result.get('status')}, Rating: {result.get('overall_rating')}")
        
        # Serialize Base64 image payload when requested
        if include_preview:
            result["preview_image"] = image_to_base64(deskewed_color)
        else:
            result["preview_image"] = ""
            
        print(f"[Server] 4. Returning JSON response to C# client.")
        print(f"=======================================================\n")
        return sanitize_for_json(result)
        
    except HTTPException:
        raise
    except Exception as e:
        import traceback
        print(f"[Server CRITICAL ERROR in /extract-form]:")
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))



@app.post("/extract-roi")
def extract_roi(
    file: UploadFile = File(...),
    x: float = Form(...),
    y: float = Form(...),
    w: float = Form(...),
    h: float = Form(...),
    field_key: str = Form("")
):
    """
    Real-time single-field re-extraction when a caseworker tests or repositions a bounding box.
    Uses DBNet text polygon detection inside the crop and applies the exact same domain
    cleaning/filters as full document processing.
    """
    reload_engine_modules()
    try:
        t_start = time.time()
        contents = file.file.read()
        _, enhanced_gray, deskewed_full = preprocess_pipeline(contents, return_full=True)
        
        if enhanced_gray is None:
            raise HTTPException(status_code=400, detail="Invalid image file.")
            
        bbox = [x, y, w, h]
        crop_src = deskewed_full if deskewed_full is not None else enhanced_gray
        crop = crop_roi(crop_src, bbox)
        if crop is None or crop.size == 0:
            return {
                "status": "success",
                "value": "",
                "raw_value": "",
                "confidence": 0.0,
                "rating": "Low",
                "bbox": bbox,
                "execution_time_seconds": 0.0
            }

        template = load_template()
        fields_config = template.get("fields", {})

        # Handle Employment Checkbox fields
        if field_key in ["is_employed", "is_self_employed", "is_not_employed"]:
            ink = measure_checkbox_ink(enhanced_gray, bbox)
            is_checked = ink >= 0.08
            cleaned = "True" if is_checked else "False"
            raw_value = f"Checkbox Ink: {ink:.1%}"
            final_conf = 0.95 if is_checked else 0.85
            elapsed = round(time.time() - t_start, 3)
            return sanitize_for_json({
                "status": "success",
                "value": cleaned,
                "raw_value": raw_value,
                "confidence": round(final_conf, 2),
                "rating": get_confidence_rating(final_conf),
                "bbox": bbox,
                "execution_time_seconds": elapsed
            })
        elif field_key.endswith("_checkbox"):
            ink = measure_checkbox_ink(enhanced_gray, bbox)
            is_checked = ink >= 0.045
            cleaned = "True" if is_checked else "False"
            raw_value = f"Checkbox Ink: {ink:.1%}"
            final_conf = 0.95 if is_checked else 0.85
            elapsed = round(time.time() - t_start, 3)
            return sanitize_for_json({
                "status": "success",
                "value": cleaned,
                "raw_value": raw_value,
                "confidence": round(final_conf, 2),
                "rating": get_confidence_rating(final_conf),
                "bbox": bbox,
                "execution_time_seconds": elapsed
            })

        # Handle Circumstance Code Box markings
        if field_key.startswith("circ_") and not any(sub in field_key for sub in ["_cause", "_date", "_disability", "_period", "_stay"]):
            gray_crop = cv2.cvtColor(crop, cv2.COLOR_BGR2GRAY) if len(crop.shape) == 3 else crop
            _, bin_img = cv2.threshold(gray_crop, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
            ink_ratio = float(np.count_nonzero(bin_img)) / float(bin_img.size)
            marked = ink_ratio > 0.04
            cleaned = "Marked" if marked else "Unmarked"
            raw_value = f"Circumstance Ink: {ink_ratio:.1%}"
            final_conf = min(0.95, round(0.70 + ink_ratio * 0.30, 2)) if marked else 0.80
            elapsed = round(time.time() - t_start, 3)
            return sanitize_for_json({
                "status": "success",
                "value": cleaned,
                "raw_value": raw_value,
                "confidence": round(final_conf, 2),
                "rating": get_confidence_rating(final_conf),
                "bbox": bbox,
                "execution_time_seconds": elapsed
            })

        # 1. Detection inside crop using DBNet (same model as full document processing)
        crop_bgr = cv2.cvtColor(crop, cv2.COLOR_GRAY2BGR) if len(crop.shape) == 2 else crop
        p = getattr(ocr, "paddlex_pipeline", None)
        pipeline_obj = getattr(p, "_pipeline", None) if p else None

        words = []
        confs = []
        tokens = []
        filtered_labels = []
        crop_h, crop_w = crop.shape[:2]

        if pipeline_obj and hasattr(pipeline_obj, "text_det_model"):
            try:
                pad_px = 25
                padded_bgr = cv2.copyMakeBorder(crop_bgr, pad_px, pad_px, pad_px, pad_px, cv2.BORDER_CONSTANT, value=[255, 255, 255])
                det_res = list(pipeline_obj.text_det_model.predict(padded_bgr))
                raw_polys = det_res[0].get("dt_polys", []) if det_res else []
                polys = []
                for poly in raw_polys:
                    if poly is not None and len(poly) >= 4:
                        pts = np.array(poly) - np.array([pad_px, pad_px])
                        polys.append(pts)
                boxes = []
                for poly in polys:
                    if poly is None or len(poly) < 4:
                        continue
                    pts = np.array(poly)
                    x1 = max(0, int(np.min(pts[:, 0])))
                    y1 = max(0, int(np.min(pts[:, 1])))
                    x2 = min(crop.shape[1], int(np.max(pts[:, 0])))
                    y2 = min(crop.shape[0], int(np.max(pts[:, 1])))
                    if x2 > x1 and y2 > y1:
                        boxes.append((x1, y1, x2, y2))
                
                # Sort left to right
                boxes.sort(key=lambda b: b[0])
                for x1, y1, x2, y2 in boxes:
                    word_crop = crop[y1:y2, x1:x2]
                    w_text, w_conf = recognize_crop(ocr, word_crop)
                    if w_text:
                        # Full image normalized coordinates
                        rx = round(x + (float(x1) / float(crop_w)) * w, 4)
                        ry = round(y + (float(y1) / float(crop_h)) * h, 4)
                        rw = round((float(x2 - x1) / float(crop_w)) * w, 4)
                        rh = round((float(y2 - y1) / float(crop_h)) * h, 4)

                        is_lbl = is_printed_label_word(w_text)
                        if field_key in ["address", "emergency_address"]:
                            if re.search(r"person\s*t?o?\s*be\s*contact|in\s*case\s*of\s*emergency|relationship|relasyon", w_text, re.IGNORECASE) or \
                               re.search(r"\b(?:address|puuress|ress|house|nouse|street|subdivision|subaiv|zone|purok)\b", w_text, re.IGNORECASE):
                                is_lbl = True

                        tokens.append({
                            "text": str(w_text).strip(),
                            "conf": round(float(w_conf), 2),
                            "bbox": [rx, ry, rw, rh],
                            "is_label": bool(is_lbl)
                        })

                        if is_lbl:
                            filtered_labels.append(str(w_text).strip())
                        else:
                            words.append(w_text)
                            confs.append(w_conf)
            except Exception as det_err:
                print(f"[ROI Det Warning]: {det_err}")

        # If DBNet found words, join them; otherwise fallback to direct crop recognition
        if words:
            raw_text = " ".join(words).strip()
            avg_conf = float(sum(confs) / len(confs))
        else:
            raw_text, avg_conf = recognize_crop(ocr, crop)
            raw_text = raw_text.strip()
            if is_printed_label_word(raw_text):
                filtered_labels.append(raw_text)
                raw_text = ""
                avg_conf = 0.0

        raw_value = raw_text

        # 2. Apply field-specific domain cleaning (exact same as full document processing)
        cleaned = clean_text(raw_text)
        final_conf = avg_conf

        if field_key:
            cfg = fields_config.get(field_key, {"bbox": bbox, "type": "text"})
            f_type = cfg.get("type", "text")
            cleaned, final_conf = clean_field_value(
                field_key, f_type, raw_text, avg_conf, enhanced_gray, ocr, fields_config
            )

        elapsed = round(time.time() - t_start, 3)
        print(f"[ROI Extract] Field: '{field_key}' Bbox [{x:.3f}, {y:.3f}, {w:.3f}, {h:.3f}] -> Raw: '{raw_value}', Cleaned: '{cleaned}' (conf: {final_conf:.2f}, time: {elapsed}s)")

        return sanitize_for_json({
            "status": "success",
            "value": cleaned,
            "raw_value": raw_value,
            "confidence": round(final_conf, 2),
            "rating": get_confidence_rating(final_conf),
            "bbox": bbox,
            "tokens": tokens,
            "filtered_labels": filtered_labels,
            "execution_time_seconds": elapsed
        })
    except Exception as e:
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8000)

