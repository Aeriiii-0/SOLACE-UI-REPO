import os
import re
from datetime import datetime, date
import cv2
import numpy as np
from preprocess import crop_roi
from cleaners import (
    clean_text,
    is_ocr_na,
    clean_person_name,
    clean_sex,
    clean_family_age,
    clean_date,
    clean_civil_status,
    clean_relationship,
    clean_education,
    clean_income,
)
from inference import recognize_crop, recognize_with_server_model
from labels import assign_detections_to_field

def cell_has_ink(crop: np.ndarray, thresh_val: float = 0.015) -> bool:
    """Fast ink pre-check to skip OCR on empty cells in < 0.1ms."""
    if crop is None or crop.size == 0:
        return False
    gray = cv2.cvtColor(crop, cv2.COLOR_BGR2GRAY) if len(crop.shape) == 3 else crop
    h, w = gray.shape[:2]
    if h < 8 or w < 8:
        return False
    inner = gray[int(h * 0.15):int(h * 0.85), int(w * 0.08):int(w * 0.92)]
    if inner.size == 0:
        return False
    _, bin_img = cv2.threshold(inner, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
    ink_ratio = float(np.count_nonzero(bin_img)) / float(inner.size)
    return bool(ink_ratio > thresh_val)

CIRCUMSTANCES_TABLE = [
    {"code": "A1", "col": 0, "bbox": [0.030, 0.345, 0.045, 0.018], "label": "A1. Gives birth as a result of rape", "has_subfield": False},
    {"code": "A2", "col": 0, "bbox": [0.030, 0.365, 0.045, 0.018], "label": "A2. Death of Spouse", "has_subfield": True, "subfields": {"cause": [0.100, 0.365, 0.200, 0.018], "date": [0.320, 0.365, 0.150, 0.018]}},
    {"code": "A3", "col": 0, "bbox": [0.030, 0.385, 0.045, 0.018], "label": "A3. Detention of Spouse", "has_subfield": False},
    {"code": "A4", "col": 0, "bbox": [0.030, 0.405, 0.045, 0.018], "label": "A4. Physical & Mental Incapacity of Spouse", "has_subfield": True, "subfields": {"disability": [0.100, 0.405, 0.370, 0.018]}},
    {"code": "A5", "col": 0, "bbox": [0.030, 0.425, 0.045, 0.018], "label": "A5. Legal or de facto Separation", "has_subfield": True, "subfields": {"period": [0.100, 0.425, 0.370, 0.018]}},
    {"code": "A6", "col": 0, "bbox": [0.030, 0.445, 0.045, 0.018], "label": "A6. Declaration of nullity / annulment of marriage", "has_subfield": True, "subfields": {"nullity": [0.080, 0.445, 0.040, 0.018], "annulment": [0.220, 0.445, 0.040, 0.018]}},
    {"code": "A7", "col": 0, "bbox": [0.030, 0.465, 0.045, 0.018], "label": "A7. Abandonment of spouse for at least 6 months", "has_subfield": False},
    {"code": "B",  "col": 1, "bbox": [0.500, 0.345, 0.045, 0.022], "label": "B. Spouse or family member of an OFW", "has_subfield": True, "subfields": {"stay_abroad": [0.560, 0.345, 0.380, 0.022]}},
    {"code": "C",  "col": 1, "bbox": [0.500, 0.370, 0.045, 0.022], "label": "C. Unmarried Mother or Father", "has_subfield": False},
    {"code": "D",  "col": 1, "bbox": [0.500, 0.395, 0.045, 0.022], "label": "D. Legal Guardian / Adoptive / Foster Parent", "has_subfield": False},
    {"code": "E",  "col": 1, "bbox": [0.500, 0.420, 0.045, 0.022], "label": "E. Relative within 4th civil degree", "has_subfield": False},
    {"code": "F",  "col": 1, "bbox": [0.500, 0.445, 0.045, 0.022], "label": "F. Pregnant Woman", "has_subfield": False}
]

def detect_encircled_circumstance(enhanced_image: np.ndarray, ocr_engine=None, template: dict = None) -> dict:
    """
    Direct code & checkbox ROI detection for Section II.
    Scans the 12 code boxes exclusively, bypassing paragraph text completely.
    Uses dynamic bounding boxes from cswdo_template.json if calibrated by caseworker.
    """
    best_candidate = None
    max_score = 0.0
    fields_cfg = template.get("fields", {}) if template else {}

    for item in CIRCUMSTANCES_TABLE:
        code_key = f"circ_{item['code'].lower()}"
        bbox = fields_cfg.get(code_key, {}).get("bbox", item["bbox"])
        crop = crop_roi(enhanced_image, bbox)
        if crop is None or crop.size == 0:
            continue
        gray = cv2.cvtColor(crop, cv2.COLOR_BGR2GRAY) if len(crop.shape) == 3 else crop
        _, bin_img = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
        ink_ratio = float(np.count_nonzero(bin_img)) / float(bin_img.size)
        if ink_ratio > max_score and ink_ratio > 0.04:
            max_score = float(ink_ratio)
            best_candidate = (item, bbox)

    if best_candidate is not None:
        item, _ = best_candidate
        conf = float(min(0.95, round(0.70 + float(max_score) * 0.30, 2)))
        res = {
            "detected": True,
            "code": str(item["code"]),
            "label": str(item["label"]),
            "confidence": float(conf),
            "subfield_annulment": False
        }

        # Read subfields if present
        if item.get("has_subfield") and ocr_engine is not None:
            sub_defs = item.get("subfields", {})
            if "cause" in sub_defs:
                c_bbox = fields_cfg.get("circ_a2_cause", {}).get("bbox", sub_defs["cause"])
                c_crop = crop_roi(enhanced_image, c_bbox)
                if cell_has_ink(c_crop):
                    raw, _ = recognize_crop(ocr_engine, c_crop)
                    res["subfield_cause"] = clean_text(raw)
            if "date" in sub_defs:
                d_bbox = fields_cfg.get("circ_a2_date", {}).get("bbox", sub_defs["date"])
                d_crop = crop_roi(enhanced_image, d_bbox)
                if cell_has_ink(d_crop):
                    raw, _ = recognize_crop(ocr_engine, d_crop)
                    res["subfield_date"] = clean_date(raw) or clean_text(raw)
            if "disability" in sub_defs:
                dis_bbox = fields_cfg.get("circ_a4_disability", {}).get("bbox", sub_defs["disability"])
                dis_crop = crop_roi(enhanced_image, dis_bbox)
                if cell_has_ink(dis_crop):
                    raw, _ = recognize_crop(ocr_engine, dis_crop)
                    res["subfield_disability"] = clean_text(raw)
            if "period" in sub_defs:
                p_bbox = fields_cfg.get("circ_a5_period", {}).get("bbox", sub_defs["period"])
                p_crop = crop_roi(enhanced_image, p_bbox)
                if cell_has_ink(p_crop):
                    raw, _ = recognize_crop(ocr_engine, p_crop)
                    res["subfield_period"] = clean_text(raw)
            if "stay_abroad" in sub_defs:
                s_bbox = fields_cfg.get("circ_b_stay", {}).get("bbox", sub_defs["stay_abroad"])
                s_crop = crop_roi(enhanced_image, s_bbox)
                if cell_has_ink(s_crop):
                    raw, _ = recognize_crop(ocr_engine, s_crop)
                    res["subfield_stay_abroad"] = clean_text(raw)
            if item["code"] == "A6":
                null_bbox = fields_cfg.get("circ_a6_nullity", {}).get("bbox", [0.080, 0.445, 0.040, 0.018])
                annul_bbox = fields_cfg.get("circ_a6_annulment", {}).get("bbox", [0.220, 0.445, 0.040, 0.018])
                res["subfield_nullity"] = bool(cell_has_ink(crop_roi(enhanced_image, null_bbox)))
                res["subfield_annulment"] = bool(cell_has_ink(crop_roi(enhanced_image, annul_bbox)))

        return res

    return {"detected": False, "code": "", "label": "", "confidence": 0.0, "has_subfield": False, "subfield_annulment": False}

def measure_checkbox_ink(image: np.ndarray, bbox: list) -> float:
    """Measures checkmark / handwriting ink inside a specific checkbox ROI."""
    if bbox is None or len(bbox) < 4:
        return 0.0
    crop = crop_roi(image, bbox)
    if crop is None or crop.size == 0:
        return 0.0
    gray = cv2.cvtColor(crop, cv2.COLOR_BGR2GRAY) if len(crop.shape) == 3 else crop
    h, w = gray.shape[:2]
    if h < 4 or w < 4:
        return 0.0

    # Auto-detect the checkbox square contour inside crop if present
    thresh = cv2.threshold(gray, 200, 255, cv2.THRESH_BINARY_INV)[1]
    contours, _ = cv2.findContours(thresh, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)
    box_rect = None
    for c in contours:
        bx, by, bw, bh = cv2.boundingRect(c)
        aspect = float(bw) / float(max(1, bh))
        if bw >= w * 0.40 and bh >= h * 0.40 and 0.7 <= aspect <= 1.4:
            box_rect = (bx, by, bw, bh)
            break

    if box_rect:
        bx, by, bw, bh = box_rect
        pad_x = max(1, int(round(bw * 0.18)))
        pad_y = max(1, int(round(bh * 0.18)))
        inner = gray[by + pad_y : by + bh - pad_y, bx + pad_x : bx + bw - pad_x]
    else:
        pad_x = max(1, int(round(w * 0.20)))
        pad_y = max(1, int(round(h * 0.20)))
        inner = gray[pad_y : h - pad_y, pad_x : w - pad_x]

    if inner.size == 0:
        inner = gray

    # Checkmark / ink density measurement:
    paper_median = float(np.median(inner))
    if paper_median < 160:
        # Box interior is predominantly dark / filled with pen ink
        return 1.0
    thresh_val = min(195, int(paper_median - 25))
    ink_mask = inner < thresh_val
    return float(np.sum(ink_mask)) / float(inner.size)

def measure_checkbox_ink_neighborhood(image: np.ndarray, bbox: list, max_shift_px: int = 2) -> float:
    """Measures maximum checkbox ink across a small +/- max_shift_px window to handle minor scanning jitter."""
    if image is None or image.size == 0 or not bbox or len(bbox) < 4:
        return 0.0
    h, w = image.shape[:2]
    best_ink = measure_checkbox_ink(image, bbox)
    if best_ink >= 0.15:
        return best_ink

    bx, by, bw, bh = bbox
    dx_norm = float(max_shift_px) / float(max(1, w))
    dy_norm = float(max_shift_px) / float(max(1, h))
    for shift_x in [-dx_norm, dx_norm]:
        for shift_y in [-dy_norm, dy_norm]:
            cand_box = [max(0.0, bx + shift_x), max(0.0, by + shift_y), bw, bh]
            cand_ink = measure_checkbox_ink(image, cand_box)
            if cand_ink > best_ink:
                best_ink = cand_ink
    return best_ink

def detect_employment_status(enhanced_image, ocr_engine, fields_config: dict, all_detections: list = None, raw_text: str = "") -> tuple:
    """
    Detects which employment status checkbox is marked exclusively from the 3 checkbox ROIs:
    1. Checkbox: Employed (is_employed)
    2. Checkbox: Self-Employed (is_self_employed)
    3. Checkbox: Not Employed (is_not_employed)
    Bypasses broad text searches to prevent printed label interference.
    Returns: (status_string, confidence)
    """
    # Calibrated Checkbox Ink & Checkmark Symbol Analysis
    if enhanced_image is not None and enhanced_image.size > 0:
        box_emp = fields_config.get("is_employed", {}).get("bbox") or [0.6167, 0.2347, 0.0084, 0.0056]
        box_self = fields_config.get("is_self_employed", {}).get("bbox") or [0.7056, 0.2347, 0.0084, 0.0056]
        box_not = fields_config.get("is_not_employed", {}).get("bbox") or [0.8256, 0.2347, 0.0084, 0.0056]

        ink_emp = measure_checkbox_ink_neighborhood(enhanced_image, box_emp)
        ink_self = measure_checkbox_ink_neighborhood(enhanced_image, box_self)
        ink_not = measure_checkbox_ink_neighborhood(enhanced_image, box_not)

        rec_emp, _ = recognize_crop(ocr_engine, crop_roi(enhanced_image, box_emp)) if ocr_engine else ("", 0.0)
        rec_self, _ = recognize_crop(ocr_engine, crop_roi(enhanced_image, box_self)) if ocr_engine else ("", 0.0)
        rec_not, _ = recognize_crop(ocr_engine, crop_roi(enhanced_image, box_not)) if ocr_engine else ("", 0.0)

        mark_chars = {"v", "x", "1", "7", "o", "/", chr(92), "*", "c", "k", "y", "e", "s"}
        has_mark_emp = any(c in mark_chars for c in rec_emp.lower()) if rec_emp else False
        has_mark_self = any(c in mark_chars for c in rec_self.lower()) if rec_self else False
        has_mark_not = any(c in mark_chars for c in rec_not.lower()) if rec_not else False

        score_emp = ink_emp + (0.35 if has_mark_emp else 0.0)
        score_self = ink_self + (0.35 if has_mark_self else 0.0)
        score_not = ink_not + (0.35 if has_mark_not else 0.0)

        rec_emp_safe = rec_emp.encode('ascii', 'replace').decode('ascii')
        rec_self_safe = rec_self.encode('ascii', 'replace').decode('ascii')
        rec_not_safe = rec_not.encode('ascii', 'replace').decode('ascii')
        print(f"[Employment Checkbox Analysis] Employed: {score_emp:.3f} (ink:{ink_emp:.3f}), Self: {score_self:.3f} (ink:{ink_self:.3f}), Not: {score_not:.3f} (ink:{ink_not:.3f})")

        max_score = max(score_emp, score_self, score_not)
        if max_score >= 0.10:
            if score_not >= score_emp and score_not >= score_self:
                return "Not Employed", 0.95
            elif score_self >= score_emp and score_self >= score_not:
                return "Self-Employed", 0.95
            else:
                return "Employed", 0.95

    # Step C: Contextual deduction from Occupation / Income if available
    occ_val = str(fields_config.get("occupation", {}).get("value", "")).lower().strip()
    inc_val = str(fields_config.get("monthly_income", {}).get("value", "")).strip()
    if occ_val in ["none", "wala", "n/a", "no work", "unemployed", "housewife", "homemaker"] or inc_val in ["0", "0.0", "none"]:
        return "Not Employed", 0.75

    return "", 0.0

def parse_family_table_grid(ocr_engine, enhanced_image: np.ndarray, fields_config: dict, all_detections: list = None) -> list:
    """Extracts Section III/V Family Dependents Table rows with ink gating and strict N/A filtering."""
    family_members = []
    if ocr_engine is None:
        return family_members

    table_cfg = fields_config.get("family_composition_table", {})
    t_bbox = table_cfg.get("bbox", [0.025, 0.440, 0.940, 0.100])
    tb_x, tb_y, tb_w, tb_h = t_bbox

    row_count = 5
    hdr_ratio = 0.14
    avail_h = tb_h * (1.0 - hdr_ratio)
    r_step = avail_h / float(row_count)

    cols_default = [
        ("name", 0.033, 0.268),
        ("sex",  0.303, 0.050),
        ("age",  0.354, 0.050),
        ("dob",  0.405, 0.106),
        ("civ",  0.511, 0.093),
        ("rel",  0.604, 0.113),
        ("edu",  0.717, 0.175),
        ("inc",  0.891, 0.095)
    ]

    for r_idx in range(row_count):
        r_num = r_idx + 1
        r_top = tb_y + (tb_h * hdr_ratio) + (r_idx * r_step) + 0.001
        r_height = max(0.016, r_step - 0.003)

        def get_cell_text(col_name: str, col_idx: int) -> tuple:
            custom_key = f"fam_row{r_num}_{col_name}"
            bbox = [cols_default[col_idx][1], r_top, cols_default[col_idx][2], r_height]
            if custom_key in fields_config and "bbox" in fields_config[custom_key]:
                bbox = fields_config[custom_key]["bbox"]

            # Narrow column: shave ~2-3px inward horizontally to avoid clipping vertical table grid borders
            if col_name == "sex":
                x_margin = 0.004
                bbox = [bbox[0] + x_margin, bbox[1], max(0.005, bbox[2] - x_margin * 2.0), bbox[3]]

            raw_text = ""
            conf = 0.90
            # 1. Try matching from all_detections (Global OCR batch)
            if all_detections:
                raw_text, conf = assign_detections_to_field(custom_key, {"bbox": bbox}, all_detections)

            # 2. Fallback to crop OCR if not captured by detection
            if not raw_text:
                crop = crop_roi(enhanced_image, bbox)
                if cell_has_ink(crop):
                    raw_text, conf = recognize_crop(ocr_engine, crop)

            # 3. Two-Tier Hybrid Escalation for difficult handwriting in grid cells
            enable_tier2 = os.environ.get("ENABLE_TIER2_SERVER", "0") in ("1", "true", "True")
            cell_crop = crop_roi(enhanced_image, bbox)
            if enable_tier2 and cell_has_ink(cell_crop, thresh_val=0.012) and (conf < 0.75 or not raw_text):
                t2_raw, t2_score = recognize_with_server_model(cell_crop)
                if t2_raw and (t2_score > conf or not raw_text):
                    raw_text = t2_raw
                    conf = t2_score

            return clean_text(raw_text), conf

        # 1. Member Name (gatekeeper for the entire row)
        clean_m_name, name_conf = get_cell_text("name", 0)
        clean_m_name = clean_person_name(clean_m_name)
        header_filter = [
            "name", "pangalan", "kasarian", "sex", "age", "edad", "relasyon", "relationship",
            "family", "composition", "iii.", "iv.", "v.", "status", "income", "birthdate",
            "needs", "problems", "pangangailangan", "suliranin", "problema"
        ]
        if not clean_m_name or len(clean_m_name) < 2 or is_ocr_na(clean_m_name) or any(h in clean_m_name.lower() for h in header_filter):
            continue

        # 2. Sex
        raw_sex, sex_conf = get_cell_text("sex", 1)
        clean_m_sex = "" if is_ocr_na(raw_sex) else clean_sex(raw_sex)
        if is_ocr_na(clean_m_sex): clean_m_sex = ""

        # 3. Age
        raw_age, age_conf = get_cell_text("age", 2)
        clean_m_age = "" if is_ocr_na(raw_age) else clean_family_age(raw_age)

        # 4. Birthdate
        raw_dob, dob_conf = get_cell_text("dob", 3)
        clean_m_dob = "" if is_ocr_na(raw_dob) else (clean_date(raw_dob) or raw_dob)
        if is_ocr_na(clean_m_dob): clean_m_dob = ""

        # If Birthdate is valid, compute Age automatically if missing or misread
        # and format clean_m_dob as MM/DD/YYYY
        if clean_m_dob and len(clean_m_dob) == 10 and clean_m_dob[4] == '-' and clean_m_dob[7] == '-':
            try:
                dt = datetime.strptime(clean_m_dob, "%Y-%m-%d").date()
                today = date.today()
                calc_age = today.year - dt.year - ((today.month, today.day) < (dt.month, dt.day))
                if 0 <= calc_age <= 120:
                    clean_m_age = str(calc_age)
                clean_m_dob = f"{dt.month:02d}/{dt.day:02d}/{dt.year}"
            except Exception:
                pass
        elif clean_m_dob:
            m_slash = re.match(r'^(\d{1,2})[/\-](\d{1,2})[/\-](\d{2,4})$', clean_m_dob)
            if m_slash:
                p1, p2, p3 = m_slash.groups()
                yr = int(p3) if len(p3) == 4 else (2000 + int(p3) if int(p3) < 40 else 1900 + int(p3))
                m_val, d_val = int(p1), int(p2)
                if m_val > 12 and d_val <= 12: m_val, d_val = d_val, m_val
                if 1 <= m_val <= 12 and 1 <= d_val <= 31:
                    try:
                        today = date.today()
                        calc_age = today.year - yr - ((today.month, today.day) < (m_val, d_val))
                        if 0 <= calc_age <= 120:
                            clean_m_age = str(calc_age)
                        clean_m_dob = f"{m_val:02d}/{d_val:02d}/{yr}"
                    except Exception:
                        pass

        # 5. Civil Status
        raw_civ, civ_conf = get_cell_text("civ", 4)
        clean_m_civ = "" if is_ocr_na(raw_civ) else clean_civil_status(raw_civ)
        if is_ocr_na(clean_m_civ): clean_m_civ = ""

        # 6. Relationship
        raw_rel, rel_conf = get_cell_text("rel", 5)
        clean_m_rel = "" if is_ocr_na(raw_rel) else clean_relationship(raw_rel)
        if is_ocr_na(clean_m_rel): clean_m_rel = ""

        # 7. Education / Employment
        raw_edu, edu_conf = get_cell_text("edu", 6)
        clean_m_edu = "" if is_ocr_na(raw_edu) else clean_education(raw_edu)
        if is_ocr_na(clean_m_edu): clean_m_edu = ""

        # 8. Income
        raw_inc, inc_conf = get_cell_text("inc", 7)
        clean_m_inc = "0" if is_ocr_na(raw_inc) else (clean_income(raw_inc) or "0")

        # Cross-Field Semantic Inference: Sex from Relationship
        rel_upper = clean_m_rel.strip().upper()
        if not clean_m_sex or is_ocr_na(clean_m_sex):
            if any(k in rel_upper for k in ["SON", "ANAK NA LALAKI", "BROTHER", "FATHER", "NEPHEW", "GRANDSON", "BOY"]):
                clean_m_sex = "Male"
                sex_conf = max(sex_conf, 0.95)
            elif any(k in rel_upper for k in ["DAUGHTER", "ANAK NA BABAE", "SISTER", "MOTHER", "NIECE", "GRANDDAUGHTER", "GIRL"]):
                clean_m_sex = "Female"
                sex_conf = max(sex_conf, 0.95)

        # Infant/Toddler Education fallback
        if not clean_m_edu or is_ocr_na(clean_m_edu):
            try:
                if clean_m_age and int(clean_m_age) <= 4:
                    clean_m_edu = "None"
                    edu_conf = max(edu_conf, 0.90)
            except Exception:
                pass

        family_members.append({
            "memberName": clean_m_name,
            "sex": clean_m_sex,
            "age": clean_m_age,
            "birthdate": clean_m_dob,
            "civilStatus": clean_m_civ,
            "relationship": clean_m_rel,
            "educationEmployment": clean_m_edu,
            "income": clean_m_inc,
            "nameConf": round(name_conf, 2),
            "sexConf": round(sex_conf, 2),
            "ageConf": round(age_conf, 2),
            "dobConf": round(dob_conf, 2),
            "civConf": round(civ_conf, 2),
            "relConf": round(rel_conf, 2),
            "eduConf": round(edu_conf, 2),
            "incConf": round(inc_conf, 2)
        })

    return family_members
