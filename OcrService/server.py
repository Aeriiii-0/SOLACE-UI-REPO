import os
os.environ["PADDLE_PDX_ENABLE_MKLDNN_BYDEFAULT"] = "False"
os.environ["PADDLE_PDX_USE_PIR_TRT"] = "False"
os.environ["FLAGS_enable_pir_api"] = "0"
os.environ["FLAGS_use_mkldnn"] = "0"
os.environ["FLAGS_enable_pir_in_executor"] = "0"

import time
import json
import cv2
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
from parser import parse_form_with_ocr, clean_text, get_confidence_rating, load_template, recognize_crop, get_ov_recognizer

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

@app.post("/extract-form")
def extract_form(file: UploadFile = File(...), mode: str = "global"):
    """
    Full document ingestion pipeline:
    mode='global': Single full-page detection + spatial bucketing (Default)
    mode='crop': Legacy field-by-field crop loop (Fallback)
    """
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
        
        deskewed_color, enhanced_gray = preprocess_pipeline(contents)
        if deskewed_color is None or enhanced_gray is None:
            print(f"[Server ERROR] Failed to decode image format.")
            raise HTTPException(status_code=400, detail="Invalid image file or format.")
            
        print(f"[Server] 2. Preprocessed image. Shape: {enhanced_gray.shape} (H, W)")
        
        result = parse_form_with_ocr(ocr, enhanced_gray, mode=mode)
        elapsed = round(time.time() - t_start, 2)
        result["execution_time_seconds"] = elapsed
        print(f"[Server] 3. OCR Extraction ({mode}) finished in {elapsed}s. Status: {result.get('status')}, Rating: {result.get('overall_rating')}")
        
        result["preview_image"] = image_to_base64(deskewed_color)
        print(f"[Server] 4. Returning JSON response to C# client.")
        print(f"=======================================================\n")
        return result
        
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
    h: float = Form(...)
):
    """
    Real-time single-field re-extraction when a caseworker repositions a bounding box.
    """
    try:
        t_start = time.time()
        contents = file.file.read()
        _, enhanced_gray = preprocess_pipeline(contents)
        
        if enhanced_gray is None:
            raise HTTPException(status_code=400, detail="Invalid image file.")
            
        bbox = [x, y, w, h]
        crop = crop_roi(enhanced_gray, bbox)
        
        raw_text, conf = recognize_crop(ocr, crop)
        cleaned = clean_text(raw_text)
        elapsed = round(time.time() - t_start, 3)
        
        print(f"[ROI Extract] Bbox [{x:.3f}, {y:.3f}, {w:.3f}, {h:.3f}] -> '{cleaned}' (conf: {conf:.2f}, time: {elapsed}s)")
        
        return {
            "status": "success",
            "value": cleaned,
            "confidence": round(conf, 2),
            "rating": get_confidence_rating(conf),
            "bbox": bbox,
            "execution_time_seconds": elapsed
        }
    except Exception as e:
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8000)

