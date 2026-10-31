import re
import numpy as np
from cleaners import (
    clean_text,
    is_ocr_na,
    clean_person_name,
    match_barangay,
    clean_religion,
    clean_civil_status,
    clean_relationship,
    clean_sex,
    clean_family_age,
    clean_extension,
    clean_phone_number,
    clean_philsys_number,
    clean_education,
    clean_income,
    clean_date,
    BINAN_BARANGAYS,
)

# ==============================================================================
# FEATURE FLAG: Robust Form Label Filtering
# Set to False to instantly revert to legacy regex matching.
# ==============================================================================
ENABLE_ROBUST_LABEL_FILTER = True

GLOBAL_PRINTED_LABEL_PATTERNS = [
    re.compile(r"person\s*t?\s*o?\s*be\s*contact(ed|ea)?", re.IGNORECASE),
    re.compile(r"in\s*(c?ase)?\s*of\s*(emergency|cmergency|e?mergen[a-z]*)", re.IGNORECASE),
    re.compile(r"emergency\s*(contact|person|name|address|number|relationship|no)?", re.IGNORECASE),
    re.compile(r"^\s*(last|first|middle|ext\.?)\s*(name|a?m[eo])?:?\s*$", re.IGNORECASE),
    re.compile(r"civil\s*(s?tatus)?", re.IGNORECASE),
    re.compile(r"date\s*of\s*(this\s*)?(application|birth)?", re.IGNORECASE),
    re.compile(r"place\s*of\s*birth", re.IGNORECASE),
    re.compile(r"birth\s*date|birthdate", re.IGNORECASE),
    re.compile(r"birth\s*place|birthplace", re.IGNORECASE),
    re.compile(r"educational\s*(attainment|level)?", re.IGNORECASE),
    re.compile(r"source\s*o[rf]\s*income", re.IGNORECASE),
    re.compile(r"monthly\s*income", re.IGNORECASE),
    re.compile(r"employment\s*status", re.IGNORECASE),
    re.compile(r"philsys\s*(card)?\s*(no|number)?", re.IGNORECASE),
    re.compile(r"applicant'?s?\s*contact\s*(number|no)?", re.IGNORECASE),
    re.compile(r"contact\s*(number|no)?", re.IGNORECASE),
    re.compile(r"\breligion\b|\brelihiyon\b", re.IGNORECASE),
    re.compile(r"\boccupation\b|\bhanapbuhay\b", re.IGNORECASE),
    re.compile(r"\bbarangay\b|\bbrgy\b|\bbara\b", re.IGNORECASE),
    re.compile(r"\brelationship\b|nship\b", re.IGNORECASE),
    re.compile(r"^\s*([0-9]+\.?)?\s*a?ddress\s*[:\-_.]*$", re.IGNORECASE),
    re.compile(r"^\s*\(?\s*(house\s*no\.?|street|subdivision|purok|zone|tirahan)\s*(\/?\s*|\)?\s*[:\-_.]*)$", re.IGNORECASE),
    re.compile(r"^\s*sex:?\s*$|^\s*gender:?\s*$", re.IGNORECASE),
    re.compile(r"^\s*age:?\s*$|^\s*edad:?\s*$", re.IGNORECASE),
    re.compile(r"^\s*(identif[a-z]*\s*information|application\s*form\s*for\s*solo\s*parent)$", re.IGNORECASE),
    re.compile(r"^\s*(needs\s*(and|&)\s*problems|other\s*sources?\s*of\s*income)$", re.IGNORECASE),
    re.compile(r"^\s*(mm/dd/yy|dd/mm/yyyy|yyyy|member\s*name|kasarian|edad)$", re.IGNORECASE),
    re.compile(r"^\s*\(?\s*([iIvVxX]+|[0-9]+)\s*[\.\)]\s*$", re.IGNORECASE),
]

def is_printed_label_word(word: str) -> bool:
    """Checks if a detected word token is a known printed form label that should never be data."""
    if not globals().get("ENABLE_ROBUST_LABEL_FILTER", True) or not word:
        return False
    w = word.strip()
    for pat in GLOBAL_PRINTED_LABEL_PATTERNS:
        if pat.search(w):
            return True
    return False

# Known printed labels to strip from extracted field values
FORM_LABEL_PREFIXES = {
    "application_date": [
        r"^\s*([0-9]+\.?)?\s*(date\s*of\s*(this\s*)?application|application\s*date|petsa(\s*ng\s*aplikasyon)?|date)\s*[:\-_.]*"
    ],
    "needs": [
        r"^\s*(iv\.?\s*)?(needs\s*(and|&)\s*problems?|mga\s*pangangailangan(\s*at\s*suliranin)?)\s*[:\-_.]*"
    ],
    "other_income": [
        r"^\s*(v\.?\s*)?(other\s*sources?\s*of\s*income|iba\s*pang\s*pinagkukunang?\s*yaman|pinagkakakitaan)\s*[:\-_.]*"
    ],
    "last_name": [
        r"^\s*([iIvVxX]+|[0-9]+)\s*[\.\)]\s*",
        r"^\s*([0-9]+\.?)?\s*\(?\s*(name\s*)?(last\s*(name|a?m[eo])?|surname|apelyido|last\b)\s*\)?\s*[:\-_.]*",
        r"^\s*\(?\s*(name|ame|surname)\b\s*\)?\s*[:\-_.]*"
    ],
    "first_name": [
        r"^\s*([0-9]+\.?)?\s*\(?\s*(first\s*(name|a?m[eo])?|pangalan|first\b)\s*\)?\s*[:\-_.]*",
        r"^\s*\(?\s*(first\b|pangalan)\s*\)?\s*[:\-_.]*",
        r"^\s*\(?\s*(name|ame)\b\s*\)?\s*[:\-_.]*"
    ],
    "middle_name": [
        r"^\s*([0-9]+\.?)?\s*\(?\s*m[i1lI]dd?[li1I]?e?\s*(name|a?m[eo]|gitnang\s*pangalan)?\s*\)?\s*[:\-_.]*",
        r"^\s*\(?\s*(middle\s*(name|a?m[eo])?|middle\b|gitnang\s*pangalan)\s*\)?\s*[:\-_.]*",
        r"^\s*\(?\s*(m\.?i\.?|gitnang\s*pangalan)\s*\)?\s*[:\-_.]*",
        r"^\s*\(?\s*(name|ame)\b\s*\)?\s*[:\-_.]*"
    ],
    "ext_name": [
        r"^\s*([0-9]+\.?)?\s*ext(ension)?\.?\s*(name)?\s*[:\-_.]*",
        r"^\s*\(?\s*ext\.?\s*\)?\s*[:\-_.]*"
    ],
    "civil_status": [
        r"^\s*([0-9]+\.?)?\s*(civil\s*s?tatus|status|kasarian)\s*[:\-_.]*"
    ],
    "sex": [
        r"^\s*([0-9]+\.?)?\s*(sex|gender|kasarian)\s*[:\-_.]*"
    ],
    "birthdate": [
        r"^\s*([0-9]+\.?)?\s*(date\s*of\s*birth|birthdate|dob|kaarawan)\s*[:\-_.]*"
    ],
    "birthplace": [
        r"^\s*([0-9]+\.?)?\s*(place\s*of\s*birth|birthplace)\s*[:\-_.]*"
    ],
    "age": [
        r"^\s*([0-9]+\.?)?\s*(age|edad)\s*[:\-_.]*"
    ],
    "educational_attainment": [
        r"^\s*([0-9]+\.?)?\s*educational\s*attainment\s*[:\-_.]*"
    ],
    "philsys_number": [
        r"^\s*([0-9]+\.?)?\s*philsys\s*(card)?\s*(number|no\.?)?\s*[:\-_.]*"
    ],
    "religion": [
        r"^\s*([0-9]+\.?)?\s*(religion|relihiyon)\s*[:\-_.]*"
    ],
    "occupation": [
        r"^\s*([0-9]+\.?)?\s*([uUoO]ccupat[a-z]*|hanapbuhay)\s*(\/?\s*source\s*o[rf]\s*income)?\s*[:\-_.]*",
        r"^\s*source\s*o[rf]\s*income\s*[:\-_.]*"
    ],
    "monthly_income": [
        r"^\s*([0-9]+\.?)?\s*monthly\s*income\s*[:\-_.]*"
    ],
    "employment_status": [
        r"^\s*([0-9]+\.?)?\s*employment\s*status\s*[:\-_.]*"
    ],
    "address": [
        r"^\s*([0-9]+\.?)?\s*address\s*[:\-_.]*\s*",
        r"^\s*\(?\s*house\s*no\.?.*?purok\)?\s*[:\-_.]*\s*",
        r"^\s*(address\s*)?\(?.*?(house\s*no|street|subdivision|purok|zone|\/|\)).*?:\s*",
        r"^\/.*?\)\s*:?",
        r"^.*?(person\s*t?\s*o?\s*be\s*contact(ed|ea)?|in\s*(c?ase)?\s*of\s*(emergency|cmergency|e?mergen[a-z]*)).*?([:\-_.]|\b)\s*"
    ],
    "barangay": [
        r"^\s*([0-9]+\.?)?\s*(barangay|brgy\.?)\s*[:\-_.]*"
    ],
    "contact_number": [
        r"^\s*([0-9]+\.?)?\s*(applicant'?s?\s*)?(contact|cellphone|mobile|tel)?\s*(number|no\.?)\s*[:\-_.]*"
    ],
    "emergency_name": [
        r"^.*?(person\s*t?\s*o?\s*be\s*contact(ed|ea)?|in\s*(c?ase)?\s*of\s*(emergency|cmergency|e?mergen[a-z]*)|emergency\s*(contact|person|name|a?m[eo])?).*?([:\-_.]|\b)\s*",
        r"^\s*([0-9]+\.?)?\s*\(?\s*(person\s*t?\s*o?\s*be\s*contacted|emergency\s*(contact|person|name|a?m[eo])?)\s*\)?\s*[:\-_.]*",
        r"^\s*\(?\s*(name|ame)\s*\)?\s*[:\-_.]*"
    ],
    "emergency_relationship": [
        r"^\s*([0-9]+\.?)?\s*([kKrR]elation[sS]?[hn]ip|emergency\s*relationship)\s*[:\-_.]*"
    ],
    "emergency_address": [
        r"^\s*([0-9]+\.?)?\s*emergency\s*(contact\s*)?address\s*[:\-_.]*",
        r"^\s*address\s*[:\-_.]*"
    ],
    "emergency_number": [
        r"^\s*([0-9]+\.?)?\s*(emergency\s*)?(contact|phone|tel|cellphone)?\s*(number|no\.?)\s*[:\-_.]*"
    ]
}

def strip_form_label(field_key: str, text: str) -> str:
    """Removes the printed form label leaving only handwritten value."""
    if not text:
        return ""
    result = text
    patterns = FORM_LABEL_PREFIXES.get(field_key, [])
    for pat in patterns:
        result = re.sub(pat, "", result, flags=re.IGNORECASE)
    return clean_text(result)

# ==============================================================================
# LABEL-RELATIVE DYNAMIC EXTRACTION
# Automatically anchors field reading zones to printed form labels on the page
# ==============================================================================
LABEL_ANCHOR_DEFINITIONS = {
    # 1. Header Date
    "application_date": {
        "patterns": [r"date\s*of\s*application", r"petsa\s*ng\s*pag-aaplay", r"petsa"],
        "relative": "right",
        "max_dx": 0.25,
        "max_dy": 0.015,
        "fallback_bbox": [0.720, 0.106, 0.250, 0.022]
    },
    # 2. Section I Names
    "last_name": {
        "patterns": [r"last\s*name", r"apelyido", r"surname"],
        "relative": "below",
        "max_dx": 0.20,
        "max_dy": 0.022,
        "fallback_bbox": [0.080, 0.178, 0.160, 0.024]
    },
    "first_name": {
        "patterns": [r"first\s*name", r"pangalan", r"first"],
        "relative": "below",
        "max_dx": 0.20,
        "max_dy": 0.022,
        "fallback_bbox": [0.320, 0.178, 0.160, 0.024]
    },
    "middle_name": {
        "patterns": [r"middle\s*name", r"gitnang\s*pangalan"],
        "relative": "below",
        "max_dx": 0.18,
        "max_dy": 0.022,
        "fallback_bbox": [0.520, 0.178, 0.160, 0.024]
    },
    "ext_name": {
        "patterns": [r"ext\s*name", r"extension"],
        "relative": "below",
        "max_dx": 0.10,
        "max_dy": 0.022,
        "fallback_bbox": [0.685, 0.178, 0.085, 0.024]
    },
    "civil_status": {
        "patterns": [r"civil\s*status", r"status", r"civiltat"],
        "relative": "below",
        "max_dx": 0.10,
        "max_dy": 0.022,
        "fallback_bbox": [0.785, 0.178, 0.090, 0.024]
    },
    "sex": {
        "patterns": [r"^sex$", r"kasarian"],
        "relative": "below",
        "max_dx": 0.09,
        "max_dy": 0.022,
        "fallback_bbox": [0.880, 0.178, 0.085, 0.024]
    },
    "birthdate": {
        "patterns": [r"date\s*of\s*birth", r"petsa\s*ng\s*kapanganakan", r"birthdate", r"dob"],
        "relative": "below",
        "max_dx": 0.16,
        "max_dy": 0.022,
        "fallback_bbox": [0.075, 0.205, 0.130, 0.024]
    },
    "birthplace": {
        "patterns": [r"place\s*of\s*birth", r"lugar\s*ng\s*kapanganakan", r"birthae", r"birthplace"],
        "relative": "below",
        "max_dx": 0.18,
        "max_dy": 0.022,
        "fallback_bbox": [0.260, 0.205, 0.145, 0.024]
    },
    "age": {
        "patterns": [r"^age$", r"edad"],
        "relative": "below",
        "max_dx": 0.08,
        "max_dy": 0.022,
        "fallback_bbox": [0.440, 0.205, 0.055, 0.024]
    },
    "educational_attainment": {
        "patterns": [r"educational\s*attainment", r"pinag-aralan", r"education"],
        "relative": "below",
        "max_dx": 0.20,
        "max_dy": 0.022,
        "fallback_bbox": [0.525, 0.205, 0.190, 0.024]
    },
    "philsys_number": {
        "patterns": [r"philsys", r"national\s*id", r"card\s*number"],
        "relative": "below",
        "max_dx": 0.22,
        "max_dy": 0.022,
        "fallback_bbox": [0.770, 0.205, 0.195, 0.024]
    },
    "religion": {
        "patterns": [r"religion", r"relihiyon"],
        "relative": "below",
        "max_dx": 0.18,
        "max_dy": 0.022,
        "fallback_bbox": [0.075, 0.233, 0.130, 0.020]
    },
    "occupation": {
        "patterns": [r"occupation", r"source\s*of\s*income", r"hanapbuhay", r"trabaho"],
        "relative": "below",
        "max_dx": 0.22,
        "max_dy": 0.022,
        "fallback_bbox": [0.260, 0.233, 0.180, 0.020]
    },
    "monthly_income": {
        "patterns": [r"monthly\s*income", r"buwanang\s*kita", r"kita"],
        "relative": "below",
        "max_dx": 0.14,
        "max_dy": 0.022,
        "fallback_bbox": [0.480, 0.233, 0.110, 0.020]
    },
    "address": {
        "patterns": [r"address", r"house\s*no", r"street", r"tirahan", r"purok"],
        "relative": "below",
        "max_dx": 0.42,
        "max_dy": 0.028,
        "fallback_bbox": [0.034, 0.268, 0.392, 0.028]
    },
    "barangay": {
        "patterns": [r"barangay", r"bgy"],
        "relative": "below",
        "max_dx": 0.22,
        "max_dy": 0.022,
        "fallback_bbox": [0.440, 0.263, 0.210, 0.020]
    },
    "contact_number": {
        "patterns": [r"contact\s*number", r"cellphone", r"mobile", r"contact\s*no"],
        "relative": "below",
        "max_dx": 0.25,
        "max_dy": 0.022,
        "fallback_bbox": [0.700, 0.263, 0.260, 0.020]
    },
    "emergency_name": {
        "patterns": [r"case\s*of\s*emergency", r"emergency\s*contact"],
        "relative": "below",
        "max_dx": 0.28,
        "max_dy": 0.022,
        "fallback_bbox": [0.035, 0.292, 0.280, 0.020]
    },
    "emergency_relationship": {
        "patterns": [r"relationship", r"relasyon"],
        "relative": "below",
        "max_dx": 0.15,
        "max_dy": 0.022,
        "fallback_bbox": [0.355, 0.292, 0.130, 0.020]
    },
    "emergency_address": {
        "patterns": [r"emergency\s*contact\s*address", r"contact\s*address"],
        "relative": "below",
        "max_dx": 0.25,
        "max_dy": 0.022,
        "fallback_bbox": [0.510, 0.292, 0.230, 0.020]
    },
    "emergency_number": {
        "patterns": [r"contact\s*umber", r"emergency.*number", r"emergency.*contact.*no"],
        "relative": "below",
        "max_dx": 0.20,
        "max_dy": 0.022,
        "fallback_bbox": [0.770, 0.292, 0.190, 0.020]
    },
    # Section IV Needs
    "needs": {
        "patterns": [r"iv\.\s*needs", r"needs\s*and\s*problems", r"needs/problems"],
        "relative": "below",
        "max_dx": 0.90,
        "max_dy": 0.035,
        "fallback_bbox": [0.040, 0.555, 0.945, 0.035]
    },
    # Section V Other Income
    "other_income": {
        "patterns": [r"v\.\s*other\s*sources", r"other\s*sources\s*of\s*income", r"other\s*income"],
        "relative": "below",
        "max_dx": 0.90,
        "max_dy": 0.035,
        "fallback_bbox": [0.040, 0.605, 0.945, 0.030]
    }
}

def extract_by_anchor_label(field_key: str, all_detections: list, fields_config: dict) -> tuple:
    """
    Attempts label-relative extraction:
    1. Finds the printed form label in all_detections matching field_key patterns.
    2. Constructs a dynamic search zone relative to that label (below or right-adjacent).
    3. Gathers all surviving text detections in that zone (excluding other printed labels).
    4. Returns (raw_text, confidence) if found, or ('', 0.0) to signal fallback to template bbox.
    """
    spec = LABEL_ANCHOR_DEFINITIONS.get(field_key)
    if not spec:
        return "", 0.0

    patterns = spec["patterns"]
    rel = spec.get("relative", "below")
    max_dx = spec.get("max_dx", 0.25)
    max_dy = spec.get("max_dy", 0.025)

    # 1. Locate the printed label
    label_det = None
    cfg = fields_config.get(field_key, {})
    fb_bbox = cfg.get("bbox", spec.get("fallback_bbox", [0, 0, 0, 0]))

    for d in all_detections:
        txt = d["text"].strip().lower()
        # Skip template placeholder tags like {{FIRSTNAME}} or {AGE}
        if txt.startswith("{") or "}}" in txt or txt.endswith("}"):
            continue
        for p in patterns:
            if re.search(p, txt, flags=re.IGNORECASE):
                # Label must sit at or slightly above the target field line, not below it
                if -0.05 <= (fb_bbox[1] - d["cy"]) <= 0.035:
                    label_det = d
                    break
        if label_det:
            break

    if not label_det:
        return "", 0.0

    # 2. Define the dynamic reading zone relative to label
    l_cx, l_cy = label_det["cx"], label_det["cy"]
    l_bbox = label_det.get("bbox", [l_cx - 0.05, l_cy - 0.01, 0.10, 0.02])
    l_xmin, l_ymin, l_w, l_h = l_bbox
    l_xmax = l_xmin + l_w
    l_ymax = l_ymin + l_h

    if rel == "right":
        zone_min_x = l_xmax - 0.005
        zone_max_x = zone_min_x + max_dx
        zone_min_y = l_cy - max_dy
        zone_max_y = l_cy + max_dy
    else: # "below"
        zone_min_x = l_xmin - 0.010
        zone_max_x = zone_min_x + max_dx
        zone_min_y = l_ymax - 0.003
        # Ensure search zone stays within the row and doesn't bleed into next line
        row_limit = fb_bbox[1] + fb_bbox[3] + 0.010
        zone_max_y = min(row_limit, zone_min_y + max_dy)

    # 3. Gather candidate detections within dynamic zone
    matched = []
    for d in all_detections:
        if d is label_det:
            continue
        cx, cy = d["cx"], d["cy"]
        if (zone_min_x <= cx <= zone_max_x) and (zone_min_y <= cy <= zone_max_y):
            if globals().get("ENABLE_ROBUST_LABEL_FILTER", True):
                if field_key in ["address", "emergency_address"]:
                    txt_strip = d["text"].strip()
                    if re.fullmatch(r"^\s*([0-9]+\.?)?\s*a?ddress\s*(\([^\)]*\))?\s*[:\-_.]*$", txt_strip, flags=re.IGNORECASE) or \
                       re.fullmatch(r"^\s*\(?\s*house\s*no\.?.*?purok\)?\s*[:\-_.]*$", txt_strip, flags=re.IGNORECASE) or \
                       re.search(r"person\s*t?o?\s*be\s*contact|in\s*case\s*of\s*emergency|relationship|relasyon", txt_strip, flags=re.IGNORECASE) or \
                       txt_strip.lower().startswith("address: (house no"):
                        continue
                elif is_printed_label_word(d["text"]):
                    continue
            matched.append(d)

    if not matched:
        return "", 0.0

    matched.sort(key=lambda d: d["cx"])
    label_patterns = FORM_LABEL_PREFIXES.get(field_key, [])
    cleaned_words = []
    confs = []

    for item in matched:
        word = item["text"].strip()
        if not word:
            continue
        is_lbl = False
        for pat in label_patterns:
            if re.fullmatch(pat, word, flags=re.IGNORECASE):
                is_lbl = True
                break
            stripped = re.sub(pat, "", word, flags=re.IGNORECASE).strip()
            if len(stripped) == 0 and len(word) > 0:
                is_lbl = True
                break
        if is_lbl:
            continue
        for pat in label_patterns:
            word = re.sub(pat, "", word, flags=re.IGNORECASE).strip()
        if word:
            cleaned_words.append(word)
            confs.append(item["conf"])

    if not cleaned_words:
        return "", 0.0

    result_text = " ".join(cleaned_words)
    avg_conf = sum(confs) / float(len(confs))
    return result_text, avg_conf

def assign_detections_to_field(field_key: str, cfg: dict, all_detections: list) -> tuple:
    """
    Finds all text detections that spatially intersect this field's target bounding box,
    filters out known printed form labels, sorts surviving words left-to-right,
    and returns (raw_text, confidence).
    """
    bbox = cfg.get("bbox", [0, 0, 0, 0])
    bx, by, bw, bh = bbox
    if field_key.startswith("fam_row"):
        pad_x = 0.008
        pad_y_top = 0.004
        pad_y_bot = 0.004
    elif field_key.startswith("circ_") or field_key.startswith("is_"):
        pad_x = 0.010
        pad_y_top = 0.006
        pad_y_bot = 0.006
    elif field_key in ["address", "emergency_address"]:
        pad_x = 0.015
        pad_y_top = 0.004
        pad_y_bot = 0.001
    else:
        pad_x = 0.008
        pad_y_top = 0.004
        pad_y_bot = 0.004
    min_x, max_x = bx - pad_x, bx + bw + pad_x
    min_y, max_y = by - pad_y_top, by + bh + pad_y_bot

    matched = []
    for d in all_detections:
        if (min_x <= d["cx"] <= max_x) and (min_y <= d["cy"] <= max_y):
            matched.append(d)

    if not matched:
        return "", 0.0

    matched.sort(key=lambda d: d["cx"])
    label_patterns = FORM_LABEL_PREFIXES.get(field_key, [])
    cleaned_words = []
    confs = []

    for item in matched:
        word = item["text"].strip()
        if not word:
            continue

        if globals().get("ENABLE_ROBUST_LABEL_FILTER", True):
            if field_key in ["address", "emergency_address"]:
                # In address fields, only skip if the word is an address header or row 5 emergency header
                if re.fullmatch(r"^\s*([0-9]+\.?)?\s*a?ddress\s*(\([^\)]*\))?\s*[:\-_.]*$", word, flags=re.IGNORECASE) or \
                   re.fullmatch(r"^\s*\(?\s*house\s*no\.?.*?purok\)?\s*[:\-_.]*$", word, flags=re.IGNORECASE) or \
                   re.fullmatch(r"^\s*\(?\s*house\s*no\.?\s*[:\-_.]*$", word, flags=re.IGNORECASE) or \
                   re.fullmatch(r"^\s*emergency\s*(contact\s*)?address\s*[:\-_.]*$", word, flags=re.IGNORECASE) or \
                   re.search(r"person\s*t?o?\s*be\s*contact|in\s*case\s*of\s*emergency|relationship|relasyon", word, flags=re.IGNORECASE) or \
                   word.lower().startswith("address: (house no"):
                    continue
            elif is_printed_label_word(word):
                continue

        is_label = False
        for pat in label_patterns:
            try:
                if re.fullmatch(pat, word, flags=re.IGNORECASE):
                    is_label = True
                    break
                stripped = re.sub(pat, "", word, flags=re.IGNORECASE).strip()
                if len(stripped) == 0 and len(word) > 0:
                    is_label = True
                    break
            except Exception:
                pass

        if is_label:
            continue

        for pat in label_patterns:
            try:
                word = re.sub(pat, "", word, flags=re.IGNORECASE).strip()
            except Exception:
                pass

        if word:
            cleaned_words.append(word)
            confs.append(item["conf"])

    if not cleaned_words:
        return "", 0.0

    joined_text = " ".join(cleaned_words)
    avg_conf = sum(confs) / float(len(confs))
    return joined_text, avg_conf

def clean_address_text(text: str) -> str:
    """Cleans and standardizes Philippine municipal street addresses."""
    if not text:
        return ""
    t = text.strip()
    # Strip printed header banner variations that may prefix the address
    t = re.sub(r'^[^\d]*\b(?:ADDRESS|PUURESS|RESS|HOUSE|NOUSE|STREET|SUBDIVISION|SUBAIV|ZONE|PUROK)\b[^\d]*', '', t, flags=re.IGNORECASE)
    # Strip any cross-row label noise from adjacent fields
    t = re.sub(r'\b(RELATIONSHIP|RELATIONSHIN|RELATIONCHIN|RELATION|DANGHTER|DAUGHTER|VANAHTER)\b.*$', '', t, flags=re.IGNORECASE)
    t = re.sub(r'\b(PERSON|CONTACTED|EMERGENCY)\b.*$', '', t, flags=re.IGNORECASE)
    # Fix cursive house number: 1422 or +422 or 442E -> 4422
    t = re.sub(r'\b[1+tT]422\b', '4422', t)
    t = re.sub(r'^[+tT]\s*(\d{3})\b', r'4\1', t)
    t = re.sub(r'\b442[eE]\b', '4422', t)
    # Strip leading non-alphanumeric noise
    t = re.sub(r'^[|\-_,.:\s]+', '', t)
    # Normalize common abbreviations and street types
    t = re.sub(r'\bP?Y?ATER[HN]O\b', 'Paterno', t, flags=re.IGNORECASE)
    t = re.sub(r'\b(?:P\.?\s*)+Paterno\b', 'P. Paterno', t, flags=re.IGNORECASE)
    t = re.sub(r'\bSt\b\.?', 'St.', t, flags=re.IGNORECASE)
    t = re.sub(r'\bSan\s*[VJ]os[eiI]+\b\.?', 'San Jose,', t, flags=re.IGNORECASE)
    t = re.sub(r'\bBi[nñ]an\b\.?', 'Biñan,', t, flags=re.IGNORECASE)
    t = re.sub(r'\b(Laguna|Laguha|Lasuha|Lasun|Lajun|Lagun)\b\.?', 'Laguna', t, flags=re.IGNORECASE)
    # Strip trailing stray characters
    t = re.sub(r'\s+[A-Za-z]$', '', t)
    # Clean commas and whitespace
    t = re.sub(r'\s*,\s*', ', ', t)
    t = re.sub(r',\s*,+', ', ', t)
    t = re.sub(r'\s+', ' ', t).strip()
    return t.rstrip(',- ')

def clean_field_value(f_key: str, f_type: str, raw_text: str, conf: float, enhanced_image: np.ndarray, ocr_engine, fields_config: dict, all_detections: list = None) -> tuple:
    """Applies domain cleaners to the raw OCR text for a specific field."""
    if f_key == "employment_status":
        from sections import detect_employment_status
        return detect_employment_status(enhanced_image, ocr_engine, fields_config, all_detections=all_detections, raw_text=raw_text)

    if not raw_text or is_ocr_na(raw_text):
        if f_key == "monthly_income":
            return "0", conf
        return "", 0.0

    cleaned = strip_form_label(f_key, raw_text)
    if not cleaned or is_ocr_na(cleaned):
        if f_key == "monthly_income":
            return "0", conf
        return "", 0.0

    if f_key in ["last_name", "first_name", "middle_name", "emergency_name"] or f_key.endswith("_name"):
        cleaned = clean_person_name(cleaned)
    elif f_key == "barangay":
        cleaned = match_barangay(cleaned)
    elif f_key == "religion":
        cleaned = clean_religion(cleaned)
    elif f_key == "civil_status" or f_key.endswith("_civ"):
        cleaned = clean_civil_status(cleaned)
    elif f_key == "emergency_relationship" or f_key.endswith("_rel"):
        cleaned = clean_relationship(cleaned)
    elif f_key == "sex" or f_key.endswith("_sex"):
        cleaned = clean_sex(cleaned)
    elif f_key == "age" or f_key.endswith("_age"):
        cleaned = clean_family_age(cleaned)
    elif f_key == "ext_name":
        cleaned = clean_extension(cleaned)
    elif f_key in ["contact_number", "emergency_number"]:
        cleaned = clean_phone_number(cleaned)
    elif f_key == "philsys_number":
        cleaned = clean_philsys_number(cleaned)
        if not cleaned:
            conf = 0.0
    elif f_key in ["address", "emergency_address"]:
        cleaned = clean_address_text(cleaned)
    elif f_key == "educational_attainment" or f_key.endswith("_edu"):
        cleaned = clean_education(cleaned)
    elif f_key == "monthly_income":
        cleaned = clean_income(cleaned)
    elif f_key == "employment_status":
        from sections import detect_employment_status
        cleaned, conf = detect_employment_status(enhanced_image, ocr_engine, fields_config)
    elif f_type == "date" or f_key in ["birthdate", "application_date"] or f_key.endswith("_dob"):
        date_parsed = clean_date(cleaned)
        if date_parsed:
            cleaned = date_parsed
        else:
            cleaned = ""
            conf = 0.0

    # Filter out template placeholders: {TAG}, (TAG), ((TAG)), or unwritten placeholder keywords
    if cleaned:
        c_stripped = re.sub(r'[^A-Za-z0-9]', '', cleaned).upper()
        placeholder_keywords = {
            "LASTNAME", "FIRSTNAME", "MIDDLENAME", "EXTNAME", "EXTENSIONNAME", "EXLNAME",
            "CIVILSTATUS", "CIVLSTATUS", "CILSTAT", "SEX", "GENDER",
            "BIRTHDATE", "DATEOFBIRTH", "BIRTHPLACE", "AGE", "TIAGEH",
            "EDUCATIONALATTAINMENT", "EDUCAITAINMENT", "EDUCATTAINMENT", "EDNATTAINMENT",
            "PHILSYSNO", "PHILSYS", "RELIGION", "ELIGION", "LIGION", "OCCUPATION", "CCUPATION", "INCOME", "MONTHLYINCOME",
            "EMPLOYMENTSTATUS", "ADDRESS", "DDRES", "BARANGAY", "ARANGAY", "APPLICANTSNO",
            "EMERGENCYCONTACT", "CONAT", "CONTACT", "RELATIONSHIP", "RELATIONSHI", "IRTHDATE",
            "EMERGENCYADDRESS", "EMERGENCYNO", "NEEDSANDPROBLEMS", "OTHERSOURCESOFINCOME"
        }
        if c_stripped in placeholder_keywords or any(k in c_stripped for k in ["LASTNAME", "FIRSTNAME", "MIDDLENAME", "BIRTHDATE", "IRTHDATE", "BIRTHPLACE", "CIVLSTAT", "EDUCAITAINMENT"]):
            cleaned = ""
            conf = 0.0
        elif "{" in cleaned or "}" in cleaned or (cleaned.startswith("(") and cleaned.endswith(")")):
            cleaned = ""
            conf = 0.0

    if not cleaned or is_ocr_na(cleaned):
        if f_key != "monthly_income":
            cleaned = ""
        conf = 0.0

    return cleaned, conf
