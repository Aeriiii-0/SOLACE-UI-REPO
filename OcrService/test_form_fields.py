import os
import sys
import time
import json
import re

os.environ["PADDLE_PDX_ENABLE_MKLDNN_BYDEFAULT"] = "False"
os.environ["FLAGS_enable_pir_api"] = "0"
os.environ["FLAGS_use_mkldnn"] = "0"

import cv2
from paddleocr import PaddleOCR

FORM_LABELS_TO_STRIP = [
    r"^1\.\s*Name\s*\(Last\):?",
    r"^Last\s*Name:?",
    r"^First\s*Name:?",
    r"^Middle\s*Name:?",
    r"^Date\s*of\s*Birth:?",
    r"^Age:?",
    r"^Sex:?",
    r"^Civil\s*Status:?",
    r"^Contact\s*Number:?",
    r"^Address:?",
    r"^Barangay:?",
    r"^Occupation:?",
    r"^Monthly\s*Income:?",
    r"^In\s*case\s*of\s*emergency:?",
    r"^Contact\s*No:?",
    r"^Name:?",
]

def clean_field_text(raw_text: str) -> str:
    cleaned = raw_text.strip()
    for pat in FORM_LABELS_TO_STRIP:
        cleaned = re.sub(pat, "", cleaned, flags=re.IGNORECASE).strip()
    # Remove leading colons, underscores, dashes
    cleaned = re.sub(r"^[:\-_\s]+", "", cleaned).strip()
    return cleaned

def run_field_extraction(image_path: str):
    print("==================================================")
    print("      HIGH-SPEED FIELD-BY-FIELD OCR RUNNER        ")
    print("==================================================")

    # 1. Load image
    if not os.path.exists(image_path):
        print(f"[Error] File not found: {image_path}")
        return

    img = cv2.imread(image_path)
    if img is None:
        print(f"[Error] Could not decode image: {image_path}")
        return

    h, w = img.shape[:2]
    print(f"[Step 1] Image Loaded: {w}x{h} px from '{image_path}'")

    # 2. Load template coordinates
    template_path = "cswdo_template.json"
    if not os.path.exists(template_path):
        print(f"[Error] Template not found: {template_path}")
        return

    with open(template_path, "r", encoding="utf-8-sig") as f:
        template = json.load(f)

    fields = template.get("fields", {})

    # 3. Initialize PaddleOCR
    print("\n[Step 2] Initializing PP-OCRv4 Mobile...")
    ocr = PaddleOCR(
        use_doc_orientation_classify=False,
        use_doc_unwarping=False,
        use_textline_orientation=False,
        ocr_version="PP-OCRv4",
        lang="en"
    )
    print("[Step 2] Engine ready!\n")

    # 4. Prepare all target crops for single-batch prediction
    print("[Step 3] Cropping all target fields and batching for instant prediction...")
    start_time = time.time()
    
    field_keys = []
    crop_list = []
    field_configs = []

    for field_name, cfg in fields.items():
        if cfg.get("type") in ["table", "circumstance_group"]:
            continue

        bbox = cfg.get("bbox", [0, 0, 0, 0])
        norm_x, norm_y, norm_w, norm_h = bbox

        x1 = max(0, int(norm_x * w))
        y1 = max(0, int(norm_y * h))
        x2 = min(w, int((norm_x + norm_w) * w))
        y2 = min(h, int((norm_y + norm_h) * h))

        if x2 <= x1 or y2 <= y1:
            continue

        crop = img[y1:y2, x1:x2]
        padded_crop = cv2.copyMakeBorder(crop, 20, 20, 20, 20, cv2.BORDER_CONSTANT, value=[255, 255, 255])
        
        field_keys.append(field_name)
        crop_list.append(padded_crop)
        field_configs.append(cfg)

    print(f"         Running batch inference on {len(crop_list)} fields simultaneously...")
    t_batch_start = time.time()
    batch_results = list(ocr.predict(crop_list))
    batch_duration = time.time() - t_batch_start
    print(f"         Batch neural network completed in {batch_duration:.2f}s!")

    extracted_data = {}

    for idx, res in enumerate(batch_results):
        field_name = field_keys[idx]
        cfg = field_configs[idx]
        
        detected_texts = []
        confidences = []
        
        if hasattr(res, "keys") or isinstance(res, dict):
            res_dict = dict(res) if hasattr(res, "keys") else res
            t_list = res_dict.get("rec_texts") or res_dict.get("rec_text", [])
            s_list = res_dict.get("rec_scores") or res_dict.get("rec_score", [])
            if hasattr(t_list, "tolist"): t_list = t_list.tolist()
            if hasattr(s_list, "tolist"): s_list = s_list.tolist()
            detected_texts = t_list
            confidences = s_list

        combined_raw = " ".join([str(t) for t in detected_texts if str(t).strip()])
        cleaned_val = clean_field_text(combined_raw)
        avg_conf = sum(confidences) / len(confidences) if confidences else 0.0

        extracted_data[field_name] = {
            "label": cfg.get("label", field_name),
            "raw": combined_raw,
            "value": cleaned_val,
            "confidence": avg_conf
        }

    total_time = time.time() - start_time

    # 5. Print Results Table
    print("\n================================================================================")
    print(f"               EXTRACTION RESULTS (Total Time: {total_time:.2f}s)               ")
    print("================================================================================")
    print(f"{'FIELD':<22} | {'CONF':<6} | {'EXTRACTED VALUE':<35} | {'RAW OCR'}")
    print("-" * 80)
    for k, v in extracted_data.items():
        val_display = v['value'] if v['value'] else "(empty)"
        print(f"{v['label']:<22} | {v['confidence']:<6.2f} | {val_display:<35} | {v['raw']}")
    print("================================================================================\n")

if __name__ == "__main__":
    if len(sys.argv) > 1:
        path = sys.argv[1]
    else:
        candidates = [f for f in os.listdir(".") if f.lower().endswith((".jpg", ".png", ".jpeg"))]
        path = candidates[0] if candidates else "sample.jpg"
        
    run_field_extraction(path)
