import os
import re
import json
import cv2
import numpy as np
from preprocess import crop_roi, detect_form_grid_lines, snap_box_to_grid
from cleaners import get_confidence_rating, BINAN_BARANGAYS
from inference import (
    get_ov_recognizer,
    prepare_static_bucket_canvas,
    recognize_crop,
    recognize_with_server_model,
    extract_detections_from_paddle,
)
from labels import (
    assign_detections_to_field,
    extract_by_anchor_label,
    clean_field_value,
    is_printed_label_word,
)
from sections import (
    cell_has_ink,
    detect_encircled_circumstance,
    detect_employment_status,
    parse_family_table_grid,
)

# Safety padding (in pixels) around tight DBNet word polygons.
# Set to 0 to completely disable padding and use raw 0-pixel polygon bounds.
DBNET_CROP_PADDING_PX = 3

def load_template() -> dict:
    """Loads the CSWDO-066-2 form template configuration."""
    template_path = os.path.join(os.path.dirname(__file__), "cswdo_template.json")
    if os.path.exists(template_path):
        with open(template_path, "r", encoding="utf-8-sig") as f:
            return json.load(f)
    return {"fields": {}}

# ==============================================================================
# DYNAMIC ANCHOR ALIGNMENT
# Automatically detects physical scanner margin offsets (dx, dy)
# ==============================================================================
ANCHOR_REGISTRATION_TARGETS = [
    # (name, search_box [x, y, w, h], keywords, ref_y, ref_x)
    ("header_office", [0.05, 0.050, 0.50, 0.060], ["binan", "social", "welfare", "dev", "office", "cswdo"], 0.083, 0.221),
    ("sec1_identifying", [0.02, 0.130, 0.45, 0.060], ["identifying", "information", "personal"], 0.166, 0.138),
    ("sec3_composition", [0.02, 0.400, 0.55, 0.060], ["composition", "household", "family"], 0.429, 0.386),
]

def calculate_dynamic_page_transform(all_detections: list) -> tuple:
    """
    Measures 2-point affine calibration (scale s_y, translation t_y, and dx)
    relative to reference ground-truth landmarks.
    Handles both mechanical scanner margin shifts (dx, dy) AND printer/photocopier
    scaling variations (e.g. 93% - 105% "Fit to Page" zoom).
    Returns: (dx, s_y, t_y)
    """
    landmarks_found = {}
    for name, s_box, keywords, ref_y, ref_x in ANCHOR_REGISTRATION_TARGETS:
        bx, by, bw, bh = s_box
        min_x, max_x = bx - 0.04, bx + bw + 0.04
        min_y, max_y = by - 0.04, by + bh + 0.04

        for d in all_detections:
            if (min_x <= d["cx"] <= max_x) and (min_y <= d["cy"] <= max_y):
                txt = d["text"].lower()
                if any(kw in txt for kw in keywords):
                    landmarks_found[name] = (d["cx"], d["cy"], ref_x, ref_y)
                    break

    shifts_x = []
    for name, (cx, cy, rx, ry) in landmarks_found.items():
        dx = cx - rx
        if abs(dx) <= 0.045:
            shifts_x.append(dx)

    med_dx = float(np.median(shifts_x)) if len(shifts_x) >= 2 else (shifts_x[0] if (len(shifts_x) == 1 and abs(shifts_x[0]) <= 0.015) else 0.0)
    if abs(med_dx) < 0.002:
        med_dx = 0.0

    s_y = 1.0
    t_y = 0.0

    top_pt = landmarks_found.get("sec1_identifying") or landmarks_found.get("header_office")
    bot_pt = landmarks_found.get("sec3_composition")

    if top_pt and bot_pt:
        meas_y1, ref_y1 = top_pt[1], top_pt[3]
        meas_y2, ref_y2 = bot_pt[1], bot_pt[3]
        ref_dist = ref_y2 - ref_y1
        meas_dist = meas_y2 - meas_y1
        if ref_dist > 0.15:
            scale_est = meas_dist / ref_dist
            if 0.90 <= scale_est <= 1.10:
                s_y = float(scale_est)
                t_y = float(meas_y1 - s_y * ref_y1)
                if abs(s_y - 1.0) < 0.003:
                    s_y = 1.0
                    t_y = float(np.median([cy - ry for _, cy, _, ry in landmarks_found.values()]))
    elif landmarks_found:
        shifts_y = [cy - ry for _, cy, _, ry in landmarks_found.values() if abs(cy - ry) <= 0.045]
        if shifts_y:
            t_y = float(np.median(shifts_y))
            s_y = 1.0
            if abs(t_y) < 0.002:
                t_y = 0.0

    return med_dx, s_y, t_y

def calculate_dynamic_page_offset(all_detections: list) -> tuple:
    """
    Measures vertical (dy) and horizontal (dx) translation shift of the scanned page
    relative to reference ground-truth landmarks.
    Returns: (dx, dy) clamped to valid range [-0.045, +0.045].
    """
    shifts_y = []
    shifts_x = []

    for name, s_box, keywords, ref_y, ref_x in ANCHOR_REGISTRATION_TARGETS:
        bx, by, bw, bh = s_box
        min_x, max_x = bx - 0.03, bx + bw + 0.03
        min_y, max_y = by - 0.03, by + bh + 0.03

        for d in all_detections:
            if (min_x <= d["cx"] <= max_x) and (min_y <= d["cy"] <= max_y):
                txt = d["text"].lower()
                if any(kw in txt for kw in keywords):
                    dy = d["cy"] - ref_y
                    dx = d["cx"] - ref_x
                    if abs(dy) <= 0.045:
                        shifts_y.append(dy)
                    if abs(dx) <= 0.045:
                        shifts_x.append(dx)
                    break

    # Require at least 2 agreeing landmarks to avoid shifting page on a single rogue detection
    med_dx = float(np.median(shifts_x)) if len(shifts_x) >= 2 else (shifts_x[0] if (len(shifts_x) == 1 and abs(shifts_x[0]) <= 0.012) else 0.0)
    med_dy = float(np.median(shifts_y)) if len(shifts_y) >= 2 else (shifts_y[0] if (len(shifts_y) == 1 and abs(shifts_y[0]) <= 0.012) else 0.0)

    if abs(med_dx) < 0.002:
        med_dx = 0.0
    if abs(med_dy) < 0.002:
        med_dy = 0.0

    return med_dx, med_dy

def generate_debug_overlay(img_bgr: np.ndarray, fields_config: dict, all_detections: list, polys: list = None) -> str:
    """
    Renders high-visibility visual debugging overlay:
    - Cyan/Gold rectangles for calibrated template query boxes + field names
    - Bright Green rectangles for accepted data words + text + confidences
    - Amber/Orange rectangles for filtered printed form labels
    - Soft Gray contours for background / non-target text
    Saves to OcrService/debug_detections.jpg and bin/Debug/debug_detections.jpg.
    Returns: file path to saved debug image.
    """
    if os.environ.get("SOLACE_OCR_DEBUG", "1") in ("0", "false", "False"):
        return ""

    try:
        debug_img = img_bgr.copy()
        full_h, full_w = debug_img.shape[:2]

        # 1. Soft Gray contours for discarded non-target background text
        if polys is not None:
            try:
                for poly in polys:
                    if poly is not None and len(poly) >= 4:
                        pts_int = np.array(poly, dtype=np.int32)
                        cv2.polylines(debug_img, [pts_int], True, (185, 185, 185), 1)
            except Exception:
                pass

        # 2. Template query boxes (Cyan / Yellow) with field keys
        for fk, cfg in fields_config.items():
            bx, by, bw, bh = cfg.get("bbox", [0, 0, 0, 0])
            if bw <= 0 or bh <= 0:
                continue
            px1, py1 = int(bx * full_w), int(by * full_h)
            px2, py2 = int((bx + bw) * full_w), int((by + bh) * full_h)
            cv2.rectangle(debug_img, (px1, py1), (px2, py2), (255, 200, 0), 1)
            cv2.putText(debug_img, fk[:16], (px1, max(10, py1 - 2)), cv2.FONT_HERSHEY_SIMPLEX, 0.32, (255, 180, 0), 1)

        # 3. Detected Word Polygons / Bounding Boxes
        for d in all_detections:
            cx, cy = d["cx"], d["cy"]
            bw, bh = d.get("bbox", [0, 0, 0.02, 0.015])[2], d.get("bbox", [0, 0, 0.02, 0.015])[3]
            gx1 = max(0, int((cx - bw / 2.0) * full_w) - DBNET_CROP_PADDING_PX)
            gy1 = max(0, int((cy - bh / 2.0) * full_h) - DBNET_CROP_PADDING_PX)
            gx2 = min(full_w, int((cx + bw / 2.0) * full_w) + DBNET_CROP_PADDING_PX)
            gy2 = min(full_h, int((cy + bh / 2.0) * full_h) + DBNET_CROP_PADDING_PX)

            txt = d.get("text", "").strip()
            conf = d.get("conf", 0.0)
            is_lbl = is_printed_label_word(txt)

            if is_lbl:
                # Amber / Orange: Form Label Header
                cv2.rectangle(debug_img, (gx1, gy1), (gx2, gy2), (0, 140, 255), 1)
                cv2.putText(debug_img, f"[LBL] {txt[:12]}", (gx1, max(12, gy1 - 3)), cv2.FONT_HERSHEY_SIMPLEX, 0.32, (0, 140, 255), 1)
            else:
                # Bright Green: Data Token
                cv2.rectangle(debug_img, (gx1, gy1), (gx2, gy2), (0, 255, 0), 2)
                cv2.putText(debug_img, f"{txt[:15]} ({conf:.2f})", (gx1, max(12, gy1 - 3)), cv2.FONT_HERSHEY_SIMPLEX, 0.35, (0, 200, 0), 1)

        debug_save_path = os.path.join(os.path.dirname(__file__), "debug_detections.jpg")
        cv2.imwrite(debug_save_path, debug_img)

        # Also save to Windows Temp directory as solace_last_scan_detections.jpg
        try:
            import tempfile
            temp_path = os.path.join(tempfile.gettempdir(), "solace_last_scan_detections.jpg")
            cv2.imwrite(temp_path, debug_img)
        except Exception:
            pass

        # Also save to bin/Debug and bin/x64/Debug
        for sub in [("..", "bin", "Debug"), ("..", "bin", "x64", "Debug")]:
            bin_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), *sub))
            if os.path.exists(bin_dir):
                cv2.imwrite(os.path.join(bin_dir, "debug_detections.jpg"), debug_img)

        print(f"[Debug Visualizer SUCCESS] Saved visual detections overlay to: {debug_save_path}")
        return debug_save_path
    except Exception as vis_err:
        print(f"[Debug Visualizer Note]: {vis_err}")
        return ""

def extract_field_crop_multitoken(img_src: np.ndarray, bbox: list, ocr_engine, field_key: str = "") -> tuple:
    """
    Tier-2 Segmented Word Extractor:
    Crops field bbox, adds boundary clearance, detects word polygons with DBNet,
    and runs PP-OCRv5 Latin handwriting recognition on each word.
    """
    if img_src is None or img_src.size == 0:
        return "", 0.0

    crop = crop_roi(img_src, bbox)
    if crop is None or crop.size == 0:
        return "", 0.0

    ch, cw = crop.shape[:2]
    # If crop is low-resolution, upscale 2x
    if ch < 50:
        crop = cv2.resize(crop, (0, 0), fx=2.0, fy=2.0, interpolation=cv2.INTER_CUBIC)

    # White padding so DBNet has boundary margin
    padded = cv2.copyMakeBorder(crop, 25, 25, 25, 25, cv2.BORDER_CONSTANT, value=[255, 255, 255])

    p = getattr(ocr_engine, "paddlex_pipeline", getattr(ocr_engine, "pipeline", ocr_engine))
    pipeline_obj = getattr(p, "_pipeline", p)
    det_model = getattr(pipeline_obj, "text_det_model", None)

    polys = []
    if det_model is not None:
        try:
            det_res = list(det_model.predict(padded))
            polys = det_res[0].get("dt_polys", []) if det_res else []
        except Exception:
            polys = []

    tokens = []
    for poly in polys:
        if poly is None or len(poly) < 4:
            continue
        pts = np.array(poly)
        px1 = float(np.min(pts[:, 0]))
        py1 = float(np.min(pts[:, 1]))
        px2 = float(np.max(pts[:, 0]))
        py2 = float(np.max(pts[:, 1]))

        sub = padded[int(py1):int(py2), int(px1):int(px2)]
        if sub.size > 0:
            txt, sc = recognize_with_server_model(sub)
            txt_clean = txt.strip()
            if not txt_clean:
                continue
            # Filter printed headers
            if is_printed_label_word(txt_clean) or any(lbl in txt_clean.upper() for lbl in [
                "ADDRESS", "HOUSE", "STREET", "SUBDIVISION", "ZONE", "PUROK", "RESS",
                "PERSON", "CONTACTED", "EMERGENCY", "RELATIONSHIP", "RELATIONSHIN"
            ]):
                continue
            tokens.append((px1, txt_clean, sc))

    if tokens:
        tokens.sort(key=lambda t: t[0])
        combined = " ".join(t[1] for t in tokens)
        avg_sc = float(sum(t[2] for t in tokens) / len(tokens))
        return combined, avg_sc

    # Fallback to direct recognition on entire crop
    return recognize_with_server_model(crop)

def parse_form_with_global_ocr(ocr_engine, enhanced_image: np.ndarray, template: dict = None, raw_image: np.ndarray = None) -> dict:
    """
    High-Performance Full-Page Spatial Bucketed Extractor:
    1. Runs full-page text detection once (~1.4s).
    2. Spatially pre-filters boxes against active template fields (discarding ~150+ unneeded boxes).
    3. Enforces static 2-bucket shapes: (N1, 3, 48, 320) for short fields and (N2, 3, 48, 960) for long fields.
    4. Executes direct native OpenVINO C++ batch tensor inference (NO Python-side wrappers or threading).
    """
    if template is None:
        template = load_template()

    fields_config = template.get("fields", {})
    extracted_fields = {}
    full_h, full_w = enhanced_image.shape[:2]

    # 1. High-Speed Detection & Static 2-Bucket Pre-Filtering Pipeline
    all_detections = []
    raw_polys = []
    try:
        if len(enhanced_image.shape) == 2:
            img_bgr = cv2.cvtColor(enhanced_image, cv2.COLOR_GRAY2BGR)
        else:
            img_bgr = enhanced_image

        p = getattr(ocr_engine, "paddlex_pipeline", None)
        pipeline_obj = getattr(p, "_pipeline", None) if p else None

        if pipeline_obj and hasattr(pipeline_obj, "text_det_model"):
            # Step A: Run DBNet Detection on full page (~1.4s)
            det_res = list(pipeline_obj.text_det_model.predict(img_bgr))
            polys = det_res[0].get("dt_polys", []) if det_res else []
            raw_polys = polys
            print(f"[Global OCR] Detected {len(polys)} total candidate boxes across page.")

            # Step B: Spatial Pre-Filter into exactly 2 Static Tensor Buckets
            short_tensors = []
            short_meta = []
            long_tensors = []
            long_meta = []

            for poly in polys:
                if poly is None or len(poly) < 4:
                    continue
                pts = np.array(poly)
                xmin = float(np.min(pts[:, 0])) / float(full_w)
                xmax = float(np.max(pts[:, 0])) / float(full_w)
                ymin = float(np.min(pts[:, 1])) / float(full_h)
                ymax = float(np.max(pts[:, 1])) / float(full_h)
                cx = (xmin + xmax) / 2.0
                cy = (ymin + ymax) / 2.0

                # Check if center falls inside any template field bounding box
                is_target = False
                pad_x = 0.015
                pad_y_top = 0.020
                pad_y_bot = 0.025
                for fk, cfg in fields_config.items():
                    bx, by, bw, bh = cfg.get("bbox", [0, 0, 0, 0])
                    if (bx - pad_x <= cx <= bx + bw + pad_x) and (by - pad_y_top <= cy <= by + bh + pad_y_bot):
                        is_target = True
                        break
                if not is_target:
                    for _, s_box, _, _, _ in ANCHOR_REGISTRATION_TARGETS:
                        abx, aby, abw, abh = s_box
                        if (abx - 0.02 <= cx <= abx + abw + 0.02) and (aby - 0.02 <= cy <= aby + abh + 0.02):
                            is_target = True
                            break

                if is_target:
                    x1 = max(0, int(np.min(pts[:, 0])) - DBNET_CROP_PADDING_PX)
                    y1 = max(0, int(np.min(pts[:, 1])) - DBNET_CROP_PADDING_PX)
                    x2 = min(full_w, int(np.max(pts[:, 0])) + DBNET_CROP_PADDING_PX)
                    y2 = min(full_h, int(np.max(pts[:, 1])) + DBNET_CROP_PADDING_PX)
                    if x2 > x1 and y2 > y1:
                        raw_crop = img_bgr[y1:y2, x1:x2]
                        ch, cw = raw_crop.shape[:2]
                        aspect_ratio = float(cw) / float(max(1, ch))
                        meta_dict = {
                            "cx": cx,
                            "cy": cy,
                            "bbox": [xmin, ymin, max(0.001, xmax - xmin), max(0.001, ymax - ymin)]
                        }

                        # Static 2-Bucket Classification & Normalization
                        if aspect_ratio <= 6.0 and cw <= 280:
                            canvas = prepare_static_bucket_canvas(raw_crop, target_w=320, target_h=48)
                            norm = (canvas.astype(np.float32) / 255.0 - 0.5) / 0.5
                            short_tensors.append(np.transpose(norm, (2, 0, 1)))
                            short_meta.append(meta_dict)
                        else:
                            canvas = prepare_static_bucket_canvas(raw_crop, target_w=960, target_h=48)
                            norm = (canvas.astype(np.float32) / 255.0 - 0.5) / 0.5
                            long_tensors.append(np.transpose(norm, (2, 0, 1)))
                            long_meta.append(meta_dict)

            discarded_count = len(polys) - (len(short_tensors) + len(long_tensors))
            print(f"[Global OCR 2-Bucket Gating] Discarded {discarded_count} non-field boxes. Bucketed {len(short_tensors)} short crops (48x320) and {len(long_tensors)} long crops (48x960).")

            # Step C: Direct Raw OpenVINO Recognition Execution
            ov_rec = get_ov_recognizer()
            if ov_rec:
                if short_tensors:
                    batch_np1 = np.stack(short_tensors, axis=0)
                    texts1, confs1 = ov_rec.recognize(batch_np1, bucket="short")
                    for idx, (t, s) in enumerate(zip(texts1, confs1)):
                        if t:
                            meta = short_meta[idx]
                            all_detections.append({
                                "text": str(t).strip(),
                                "conf": s,
                                "cx": meta["cx"],
                                "cy": meta["cy"],
                                "bbox": meta["bbox"]
                            })

                if long_tensors:
                    batch_np2 = np.stack(long_tensors, axis=0)
                    texts2, confs2 = ov_rec.recognize(batch_np2, bucket="long")
                    for idx, (t, s) in enumerate(zip(texts2, confs2)):
                        if t:
                            meta = long_meta[idx]
                            all_detections.append({
                                "text": str(t).strip(),
                                "conf": s,
                                "cx": meta["cx"],
                                "cy": meta["cy"],
                                "bbox": meta["bbox"]
                            })
            else:
                # Fallback to PaddleX if OpenVINO fails to instantiate
                if short_tensors or long_tensors:
                    all_crops = [prepare_static_bucket_canvas(img_bgr[int(m['bbox'][1]*full_h):int((m['bbox'][1]+m['bbox'][3])*full_h), int(m['bbox'][0]*full_w):int((m['bbox'][0]+m['bbox'][2])*full_w)], 320, 48) for m in short_meta + long_meta]
                    rec_res = list(pipeline_obj.text_rec_model.predict(all_crops))
                    for idx, r in enumerate(rec_res):
                        t = r.get("rec_text") if isinstance(r, dict) else str(r)
                        s = float(r.get("rec_score", 0.9)) if isinstance(r, dict) else 0.9
                        if t:
                            m = (short_meta + long_meta)[idx]
                            all_detections.append({"text": str(t).strip(), "conf": s, "cx": m["cx"], "cy": m["cy"], "bbox": m["bbox"]})
        else:
            # Fallback to standard predict
            pred_res = list(ocr_engine.predict(img_bgr))
            all_detections = extract_detections_from_paddle(pred_res, full_w, full_h)
            print(f"[Global OCR Fallback] Detected {len(all_detections)} text items.")

    except Exception as e:
        print(f"[Global OCR Error during inference]: {e}")
        all_detections = []

    total_conf = 0.0
    field_count = 0

    # 1.5 Dynamic Anchor Alignment & Grid Snapping (Scale, Translation, and Cell Line Alignment)
    dyn_dx, dyn_sy, dyn_ty = calculate_dynamic_page_transform(all_detections)
    if dyn_dx != 0.0 or dyn_sy != 1.0 or dyn_ty != 0.0:
        print(f"[Dynamic Alignment] Detected physical scanner transform: dx={dyn_dx:+.4f}, scale_y={dyn_sy:.4f}, origin_y={dyn_ty:+.4f}. Auto-adjusting template query boxes!")

    # Detect physical form grid lines for pixel-perfect cell boundary snapping
    gray_for_lines = cv2.cvtColor(enhanced_image, cv2.COLOR_BGR2GRAY) if len(enhanced_image.shape) == 3 else enhanced_image
    h_lines_mask, v_lines_mask = detect_form_grid_lines(gray_for_lines)

    aligned_fields = {}
    for fk, cfg in fields_config.items():
        c_cfg = dict(cfg)
        if "bbox" in c_cfg:
            bx, by, bw, bh = c_cfg["bbox"]
            is_checkbox = (
                fk in ["is_employed", "is_self_employed", "is_not_employed"] or
                fk.endswith("_checkbox") or
                fk.startswith("circ_")
            )
            if is_checkbox:
                # Checkboxes are micro-ROIs calibrated directly on deskewed coordinates.
                # Never distort them with grid snapping or macro translation shifts.
                c_cfg["bbox"] = [bx, by, bw, bh]
            else:
                aligned_by = dyn_ty + dyn_sy * by
                aligned_bh = dyn_sy * bh
                aligned_bx = bx + dyn_dx
                transformed_box = [
                    max(0.0, min(0.98, round(aligned_bx, 4))),
                    max(0.0, min(0.98, round(aligned_by, 4))),
                    bw,
                    max(0.005, min(0.98, round(aligned_bh, 4)))
                ]
                # Snap box inside the physical printed table lines for Section I data fields
                if fk not in ["needs", "other_income"] and not fk.startswith("fam_row"):
                    has_top = fk in ["address", "emergency_name", "emergency_relationship", "emergency_address", "emergency_number"]
                    snapped_box = snap_box_to_grid(transformed_box, h_lines_mask, v_lines_mask, full_w, full_h, max_search_px=15, has_top_label=has_top)
                    c_cfg["bbox"] = snapped_box
                else:
                    c_cfg["bbox"] = transformed_box
        aligned_fields[fk] = c_cfg
    fields_config = aligned_fields

    # 2. Spatial Bucketing into Template Fields
    for f_key, cfg in fields_config.items():
        f_type = cfg.get("type", "text")
        if f_type in ["table", "circumstance_group"] or f_key.startswith("circ_") or f_key.startswith("fam_row"):
            continue

        bbox = cfg.get("bbox", [0, 0, 0, 0])

        if f_key == "employment_status":
            raw_text = ""
            cleaned, conf = detect_employment_status(enhanced_image, ocr_engine, fields_config, all_detections=all_detections, raw_text=raw_text)
        elif f_key in ["is_employed", "is_self_employed", "is_not_employed"]:
            emp_val = extracted_fields.get("employment_status", {}).get("value", "")
            if not emp_val:
                emp_val, emp_conf = detect_employment_status(enhanced_image, ocr_engine, fields_config, all_detections=all_detections)
                winning_key = "is_not_employed" if emp_val == "Not Employed" else ("is_self_employed" if emp_val == "Self-Employed" else "is_employed")
                extracted_fields["employment_status"] = {
                    "value": emp_val,
                    "confidence": round(emp_conf, 2),
                    "rating": get_confidence_rating(emp_conf) if emp_val else "Low",
                    "bbox": fields_config.get(winning_key, {}).get("bbox", bbox),
                    "category": "Section I"
                }
            if f_key == "is_employed":
                cleaned = "True" if emp_val == "Employed" else "False"
            elif f_key == "is_self_employed":
                cleaned = "True" if emp_val == "Self-Employed" else "False"
            elif f_key == "is_not_employed":
                cleaned = "True" if emp_val == "Not Employed" else "False"
            conf = 0.95
        else:
            # 1. Primary: Spatially assign detections to user-calibrated template bbox
            raw_text, conf = assign_detections_to_field(f_key, cfg, all_detections)
            if raw_text:
                print(f'  [Template Bbox Match] [{f_key}]: "{raw_text}" (conf: {conf:.2f})')
            else:
                # 2. Fallback: Label-Relative Dynamic Extraction (if bbox was empty or shifted)
                raw_text, conf = extract_by_anchor_label(f_key, all_detections, fields_config)
                if raw_text:
                    print(f'  [Label-Relative Fallback] [{f_key}]: "{raw_text}" (conf: {conf:.2f})')

            # 3. Fallback to crop OCR if still not captured
            if not raw_text:
                crop = crop_roi(enhanced_image, bbox)
                if cell_has_ink(crop):
                    raw_text, conf = recognize_crop(ocr_engine, crop)
                    if is_printed_label_word(raw_text):
                        raw_text = ""
                        conf = 0.0

        if f_key not in ["employment_status", "is_employed", "is_self_employed", "is_not_employed"]:
            cleaned, conf = clean_field_value(f_key, f_type, raw_text, conf, enhanced_image, ocr_engine, fields_config, all_detections=all_detections)

        # Two-Tier Hybrid Escalation: Enabled by default for handwriting / low-confidence text fields
        enable_tier2 = os.environ.get("ENABLE_TIER2_SERVER", "1") not in ("0", "false", "False")
        if enable_tier2 and f_type == "text" and f_key not in ["employment_status", "is_employed", "is_self_employed", "is_not_employed"]:
            # Never overwrite a successfully validated barangay or date with lower quality crop noise
            is_valid_barangay = (f_key == "barangay" and cleaned and any(b.lower() == cleaned.lower() for b in BINAN_BARANGAYS))
            is_valid_date = (f_key in ["birthdate", "application_date"] and cleaned and len(cleaned) == 10 and "-" in cleaned)

            if not is_valid_barangay and not is_valid_date:
                tier2_source = raw_image if raw_image is not None else enhanced_image
                cell_crop = crop_roi(tier2_source, bbox)
                # An address consisting only of digits (e.g. "4422") or <= 4 chars is likely incomplete
                is_incomplete_addr = (
                    f_key in ["address", "emergency_address"] and 
                    cleaned and 
                    (re.fullmatch(r"^\d+[\s\.\-_]*$", str(cleaned).strip()) or len(str(cleaned).strip()) <= 4 or len(str(cleaned).strip().split()) < 2)
                )
                if cell_has_ink(cell_crop, thresh_val=0.012) and (conf < 0.75 or not cleaned or is_incomplete_addr):
                    if f_key in ["address", "emergency_address", "last_name", "first_name", "middle_name"]:
                        t2_raw, t2_score = extract_field_crop_multitoken(tier2_source, bbox, ocr_engine, f_key)
                    else:
                        t2_raw, t2_score = recognize_with_server_model(cell_crop)
                    if t2_raw:
                        t2_cleaned, t2_conf = clean_field_value(f_key, f_type, t2_raw, t2_score, enhanced_image, ocr_engine, fields_config)
                        # For barangay, only accept if t2_cleaned is also a valid barangay
                        if f_key == "barangay" and t2_cleaned and not any(b.lower() == t2_cleaned.lower() for b in BINAN_BARANGAYS):
                            pass
                        elif t2_cleaned and (t2_conf > conf or not cleaned or is_incomplete_addr):
                            if is_incomplete_addr and not any(c.isalpha() for c in str(t2_cleaned)) and len(str(t2_cleaned)) <= len(str(cleaned)):
                                pass
                            else:
                                safe_old = str(cleaned).encode('ascii', 'replace').decode('ascii'); safe_new = str(t2_cleaned).encode('ascii', 'replace').decode('ascii'); print(f"  [Tier 2 Upgrade] [{f_key}]: '{safe_old}' ({conf:.2f}) -> upgraded to '{safe_new}' ({t2_conf:.2f})")
                                cleaned = t2_cleaned
                                conf = max(conf, t2_conf)

        if cleaned:
            print(f"  -> Extracted [{f_key}]: \"{cleaned}\" (conf: {conf:.2f})")

        # Visual scope parity: Return the user-configured template bbox so the WPF
        # Picture Preview highlight box matches the Adjustable Boxes editor 1:1.
        orig_box = template.get("fields", {}).get(f_key, {}).get("bbox", bbox)

        extracted_fields[f_key] = {
            "value": cleaned,
            "confidence": round(conf, 2),
            "rating": get_confidence_rating(conf) if cleaned else "Low",
            "bbox": orig_box,
            "aligned_bbox": bbox,
            "category": cfg.get("category", "general")
        }
        if cleaned:
            total_conf += conf
            field_count += 1

    # 3. Extract Section II Circumstances (Pen Marks / Encircled detection)
    detected_circumstance = detect_encircled_circumstance(enhanced_image, ocr_engine, template)
    if detected_circumstance.get("detected"):
        print(f"[Circumstance Detected] Code: {detected_circumstance.get('code')} -> '{detected_circumstance.get('label')}' (conf: {detected_circumstance.get('confidence')})")

    # 4. Extract Section III/V Family Dependents Table
    family_members = parse_family_table_grid(ocr_engine, enhanced_image, fields_config, all_detections)

    # 5. Cross-field consistency: Auto-calculate and verify Age from Birthdate
    try:
        from datetime import datetime
        bdate_str = extracted_fields.get("birthdate", {}).get("value", "")
        if bdate_str:
            bparts = bdate_str.split("/")
            if len(bparts) == 3:
                b_day, b_mon, b_yr = int(bparts[0]), int(bparts[1]), int(bparts[2])
                now_yr = datetime.now().year
                calc_age = now_yr - b_yr
                curr_age_dict = extracted_fields.get("age", {})
                curr_age_val = curr_age_dict.get("value", "")
                if not curr_age_val or not curr_age_val.isdigit() or abs(int(curr_age_val) - calc_age) > 2:
                    if 0 <= calc_age <= 120:
                        extracted_fields["age"]["value"] = str(calc_age)
                        extracted_fields["age"]["confidence"] = 0.95
                        extracted_fields["age"]["rating"] = "High"
    except Exception:
        pass

    overall_confidence = round(total_conf / field_count, 2) if field_count > 0 else 0.0

    # 4. Generate always-on real-time visual debug overlay matching template boxes
    debug_path = ""
    try:
        debug_path = generate_debug_overlay(img_bgr, template.get("fields", fields_config), all_detections, polys=raw_polys)
    except Exception as _dbg_err:
        print(f"[Debug Overlay Call Error]: {_dbg_err}")

    return {
        "status": "success",
        "form_id": template.get("form_id", "CSWDO-066-2"),
        "overall_confidence": overall_confidence,
        "overall_rating": get_confidence_rating(overall_confidence),
        "fields": extracted_fields,
        "circumstance": detected_circumstance,
        "family_members": family_members,
        "debug_image_path": debug_path
    }

def parse_form_with_crop_ocr(ocr_engine, enhanced_image: np.ndarray, template: dict = None) -> dict:
    """
    Legacy Field-by-Field ROI Crop Extractor (Preserved for 100% Rollback Safety).
    """
    if template is None:
        template = load_template()

    fields_config = template.get("fields", {})
    extracted_fields = {}
    full_h, full_w = enhanced_image.shape[:2]

    total_conf = 0.0
    field_count = 0

    for f_key, cfg in fields_config.items():
        f_type = cfg.get("type", "text")
        if f_type in ["table", "circumstance_group"]:
            continue

        bbox = cfg.get("bbox", [0, 0, 0, 0])
        crop = crop_roi(enhanced_image, bbox)

        raw_text, conf = recognize_crop(ocr_engine, crop)
        cleaned, conf = clean_field_value(f_key, f_type, raw_text, conf, enhanced_image, ocr_engine, fields_config)

        # Two-Tier Hybrid Escalation: Disabled by default for < 2.0s real-time performance
        enable_tier2 = os.environ.get("ENABLE_TIER2_SERVER", "0") in ("1", "true", "True")
        if enable_tier2 and f_type == "text" and f_key not in ["employment_status", "is_employed", "is_self_employed", "is_not_employed"]:
            cell_crop = crop_roi(enhanced_image, bbox)
            is_incomplete_addr = (
                f_key in ["address", "emergency_address"] and 
                cleaned and 
                (re.fullmatch(r"^\d+[\s\.\-_]*$", str(cleaned).strip()) or len(str(cleaned).strip()) <= 4 or len(str(cleaned).strip().split()) < 2)
            )
            if cell_has_ink(cell_crop, thresh_val=0.012) and (conf < 0.75 or not cleaned or is_incomplete_addr):
                t2_raw, t2_score = recognize_with_server_model(cell_crop)
                if t2_raw:
                    t2_cleaned, t2_conf = clean_field_value(f_key, f_type, t2_raw, t2_score, enhanced_image, ocr_engine, fields_config)
                    if t2_cleaned and (t2_conf > conf or not cleaned or is_incomplete_addr):
                        if is_incomplete_addr and not any(c.isalpha() for c in str(t2_cleaned)) and len(str(t2_cleaned)) <= len(str(cleaned)):
                            pass
                        else:
                            safe_old = str(cleaned).encode('ascii', 'replace').decode('ascii'); safe_new = str(t2_cleaned).encode('ascii', 'replace').decode('ascii'); print(f"  [Tier 2 Upgrade] [{f_key}]: '{safe_old}' ({conf:.2f}) -> upgraded to '{safe_new}' ({t2_conf:.2f})")
                            cleaned = t2_cleaned
                            conf = max(conf, t2_conf)

        if cleaned:
            print(f"  -> Extracted [{f_key}]: \"{cleaned}\" (conf: {conf:.2f})")

        extracted_fields[f_key] = {
            "value": cleaned,
            "confidence": round(conf, 2),
            "rating": get_confidence_rating(conf) if cleaned else "Low",
            "bbox": bbox,
            "category": cfg.get("category", "general")
        }
        if cleaned:
            total_conf += conf
            field_count += 1

    detected_circumstance = detect_encircled_circumstance(enhanced_image, ocr_engine, template)
    family_members = parse_family_table_grid(ocr_engine, enhanced_image, fields_config, None)

    # 5. Cross-field consistency: Auto-calculate and verify Age from Birthdate
    try:
        from datetime import datetime
        bdate_str = extracted_fields.get("birthdate", {}).get("value", "")
        if bdate_str:
            bparts = bdate_str.split("/")
            if len(bparts) == 3:
                b_day, b_mon, b_yr = int(bparts[0]), int(bparts[1]), int(bparts[2])
                now_yr = datetime.now().year
                calc_age = now_yr - b_yr
                curr_age_dict = extracted_fields.get("age", {})
                curr_age_val = curr_age_dict.get("value", "")
                if not curr_age_val or not curr_age_val.isdigit() or abs(int(curr_age_val) - calc_age) > 2:
                    if 0 <= calc_age <= 120:
                        extracted_fields["age"]["value"] = str(calc_age)
                        extracted_fields["age"]["confidence"] = 0.95
                        extracted_fields["age"]["rating"] = "High"
    except Exception:
        pass

    overall_confidence = round(total_conf / field_count, 2) if field_count > 0 else 0.0

    return {
        "status": "success",
        "form_id": template.get("form_id", "CSWDO-066-2"),
        "overall_confidence": overall_confidence,
        "overall_rating": get_confidence_rating(overall_confidence),
        "fields": extracted_fields,
        "circumstance": detected_circumstance,
        "family_members": family_members
    }

def parse_form_with_ocr(ocr_engine, enhanced_image: np.ndarray, template: dict = None, mode: str = "global", raw_image: np.ndarray = None) -> dict:
    """
    Main extraction dispatcher.
    mode='global': High-Performance Full-Page Spatial Bucketed Extractor (Default).
    mode='crop': Legacy Field-by-Field ROI Crop Extractor (Fallback).
    raw_image: Optional unscaled full-resolution image for high-fidelity Tier-2 crops.
    """
    if mode == "crop":
        return parse_form_with_crop_ocr(ocr_engine, enhanced_image, template)
    return parse_form_with_global_ocr(ocr_engine, enhanced_image, template, raw_image=raw_image)
