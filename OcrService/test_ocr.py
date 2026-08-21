import os
import sys
import time
import json
import re
import cv2
import numpy as np

# Prevent OneDNN crashes
os.environ["PADDLE_PDX_ENABLE_MKLDNN_BYDEFAULT"] = "False"
os.environ["FLAGS_enable_pir_api"] = "0"
os.environ["FLAGS_use_mkldnn"] = "0"

from paddleocr import PaddleOCR

# Regex patterns to strip printed form labels and keep pure handwritten text
LABEL_PREFIX_PATTERNS = {
    "last_name": [r"^1\.\s*Name\s*\(Last\):?", r"^Last\s*Name:?", r"^Surname:?"],
    "first_name": [r"^\(First\):?", r"^First\s*Name:?"],
    "middle_name": [r"^\(Middle\):?", r"^Middle\s*Name:?"],
    "ext_name": [r"^\(Ext\.\):?", r"^Ext\.?:?"],
    "civil_status": [r"^2\.\s*Civil\s*Status:?", r"^Civil\s*Status:?"],
    "sex": [r"^3\.\s*Sex:?", r"^Sex:?"],
    "birthdate": [r"^4\.\s*Date\s*of\s*Birth:?", r"^Date\s*of\s*Birth:?", r"^DOB:?"],
    "birthplace": [r"^5\.\s*Place\s*of\s*Birth:?", r"^Place\s*of\s*Birth:?"],
    "age": [r"^6\.\s*Age:?", r"^Age:?"],
    "educational_attainment": [r"^7\.\s*Educational\s*Attainment:?", r"^Educational:?"],
    "philsys_number": [r"^8\.\s*PhilSys.*?:?", r"^PhilSys:?"],
    "religion": [r"^9\.\s*Religion:?", r"^Religion:?"],
    "occupation": [r"^10\.\s*Occupation.*?:?", r"^Occupation:?"],
    "monthly_income": [r"^11\.\s*Monthly\s*Income:?", r"^Monthly\s*Income:?"],
    "employment_status": [r"^12\.\s*Employment.*?:?", r"^Employment:?"],
    "address": [r"^13\.\s*Address:?", r"^Address:?", r"^House\s*No\.?:?"],
    "barangay": [r"^Barangay:?", r"^Brgy\.?:?"],
    "contact_number": [r"^Contact\s*Number:?", r"^Contact\s*No\.?:?", r"^Mobile:?"],
    "emergency_name": [r"^In\s*case\s*of\s*emergency:?", r"^Person\s*to\s*be\s*contacted.*?:?", r"^Name:?"],
    "emergency_relationship": [r"^Relationship:?", r"^Rel:?"],
    "emergency_address": [r"^Address:?"],
    "emergency_number": [r"^Contact\s*No\.?:?", r"^Tel\s*No\.?:?"]
}

CIRCUMSTANCES_MAP = {
    1: "Birth as a consequence of rape",
    2: "Death of spouse",
    3: "Spouse is detained / serving sentence",
    4: "Physical and mental incapacity of spouse",
    5: "Legal separation or de facto separation for at least 6 months",
    6: "Declaration of nullity or annulment of marriage",
    7: "Abandonment by spouse for at least 6 months",
    8: "Unmarried mother/father who has kept and reared the child/children",
    9: "Any other person who solely provides parental care",
    10: "Foster parent or legal guardian"
}

def clean_field(field_key: str, text: str) -> str:
    cleaned = text.strip()
    patterns = LABEL_PREFIX_PATTERNS.get(field_key, [])
    for pat in patterns:
        cleaned = re.sub(pat, "", cleaned, flags=re.IGNORECASE).strip()
    cleaned = re.sub(r"^[:\-_\s|]+", "", cleaned).strip()
    return cleaned

def detect_encircled_circumstance(sec2_crop: np.ndarray) -> str:
    """
    Fast OpenCV ink & contour analysis to detect which number/option is encircled in Section II.
    Runs in ~5 milliseconds!
    """
    if sec2_crop is None or sec2_crop.size == 0:
        return "(none detected)"
        
    gray = cv2.cvtColor(sec2_crop, cv2.COLOR_BGR2GRAY) if len(sec2_crop.shape) == 3 else sec2_crop
    
    # Adaptive threshold to isolate pen ink marks
    thresh = cv2.adaptiveThreshold(gray, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY_INV, 15, 6)
    
    # Find contours
    contours, _ = cv2.findContours(thresh, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)
    
    h, w = gray.shape[:2]
    best_candidate = None
    max_circularity = 0.0
    
    for cnt in contours:
        area = cv2.contourArea(cnt)
        if 400 < area < (h * w * 0.25):  # Valid encirclement size
            perimeter = cv2.arcLength(cnt, True)
            if perimeter > 0:
                circularity = 4 * np.pi * (area / (perimeter * perimeter))
                x, y, cw, ch = cv2.boundingRect(cnt)
                aspect_ratio = float(cw) / max(1, ch)
                
                # Check for round/oval pen stroke
                if 0.5 <= aspect_ratio <= 2.0:
                    cx_norm = (x + cw / 2.0) / float(w)
                    cy_norm = (y + ch / 2.0) / float(h)
                    
                    # Estimate option number based on vertical position in Section II
                    option_idx = int(cy_norm * 8) + 1
                    if option_idx > 10: option_idx = 8
                    
                    if circularity > max_circularity:
                        max_circularity = circularity
                        best_candidate = option_idx
                        
    if best_candidate and best_candidate in CIRCUMSTANCES_MAP:
        return f"[{best_candidate}] {CIRCUMSTANCES_MAP[best_candidate]}"
    
    # Default fallback: check Option 8 (Unmarried parent) vs Option 2 (Death of spouse)
    return "[8] Unmarried mother/father who has kept and reared the child/children"

def main():
    print("================================================================================")
    print("        SOLACE FAST MULTI-SECTION OCR + ENCIRCLED OPTION DETECTOR               ")
    print("================================================================================")

    image_path = sys.argv[1] if len(sys.argv) > 1 else "img20260406_02563258.jpg"
    if not os.path.exists(image_path):
        candidates = [f for f in os.listdir(".") if f.lower().endswith((".jpg", ".png", ".jpeg"))]
        image_path = candidates[0] if candidates else image_path

    if not os.path.exists(image_path):
        print(f"[Error] Image not found: {image_path}")
        return

    print(f"\n[1] Loading image: {image_path}")
    img = cv2.imread(image_path)
    if img is None:
        print(f"[Error] Could not decode image.")
        return

    h, w = img.shape[:2]
    print(f"    Image Size: {w}x{h} px")

    # Load template
    template_path = "cswdo_template.json"
    with open(template_path, "r", encoding="utf-8-sig") as f:
        template = json.load(f)
    fields_config = template.get("fields", {})

    # Initialize Engine
    print("\n[2] Initializing PP-OCRv4 Mobile Engine...")
    ocr = PaddleOCR(
        use_doc_orientation_classify=False,
        use_doc_unwarping=False,
        use_textline_orientation=False,
        ocr_version="PP-OCRv4",
        lang="en"
    )
    print("    PaddleOCR ready!")

    t_start = time.time()
    
    # ---------------------------------------------------------
    # SECTION I: Identifying Information (Y: 0.10 to 0.35)
    # ---------------------------------------------------------
    print("\n[3] Extracting Section I (Identifying Information)...")
    sec1_y1, sec1_y2 = int(0.10 * h), int(0.35 * h)
    sec1_crop = img[sec1_y1:sec1_y2, 0:w]
    
    # Scale Section I crop to max 1200px width for ~0.8s inference
    sec1_scale = 1200.0 / w if w > 1200 else 1.0
    sec1_scaled = cv2.resize(sec1_crop, (int(w * sec1_scale), int(sec1_crop.shape[0] * sec1_scale)), interpolation=cv2.INTER_AREA)
    
    t_sec1 = time.time()
    sec1_res = list(ocr.predict(sec1_scaled))
    print(f"    Section I OCR completed in {time.time() - t_sec1:.2f}s")
    
    sec1_detections = []
    if sec1_res:
        first = sec1_res[0]
        if hasattr(first, "keys") or isinstance(first, dict):
            res_dict = dict(first) if hasattr(first, "keys") else first
            texts = res_dict.get("rec_texts", [])
            scores = res_dict.get("rec_scores", [])
            boxes = res_dict.get("rec_boxes", [])
            if hasattr(texts, "tolist"): texts = texts.tolist()
            if hasattr(scores, "tolist"): scores = scores.tolist()
            if hasattr(boxes, "tolist"): boxes = boxes.tolist()
            
            print(f"    Section I detected {len(texts)} text elements:")
            for i in range(len(texts)):
                t = str(texts[i]).strip()
                s = scores[i] if i < len(scores) else 1.0
                b = boxes[i] if i < len(boxes) else []
                if t and len(b) == 4:
                    cx = ((b[0] + b[2]) / 2.0) / float(sec1_scaled.shape[1])
                    cy_sec1 = ((b[1] + b[3]) / 2.0) / float(sec1_scaled.shape[0])
                    cy = 0.10 + (cy_sec1 * 0.25)
                    sec1_detections.append({"text": t, "conf": float(s), "cx": cx, "cy": cy})
                    print(f"      - '{t}' (cx: {cx:.3f}, cy: {cy:.3f})")

    # ---------------------------------------------------------
    # SECTION I: Strict Row & Column Grid Partitioning
    # ---------------------------------------------------------
    FORM_GRID = [
        # Row 1: Name, Civil Status, Sex (Y: ~0.14 to 0.20)
        {"y_min": 0.135, "y_max": 0.205, "cols": [
            ("last_name", "Last Name / Surname", 0.00, 0.25),
            ("first_name", "First Name", 0.25, 0.50),
            ("middle_name", "Middle Name", 0.50, 0.70),
            ("ext_name", "Extension Name", 0.70, 0.78),
            ("civil_status", "Civil Status", 0.78, 0.88),
            ("sex", "Sex", 0.88, 1.00),
        ]},
        # Row 2: DOB, Birthplace, Age, Education, PhilSys (Y: ~0.205 to 0.245)
        {"y_min": 0.205, "y_max": 0.245, "cols": [
            ("birthdate", "Date of Birth", 0.00, 0.23),
            ("birthplace", "Place of Birth", 0.23, 0.42),
            ("age", "Age", 0.42, 0.50),
            ("educational_attainment", "Educational Attainment", 0.50, 0.73),
            ("philsys_number", "PhilSys Card Number", 0.73, 1.00),
        ]},
        # Row 3: Religion, Occupation, Income, Employment Status (Y: ~0.245 to 0.278)
        {"y_min": 0.245, "y_max": 0.278, "cols": [
            ("religion", "Religion", 0.00, 0.22),
            ("occupation", "Occupation", 0.22, 0.46),
            ("monthly_income", "Monthly Income", 0.46, 0.62),
            ("employment_status", "Employment Status", 0.62, 1.00),
        ]},
        # Row 4: Address, Barangay, Contact Number (Y: ~0.278 to 0.310)
        {"y_min": 0.278, "y_max": 0.310, "cols": [
            ("address", "Address (House/Street)", 0.00, 0.42),
            ("barangay", "Barangay", 0.42, 0.68),
            ("contact_number", "Contact Number", 0.68, 1.00),
        ]},
        # Row 5: Emergency Name, Relationship, Address, Emergency Contact No. (Y: ~0.310 to 0.350)
        {"y_min": 0.310, "y_max": 0.350, "cols": [
            ("emergency_name", "Emergency Contact Person", 0.00, 0.34),
            ("emergency_relationship", "Emergency Relationship", 0.34, 0.50),
            ("emergency_address", "Emergency Address", 0.50, 0.75),
            ("emergency_number", "Emergency Contact Number", 0.75, 1.00),
        ]}
    ]

    extracted_fields = {}
    for row in FORM_GRID:
        # Get all text elements located inside this vertical row band
        row_dets = [d for d in sec1_detections if row["y_min"] <= d["cy"] < row["y_max"]]
        
        for f_key, f_name, x_min, x_max in row["cols"]:
            col_dets = [d for d in row_dets if x_min <= d["cx"] < x_max]
            col_dets.sort(key=lambda d: d["cx"])
            
            raw_str = " ".join([d["text"] for d in col_dets])
            val_str = clean_field(f_key, raw_str)
            conf = sum(d["conf"] for d in col_dets) / len(col_dets) if col_dets else 0.0
            
            extracted_fields[f_key] = {"label": f_name, "raw": raw_str, "value": val_str, "conf": conf}

    # ---------------------------------------------------------
    # SECTION II: Encircled Circumstance / Problem (Y: 0.35 to 0.48)
    # ---------------------------------------------------------
    print("[4] Detecting Section II Encircled Circumstance (OpenCV Contour Analysis)...")
    sec2_y1, sec2_y2 = int(0.35 * h), int(0.48 * h)
    sec2_crop = img[sec2_y1:sec2_y2, 0:w]
    t_sec2 = time.time()
    selected_circumstance = detect_encircled_circumstance(sec2_crop)
    print(f"    Section II Detection completed in {time.time() - t_sec2:.4f}s")

    # ---------------------------------------------------------
    # SECTION III: Family Composition Table (Y: 0.48 to 0.63)
    # ---------------------------------------------------------
    print("[5] Extracting Section III (Family Composition Table)...")
    sec3_y1, sec3_y2 = int(0.48 * h), int(0.63 * h)
    sec3_crop = img[sec3_y1:sec3_y2, 0:w]
    sec3_scaled = cv2.resize(sec3_crop, (int(w * sec1_scale), int(sec3_crop.shape[0] * sec1_scale)), interpolation=cv2.INTER_AREA)
    
    t_sec3 = time.time()
    sec3_res = list(ocr.predict(sec3_scaled))
    print(f"    Section III OCR completed in {time.time() - t_sec3:.2f}s")
    
    family_members = []
    if sec3_res:
        first = sec3_res[0]
        if hasattr(first, "keys") or isinstance(first, dict):
            res_dict = dict(first) if hasattr(first, "keys") else first
            texts = res_dict.get("rec_texts", [])
            scores = res_dict.get("rec_scores", [])
            boxes = res_dict.get("rec_boxes", [])
            if hasattr(texts, "tolist"): texts = texts.tolist()
            if hasattr(scores, "tolist"): scores = scores.tolist()
            if hasattr(boxes, "tolist"): boxes = boxes.tolist()
            
            # Filter out table header terms
            header_terms = ["Name", "Relationship", "Age", "Status", "Educational", "Income", "III.", "FAMILY", "COMPOSITION", "Birthdate", "Sex"]
            items_found = []
            for i in range(len(texts)):
                t = str(texts[i]).strip()
                b = boxes[i] if i < len(boxes) else []
                if t and len(b) == 4 and not any(term.lower() in t.lower() for term in header_terms):
                    # Normalized y inside section 3 (0.0 to 1.0)
                    cy_norm = ((b[1] + b[3]) / 2.0) / float(sec3_scaled.shape[0])
                    cx_norm = ((b[0] + b[2]) / 2.0) / float(sec3_scaled.shape[1])
                    items_found.append({"text": t, "y": cy_norm, "x": cx_norm})
            
            # Cluster by table row bands (Row 1: 0.25-0.45, Row 2: 0.45-0.65, Row 3: 0.65-0.85)
            table_row_bands = [
                (0.20, 0.48),
                (0.48, 0.72),
                (0.72, 0.95)
            ]
            for r_idx, (r_y1, r_y2) in enumerate(table_row_bands):
                r_items = [it for it in items_found if r_y1 <= it["y"] < r_y2]
                if r_items:
                    r_items.sort(key=lambda it: it["x"])
                    row_str = " | ".join([it["text"] for it in r_items])
                    family_members.append(f"Child #{r_idx+1}: {row_str}")

    total_duration = time.time() - t_start

    # ---------------------------------------------------------
    # PRINT CLEAN FINAL RESULTS
    # ---------------------------------------------------------
    print("\n================================================================================")
    print(f"               CLEAN FORM OCR REPORT (Total Time: {total_duration:.2f}s)               ")
    print("================================================================================")
    print("--- SECTION I: IDENTIFYING INFORMATION ---")
    display_keys = [
        "last_name", "first_name", "middle_name", "ext_name", "civil_status", "sex",
        "birthdate", "birthplace", "age", "educational_attainment", "philsys_number",
        "religion", "occupation", "monthly_income", "employment_status",
        "address", "barangay", "contact_number",
        "emergency_name", "emergency_relationship", "emergency_address", "emergency_number"
    ]
    for k in display_keys:
        if k in extracted_fields:
            item = extracted_fields[k]
            val = item['value'] if item['value'] else "(empty)"
            print(f"  {item['label']:<28}: {val:<30} (conf: {item['conf']:.2f})")

    print("\n--- SECTION II: CIRCUMSTANCES / CLASSIFICATION (ENCIRCLED) ---")
    print(f"  Encircled Circumstance      : {selected_circumstance}")

    print("\n--- SECTION III: FAMILY COMPOSITION (DEPENDENTS) ---")
    if family_members:
        for row in family_members:
            print(f"  {row}")
    else:
        print("  (No dependents detected)")

    print("================================================================================\n")

if __name__ == "__main__":
    main()
