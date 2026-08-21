import os
os.environ["PADDLE_PDX_ENABLE_MKLDNN_BYDEFAULT"] = "False"
os.environ["PADDLE_PDX_USE_PIR_TRT"] = "False"
os.environ["FLAGS_enable_pir_api"] = "0"
os.environ["FLAGS_use_mkldnn"] = "0"

import re
import json
import difflib
import cv2
import numpy as np
from typing import Dict, Any, List
from preprocess import crop_roi

# Official list of 24 Barangays of the City of Biñan
BINAN_BARANGAYS = [
    "Biñan Poblacion", "Bungahan", "Canlalay", "Casile", "De La Paz",
    "Ganado", "Langkiwa", "Loma", "Malaban", "Malamig", "Mamplasan",
    "Platero", "Poblacion", "San Antonio", "San Francisco (Halang)",
    "San Jose", "San Vicente", "Santo Niño", "Santo Tomas (Calabuso)",
    "Soro-Soro", "Timbao", "Tubigan", "Zapote"
]

# Aliases and common handwriting / OCR misreadings
BARANGAY_ALIASES = {
    "halang": "San Francisco (Halang)",
    "san francisco": "San Francisco (Halang)",
    "san fran": "San Francisco (Halang)",
    "calabuso": "Santo Tomas (Calabuso)",
    "sto tomas": "Santo Tomas (Calabuso)",
    "sto. tomas": "Santo Tomas (Calabuso)",
    "sto tomas calabuso": "Santo Tomas (Calabuso)",
    "sto. tomas calabuso": "Santo Tomas (Calabuso)",
    "santo tomas": "Santo Tomas (Calabuso)",
    "sto nino": "Santo Niño",
    "sto. nino": "Santo Niño",
    "sto niño": "Santo Niño",
    "sto. niño": "Santo Niño",
    "santo nino": "Santo Niño",
    "delapaz": "De La Paz",
    "dela paz": "De La Paz",
    "mampalasan": "Mamplasan",
    "mamplasan": "Mamplasan",
    "sorosoro": "Soro-Soro",
    "soro soro": "Soro-Soro",
    "binan poblacion": "Biñan Poblacion",
    "binan": "Biñan Poblacion",
    "poblacion": "Poblacion",
    "pob": "Poblacion",
}

def normalize_for_matching(text: str) -> str:
    """Normalizes text for robust comparison (lowercase, ñ->n, stripped punctuation)."""
    if not text:
        return ""
    t = text.lower().replace("ñ", "n")
    t = re.sub(r'[^a-z0-9\s]', ' ', t)
    return re.sub(r'\s+', ' ', t).strip()

def load_template() -> dict:
    """Loads the CSWDO-066-2 form template configuration."""
    template_path = os.path.join(os.path.dirname(__file__), "cswdo_template.json")
    if os.path.exists(template_path):
        with open(template_path, "r", encoding="utf-8-sig") as f:
            return json.load(f)
    return {"fields": {}}

def clean_text(text: str) -> str:
    """Removes stray OCR artifacts and trims whitespace."""
    if not text:
        return ""
    cleaned = re.sub(r'[\r\n\t_]+', ' ', text)
    cleaned = re.sub(r'\s+', ' ', cleaned)
    return cleaned.strip()

def match_barangay(raw_text: str) -> str:
    """
    Performs fuzzy matching on OCR extracted text against official Biñan barangays.
    Ensures OCR output is first matched using fuzzy similarity before being returned.
    """
    if not raw_text:
        return ""
    
    cleaned = clean_text(raw_text)
    norm_raw = normalize_for_matching(cleaned)
    if not norm_raw:
        return cleaned

    # 1. Exact alias match
    if norm_raw in BARANGAY_ALIASES:
        return BARANGAY_ALIASES[norm_raw]

    # 2. Check if alias is contained in or contains the input
    for alias, canonical in BARANGAY_ALIASES.items():
        if alias == norm_raw or (len(alias) >= 4 and alias in norm_raw):
            return canonical

    # 3. Direct substring match against canonical barangays & core names
    for brgy in BINAN_BARANGAYS:
        norm_brgy = normalize_for_matching(brgy)
        if norm_brgy == norm_raw:
            return brgy
        core_name = normalize_for_matching(brgy.split('(')[0].strip())
        if core_name and (core_name == norm_raw or (len(core_name) >= 5 and core_name in norm_raw)):
            return brgy

    # 4. Fuzzy SequenceMatcher comparison across all barangays and aliases
    best_match = None
    best_score = 0.0

    eval_pool = []
    for brgy in BINAN_BARANGAYS:
        eval_pool.append((normalize_for_matching(brgy), brgy))
        core = normalize_for_matching(brgy.split('(')[0].strip())
        if core and core != normalize_for_matching(brgy):
            eval_pool.append((core, brgy))
    for alias, canonical in BARANGAY_ALIASES.items():
        eval_pool.append((alias, canonical))

    for term, canonical in eval_pool:
        ratio = difflib.SequenceMatcher(None, norm_raw, term).ratio()
        if norm_raw.startswith(term) or term.startswith(norm_raw):
            ratio = max(ratio, 0.82)
        if ratio > best_score:
            best_score = ratio
            best_match = canonical

    # If fuzzy match score is sufficiently high (>= 0.60), return canonical barangay
    if best_match and best_score >= 0.60:
        return best_match

    return cleaned

def is_ocr_na(raw_text: str) -> bool:
    """Checks if raw text represents N/A or empty/none in noisy handwritten OCR."""
    if not raw_text:
        return True
    t = raw_text.strip().upper()
    t_no_punct = re.sub(r'[^A-Z0-9]', '', t)
    if t in ["N/A", "NA", "NLA", "N\\A", "N|A", "N1A", "NIA", "NLA", "N-A", "N_A", "N A", "N.A.", "N.A", "NONE", "NONE.", "WALA", "-", "--", "—", "N/a", "na", "nla", "0", "0.00"]:
        return True
    if t_no_punct in ["NA", "NLA", "NIA", "N1A", "NONE", "WALA"]:
        return True
    return False

def clean_income(raw_text: str) -> str:
    """Extracts numeric monthly income value. If N/A, None, or empty, returns '0'."""
    if not raw_text or is_ocr_na(raw_text):
        return "0"
    num = re.sub(r'[^\d.]', '', raw_text)
    if not num:
        return "0"
    try:
        val = float(num)
        return str(int(val)) if val.is_integer() else f"{val:.2f}"
    except Exception:
        return num

def clean_civil_status(raw_text: str) -> str:
    """Fuzzy normalization of Civil Status, including single letters (S, M, W, D, A, etc.)."""
    if not raw_text or is_ocr_na(raw_text):
        return ""
    t = raw_text.strip().lower()
    t_clean = re.sub(r'[^a-z]', '', t)
    
    # 1. Single letter or exact abbreviations
    if t in ["s", "s.", "s-", "sin", "sngl", "simgle", "simglt", "singl"] or t_clean in ["s", "sin", "sngl", "simgle", "simglt", "singl"]:
        return "Single"
    if t in ["m", "m.", "m-", "mar", "marr", "mared", "marrd"] or t_clean in ["m", "mar", "marr", "mared", "marrd"]:
        return "Married"
    if t in ["w", "w.", "w-", "wid", "wdw", "biyuda", "biyudo", "balo"] or t_clean in ["w", "wid", "wdw"]:
        return "Widowed"
    if t in ["d", "d.", "d-", "div"] or t_clean in ["d", "div"]:
        return "Divorced"
    if t in ["a", "a.", "a-", "anul", "anulled", "annul"] or t_clean in ["a", "anul", "anulled", "annul"]:
        return "Annulled"
    if t in ["sep", "sep.", "hiwalay"] or t_clean == "sep":
        return "Separated"
    if t in ["c", "c.", "live", "livein", "kinakasama"] or t_clean in ["c", "live", "livein"]:
        return "Common-law / Live-in"

    # 2. Keyword containment
    if any(k in t for k in ["single", "binata", "dalaga", "walang asawa"]):
        return "Single"
    if any(k in t for k in ["married", "kasal", "may asawa"]):
        return "Married"
    if any(k in t for k in ["widow", "widower", "balo", "biyud"]):
        return "Widowed"
    if any(k in t for k in ["separat", "hiwalay"]):
        return "Separated"
    if any(k in t for k in ["divorc"]):
        return "Divorced"
    if any(k in t for k in ["annul", "nullity"]):
        return "Annulled"
    if any(k in t for k in ["common", "live-in", "live in", "kinakasama"]):
        return "Common-law / Live-in"

    # 3. Fuzzy similarity
    for standard in ["Single", "Married", "Widowed", "Separated", "Divorced", "Annulled"]:
        if difflib.SequenceMatcher(None, t_clean, standard.lower()).ratio() >= 0.65:
            return standard

    val = clean_text(raw_text)
    return val if val else ""

def clean_relationship(raw_text: str) -> str:
    """Fuzzy normalization of Relationship (Emergency Contact & Dependents)."""
    if not raw_text or is_ocr_na(raw_text):
        return ""
    t = raw_text.strip().lower()
    t_clean = re.sub(r'[^a-z]', '', t)
    
    # Direct / keyword containment
    if any(k in t for k in ["sister", "sis", "ate", "kapatid na babae", "sstr", "sistr"]):
        return "Sister"
    if any(k in t for k in ["brother", "bro", "kuya", "kapatid na lalaki", "brthr", "brothr"]):
        return "Brother"
    if any(k in t for k in ["mother", "ina", "nanay", "mom", "mama", "mami", "nay", "mthr"]):
        return "Mother"
    if any(k in t for k in ["father", "ama", "tatay", "dad", "papa", "papi", "tay", "fthr"]):
        return "Father"
    if any(k in t for k in ["daughter", "dauglster", "dau", "dtr", "daugh", "anak na babae"]):
        return "Daughter"
    if any(k in t for k in ["son", "anak na lalaki"]):
        return "Son"
    if any(k in t for k in ["child", "anak", "bata"]):
        return "Child"
    if any(k in t for k in ["spouse", "asawa", "husband", "wife", "partner"]):
        return "Spouse"
    if any(k in t for k in ["aunt", "auntie", "tita", "tiya"]):
        return "Aunt"
    if any(k in t for k in ["uncle", "tito", "tiyo"]):
        return "Uncle"
    if any(k in t for k in ["cousin", "pinsan", "cosin", "cous"]):
        return "Cousin"
    if any(k in t for k in ["grandmother", "lola", "grandma", "inang"]):
        return "Grandmother"
    if any(k in t for k in ["grandfather", "lolo", "grandpa", "amang"]):
        return "Grandfather"
    if any(k in t for k in ["in-law", "in law", "biyanan", "hipag", "bayaw", "manugang"]):
        return "In-law"
    if any(k in t for k in ["friend", "kaibigan"]):
        return "Friend"
    if any(k in t for k in ["neighbor", "kapitbahay"]):
        return "Neighbor"
    if any(k in t for k in ["guardian", "tagapangalaga"]):
        return "Guardian"

    relationships = ["Mother", "Father", "Sister", "Brother", "Son", "Daughter", "Child", "Spouse", "Aunt", "Uncle", "Cousin", "Grandmother", "Grandfather", "Friend", "Neighbor", "Guardian"]
    for rel in relationships:
        if difflib.SequenceMatcher(None, t_clean, rel.lower()).ratio() >= 0.65:
            return rel

    val = clean_text(raw_text)
    return val if val else "Others"

MONTH_MAP = {
    "january": 1, "jan": 1, "janu": 1, "jau": 1,
    "february": 2, "feb": 2, "febr": 2,
    "march": 3, "mar": 3, "marc": 3,
    "april": 4, "apr": 4, "apri": 4, "apl": 4,
    "may": 5,
    "june": 6, "jun": 6, "jum": 6, "jne": 6,
    "july": 7, "jul": 7, "jly": 7,
    "august": 8, "aug": 8, "augu": 8, "agst": 8,
    "september": 9, "sep": 9, "sept": 9, "set": 9,
    "october": 10, "oct": 10, "octo": 10, "okt": 10,
    "november": 11, "nov": 11, "nove": 11,
    "december": 12, "dec": 12, "dece": 12, "dek": 12
}

def parse_month_word(word: str):
    """Matches a written month or OCR misspelling (e.g. 'Jum' -> 6 for June)."""
    if not word:
        return None
    w = word.lower().strip()
    if w in MONTH_MAP:
        return MONTH_MAP[w]
    best_m, best_s = None, 0.0
    for key, val in MONTH_MAP.items():
        score = difflib.SequenceMatcher(None, w, key).ratio()
        if score > best_s:
            best_s, best_m = score, val
    if best_m and best_s >= 0.60:
        return best_m
    return None

def clean_date(raw_text: str) -> str:
    """
    Extracts and normalizes dates from handwritten or printed text.
    Handles 'June 19, 2004', OCR merged 'Jum192004', '19 June 2004', MM/DD/YYYY, YYYY-MM-DD, MMDDYYYY, MMDDYY.
    Returns normalized YYYY-MM-DD or empty string '' if no valid date is found.
    """
    if not raw_text:
        return ""
    raw = raw_text.strip()
    
    # Strip common label prefixes
    raw = re.sub(r'^(date\s*of\s*birth|birthdate|dob|kaarawan|bday)\s*[:\-_.]*', '', raw, flags=re.IGNORECASE).strip()

    # 1. Numeric with separators: MM/DD/YYYY, YYYY-MM-DD, DD/MM/YYYY, MM/DD/YY
    m1 = re.search(r'(\d{1,4})[/\-.](\d{1,2})[/\-.](\d{2,4})', raw)
    if m1:
        p1, p2, p3 = m1.groups()
        if len(p1) == 4 and int(p1) >= 1920:  # YYYY-MM-DD
            if 1 <= int(p2) <= 12 and 1 <= int(p3) <= 31:
                return f"{p1}-{int(p2):02d}-{int(p3):02d}"
        else:
            yr = p3 if len(p3) == 4 else f"20{p3}" if int(p3) < 40 else f"19{p3}"
            if int(p1) > 12 and int(p2) <= 12 and int(p1) <= 31:
                return f"{yr}-{int(p2):02d}-{int(p1):02d}"
            elif int(p1) <= 12 and int(p2) <= 31:
                return f"{yr}-{int(p1):02d}-{int(p2):02d}"

    # 2. Text month + day + year (e.g. 'June 19, 2004', 'Jun 19 2004', 'Jum192004', 'June192004')
    clean = re.sub(r'[^a-zA-Z0-9\s]', ' ', raw)

    # Month word followed by day and year (e.g. 'Jum192004' or 'June 19 2004')
    m_concat = re.search(r'([a-zA-Z]{3,})\s*(\d{1,2})\s*(\d{4}|\d{2})', clean)
    if m_concat:
        m_word, day_str, yr_str = m_concat.groups()
        m_num = parse_month_word(m_word)
        if m_num and 1 <= int(day_str) <= 31:
            yr = yr_str if len(yr_str) == 4 else f"20{yr_str}" if int(yr_str) < 40 else f"19{yr_str}"
            return f"{yr}-{m_num:02d}-{int(day_str):02d}"

    # Day followed by Month word and year (e.g. '19 June 2004')
    m_day_first = re.search(r'(\d{1,2})\s*([a-zA-Z]{3,})\s*(\d{4}|\d{2})', clean)
    if m_day_first:
        day_str, m_word, yr_str = m_day_first.groups()
        m_num = parse_month_word(m_word)
        if m_num and 1 <= int(day_str) <= 31:
            yr = yr_str if len(yr_str) == 4 else f"20{yr_str}" if int(yr_str) < 40 else f"19{yr_str}"
            return f"{yr}-{m_num:02d}-{int(day_str):02d}"

    # 3. Pure 8-digit or 6-digit string
    digits_only = re.sub(r'\D', '', raw)
    if len(digits_only) == 8:
        y_cand, m_cand, d_cand = digits_only[:4], digits_only[4:6], digits_only[6:]
        if 1920 <= int(y_cand) <= 2030 and 1 <= int(m_cand) <= 12 and 1 <= int(d_cand) <= 31:
            return f"{y_cand}-{int(m_cand):02d}-{int(d_cand):02d}"
        m_s, d_s, y_s = digits_only[:2], digits_only[2:4], digits_only[4:]
        if 1 <= int(m_s) <= 12 and 1 <= int(d_s) <= 31 and 1920 <= int(y_s) <= 2030:
            return f"{y_s}-{int(m_s):02d}-{int(d_s):02d}"
            
    if len(digits_only) == 6:
        m_s, d_s, y_s = digits_only[:2], digits_only[2:4], digits_only[4:]
        if 1 <= int(m_s) <= 12 and 1 <= int(d_s) <= 31:
            yr = f"20{y_s}" if int(y_s) < 40 else f"19{y_s}"
            return f"{yr}-{int(m_s):02d}-{int(d_s):02d}"

    return ""

def clean_sex(raw_text: str) -> str:
    """Fuzzy normalization of Sex field."""
    if not raw_text:
        return "—"
    t = raw_text.strip().lower()
    if t in ["m", "male", "lalaki", "m.", "boy", "son", "father", "brother", "l"]:
        return "Male"
    if t in ["f", "female", "babae", "fem", "f.", "girl", "daughter", "mother", "sister"]:
        return "Female"
    if re.search(r'\b(female|babae|fem)\b', t) or t.startswith('f'):
        return "Female"
    if re.search(r'\b(male|lalaki)\b', t) or t.startswith('m'):
        return "Male"
    return clean_text(raw_text)

def clean_phone_number(raw_text: str) -> str:
    """
    Normalizes Philippine mobile/contact numbers to standard 11-digit format (09XXXXXXXXX).
    Handles +639..., 639..., 9XXXXXXXXX, spaces, and dashes.
    """
    if not raw_text:
        return ""
    digits = re.sub(r'\D', '', raw_text)
    if digits.startswith("63") and len(digits) == 12:
        return "0" + digits[2:]
    if digits.startswith("9") and len(digits) == 10:
        return "0" + digits
    return digits if digits else clean_text(raw_text)

def clean_extension(raw_text: str) -> str:
    """Fuzzy normalization of Extension Name to predetermined list (Jr., Sr., II, III, IV, V)."""
    if not raw_text:
        return ""
    t = raw_text.strip().upper()
    if t in ["N/A", "NONE", "NA", "-", "--", "NO", "N / A", "N.A.", "N.A", "NONE.", "WALA"]:
        return ""
    
    t = re.sub(r'\b(EXT|EXTENSION|NAME)\b', '', t).strip()
    if not t:
        return ""

    if re.search(r'\b(JR\.?|JUNIOR|J\.R\.?|JR_)\b', t) or t in ["JR", "JR.", "J.R.", "J.R", "JR_"]:
        return "Jr."
    if re.search(r'\b(SR\.?|SENIOR|S\.R\.?)\b', t) or t in ["SR", "SR.", "S.R.", "S.R"]:
        return "Sr."
    if re.search(r'\b(III|111|LLL|\|\|\||I/I|3RD|THIRD)\b', t) or t in ["III", "111", "LLL", "I I I"]:
        return "III"
    if re.search(r'\b(II|11|LL|\|\||2ND|SECOND)\b', t) or t in ["II", "11", "LL", "I I"]:
        return "II"
    if re.search(r'\b(IV|1V|4TH|FOURTH)\b', t) or t in ["IV", "1V"]:
        return "IV"
    if re.search(r'\b(V|5TH|FIFTH)\b', t) or t == "V":
        return "V"

    return ""

def clean_religion(raw_text: str) -> str:
    """Fuzzy normalizes religion to standard list or Others."""
    if not raw_text:
        return "Others"
    t = raw_text.strip().lower()
    t_clean = re.sub(r'[^a-z0-9]', '', t)
    
    # 1. Direct and fuzzy keyword matches for Roman Catholic (Ctolc, Catholic, Katoliko, RC, etc.)
    cath_keywords = ["cath", "katol", "roman", "rc", "ctolc", "catol", "katlk", "cthlc", "cathl", "cato", "kato", "ctc"]
    if any(k in t_clean for k in cath_keywords) or any(k in t for k in ["catholic", "katoliko", "roman catholic", "r.c.", "r.c"]):
        return "Roman Catholic"
        
    if difflib.SequenceMatcher(None, t_clean, "catholic").ratio() >= 0.45 or \
       difflib.SequenceMatcher(None, t_clean, "katoliko").ratio() >= 0.45:
        return "Roman Catholic"

    # 2. Islam / Muslim
    if any(k in t_clean for k in ["islam", "muslim", "moslem", "mslm", "islaam"]):
        return "Islam"
        
    # 3. Iglesia ni Cristo
    if any(k in t_clean for k in ["inc", "iglesia", "cristo", "kristo"]):
        return "Iglesia ni Cristo"
        
    # 4. Seventh-day Adventist
    if any(k in t_clean for k in ["advent", "sda", "seventh", "7th"]):
        return "Seventh-day Adventist"
        
    # 5. Evangelical / Born Again
    if any(k in t_clean for k in ["born", "evangel", "again", "christian"]):
        return "Evangelical / Born Again"
        
    # 6. Jehovah's Witnesses
    if any(k in t_clean for k in ["jehovah", "witness", "jw", "saksi"]):
        return "Jehovah's Witnesses"
        
    # 7. Baptist
    if any(k in t_clean for k in ["baptist", "bautis", "bap"]):
        return "Baptist"
        
    # 8. Methodist
    if any(k in t_clean for k in ["methodist", "metod"]):
        return "Methodist"

    val = clean_text(raw_text)
    return val if val else "Others"

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
    ink_ratio = np.count_nonzero(bin_img) / float(inner.size)
    return ink_ratio > thresh_val

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
        ink_ratio = np.count_nonzero(bin_img) / float(bin_img.size)
        if ink_ratio > max_score and ink_ratio > 0.04:
            max_score = ink_ratio
            best_candidate = (item, bbox)
            
    if best_candidate is not None:
        item, _ = best_candidate
        conf = min(0.95, round(0.70 + max_score * 0.30, 2))
        res = {
            "detected": True,
            "code": item["code"],
            "label": item["label"],
            "confidence": conf,
            "has_subfield": item.get("has_subfield", False),
            "subfield_cause": "",
            "subfield_date": "",
            "subfield_disability": "",
            "subfield_period": "",
            "subfield_stay_abroad": "",
            "subfield_nullity": False,
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
                res["subfield_nullity"] = cell_has_ink(crop_roi(enhanced_image, null_bbox))
                res["subfield_annulment"] = cell_has_ink(crop_roi(enhanced_image, annul_bbox))
                    
        return res
        
    return {"detected": False, "code": "", "label": "", "confidence": 0.0, "has_subfield": False}

def get_confidence_rating(score: float) -> str:
    """Returns High, Moderate, or Low rating based on confidence score."""
    if score >= 0.80:
        return "High"
    elif score >= 0.50:
        return "Moderate"
    return "Low"

# Known printed labels to strip from extracted field values
FORM_LABEL_PREFIXES = {
    "last_name": [
        r"^\s*([0-9]+\.?)?\s*(name\s*)?\(?\s*(last\s*name|surname|apelyido|last)\s*\)?\s*[:\-_.]*",
        r"^\s*name\s*[:\-_.]*"
    ],
    "first_name": [
        r"^\s*([0-9]+\.?)?\s*(first\s*name|pangalan|first)\s*[:\-_.]*",
        r"^\s*\(?\s*first\s*\)?\s*[:\-_.]*"
    ],
    "middle_name": [
        r"^\s*([0-9]+\.?)?\s*(middle\s*name|gitnang\s*pangalan|middle)\s*[:\-_.]*",
        r"^\s*\(?\s*middle\s*\)?\s*[:\-_.]*"
    ],
    "ext_name": [
        r"^\s*([0-9]+\.?)?\s*ext(ension)?\.?\s*(name)?\s*[:\-_.]*",
        r"^\s*\(?\s*ext\.?\s*\)?\s*[:\-_.]*"
    ],
    "civil_status": [
        r"^\s*([0-9]+\.?)?\s*civil\s*status\s*[:\-_.]*"
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
        r"^\s*([0-9]+\.?)?\s*address\s*(\([^\)]*\))?\s*[:\-_.]*",
        r"^\s*\(?\s*house\s*no\.?.*?purok\)?\s*[:\-_.]*",
        r"^.*?(house\s*no|street|subdivision|purok|zone|\/|\)).*?:\s*",
        r"^\/.*?\)\s*:?"
    ],
    "barangay": [
        r"^\s*([0-9]+\.?)?\s*(barangay|brgy\.?)\s*[:\-_.]*"
    ],
    "contact_number": [
        r"^\s*([0-9]+\.?)?\s*(applicant'?s?\s*)?(contact|cellphone|mobile|tel)?\s*(number|no\.?)\s*[:\-_.]*"
    ],
    "emergency_name": [
        r"^.*?(person\s*to\s*(be|oe)\s*contact(ed|ea)|in\s*case\s*of\s*(emergency|cmergency)|emergency\s*contact).*?:\s*",
        r"^\s*([0-9]+\.?)?\s*(person\s*to\s*be\s*contacted|emergency\s*(contact|person|name)?)\s*[:\-_.]*"
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
    
    # Isolate inner 75% of checkbox to ignore outer border frame lines
    margin_y = max(1, int(h * 0.12))
    margin_x = max(1, int(w * 0.12))
    inner = gray[margin_y:h - margin_y, margin_x:w - margin_x]
    if inner.size == 0:
        return 0.0
        
    # Dark ink pixels on light paper (Otsu thresholding)
    thresh = cv2.threshold(inner, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)[1]
    return float(np.sum(thresh > 0)) / float(inner.size)

def detect_employment_status(enhanced_image: np.ndarray, ocr_engine, fields_config: dict) -> tuple:
    """
    Detects which employment status checkbox is marked:
    1. Checkbox: Employed (is_employed)
    2. Checkbox: Self-Employed (is_self_employed)
    3. Checkbox: Not Employed (is_not_employed)
    Compares the actual ink density and OCR marks of the 3 calibrated boxes.
    Returns: (status_string, confidence)
    """
    box_emp = fields_config.get("is_employed", {}).get("bbox", [0.595, 0.255, 0.115, 0.028])
    box_self = fields_config.get("is_self_employed", {}).get("bbox", [0.710, 0.255, 0.135, 0.028])
    box_not = fields_config.get("is_not_employed", {}).get("bbox", [0.845, 0.255, 0.125, 0.028])

    ink_emp = measure_checkbox_ink(enhanced_image, box_emp)
    ink_self = measure_checkbox_ink(enhanced_image, box_self)
    ink_not = measure_checkbox_ink(enhanced_image, box_not)

    # Check if OCR recognized a checkmark symbol in any of the 3 boxes
    rec_emp, _ = recognize_crop(ocr_engine, crop_roi(enhanced_image, box_emp))
    rec_self, _ = recognize_crop(ocr_engine, crop_roi(enhanced_image, box_self))
    rec_not, _ = recognize_crop(ocr_engine, crop_roi(enhanced_image, box_not))

    mark_chars = set("vx17✓/\\o*ckyes")
    has_mark_emp = any(c in mark_chars for c in rec_emp.lower()) if rec_emp else False
    has_mark_self = any(c in mark_chars for c in rec_self.lower()) if rec_self else False
    has_mark_not = any(c in mark_chars for c in rec_not.lower()) if rec_not else False

    score_emp = ink_emp + (0.35 if has_mark_emp else 0.0)
    score_self = ink_self + (0.35 if has_mark_self else 0.0)
    score_not = ink_not + (0.35 if has_mark_not else 0.0)

    print(f"[Employment Ink Analysis] Employed: {score_emp:.3f} (ink:{ink_emp:.3f}, ocr:'{rec_emp}'), Self: {score_self:.3f} (ink:{ink_self:.3f}, ocr:'{rec_self}'), Not: {score_not:.3f} (ink:{ink_not:.3f}, ocr:'{rec_not}')")

    max_score = max(score_emp, score_self, score_not)
    if max_score < 0.02:
        return "Employed", 0.70

    if score_emp >= score_self and score_emp >= score_not:
        return "Employed", 0.95
    elif score_self >= score_emp and score_self >= score_not:
        return "Self-Employed", 0.95
    else:
        return "Not Employed", 0.95

def clean_employment_status(raw_text: str, crop_img: np.ndarray = None) -> str:
    """Fallback parser for employment text."""
    if not raw_text:
        return "Employed"
    t = raw_text.lower()
    if "self" in t:
        return "Self-Employed"
    if "not" in t or "unemployed" in t:
        return "Not Employed"
    if "employed" in t:
        return "Employed"
    return "Employed"

def strip_form_label(field_key: str, text: str) -> str:
    """Removes the printed form label leaving only handwritten value."""
    if not text:
        return ""
    result = text
    patterns = FORM_LABEL_PREFIXES.get(field_key, [])
    for pat in patterns:
        result = re.sub(pat, "", result, flags=re.IGNORECASE)
    return clean_text(result)

def extract_single_detection(item, w, h):
    """
    Universally extracts text, confidence, and normalized center coordinates
    from any PaddleOCR / PaddleX return structure (dict or tuple).
    """
    try:
        # Case 1: Dict format (PaddleX / PaddleOCR 3.x)
        if isinstance(item, dict):
            text = item.get("transcription") or item.get("rec_text") or item.get("text") or ""
            conf = float(item.get("score") or item.get("rec_score") or item.get("confidence") or 0.0)
            box = item.get("points") or item.get("dt_polys") or item.get("box") or []
            if box and len(box) >= 4:
                cx = sum(p[0] for p in box) / (len(box) * float(w))
                cy = sum(p[1] for p in box) / (len(box) * float(h))
                return {"text": str(text).strip(), "conf": conf, "cx": cx, "cy": cy, "box": box}
            return None

        # Case 2: List/Tuple format: [[[x1,y1],[x2,y2],[x3,y3],[x4,y4]], ("text", score)]
        if isinstance(item, (list, tuple)) and len(item) >= 2:
            box = item[0]
            text_part = item[1]
            if isinstance(text_part, (list, tuple)) and len(text_part) >= 1:
                text = str(text_part[0]).strip()
                conf = float(text_part[1]) if len(text_part) > 1 else 1.0
            else:
                text = str(text_part).strip()
                conf = 1.0
                
            if isinstance(box, (list, tuple)) and len(box) >= 4:
                cx = sum(p[0] for p in box) / (len(box) * float(w))
                cy = sum(p[1] for p in box) / (len(box) * float(h))
                return {"text": text, "conf": conf, "cx": cx, "cy": cy, "box": box}
    except Exception:
        pass
    return None

def compute_box_center(b, crop_w, crop_h):
    """
    Computes normalized center (cx, cy) from either [xmin, ymin, xmax, ymax] or [[x1,y1], [x2,y2], ...].
    """
    try:
        if hasattr(b, "tolist"):
            b = b.tolist()
            
        # Case 1: [xmin, ymin, xmax, ymax] (4 numbers)
        if len(b) == 4 and all(isinstance(v, (int, float)) for v in b):
            cx = (float(b[0]) + float(b[2])) / (2.0 * float(crop_w))
            cy = (float(b[1]) + float(b[3])) / (2.0 * float(crop_h))
            return cx, cy
            
        # Case 2: [[x1,y1], [x2,y2], [x3,y3], [x4,y4]] (list of coordinate pairs)
        if len(b) >= 4 and isinstance(b[0], (list, tuple)):
            cx = sum(float(p[0]) for p in b) / (len(b) * float(crop_w))
            cy = sum(float(p[1]) for p in b) / (len(b) * float(crop_h))
            return cx, cy
    except Exception:
        pass
    return None, None

def recognize_crop(ocr_engine, crop_image: np.ndarray):
    """
    Directly recognizes text on an isolated crop with border padding.
    Guarantees exact parity between live testing and full extraction.
    """
    if ocr_engine is None or crop_image is None or crop_image.size == 0:
        return "", 0.0
        
    if len(crop_image.shape) == 2:
        crop_bgr = cv2.cvtColor(crop_image, cv2.COLOR_GRAY2BGR)
    else:
        crop_bgr = crop_image
        
    padded = cv2.copyMakeBorder(crop_bgr, 25, 25, 25, 25, cv2.BORDER_CONSTANT, value=[255, 255, 255])
    
    try:
        res = list(ocr_engine.predict(padded))
        if res:
            first = res[0]
            if hasattr(first, "keys") or isinstance(first, dict):
                res_dict = dict(first) if hasattr(first, "keys") else first
                texts = res_dict.get("rec_texts")
                scores = res_dict.get("rec_scores")
                if texts is None: texts = res_dict.get("rec_text", [])
                if scores is None: scores = res_dict.get("rec_score", [])
                
                if hasattr(texts, "tolist"): texts = texts.tolist()
                if hasattr(scores, "tolist"): scores = scores.tolist()
                
                if texts:
                    clean_str = " ".join(str(t).strip() for t in texts if str(t).strip())
                    avg_score = float(sum(scores) / len(scores)) if scores else 0.0
                    return clean_str, avg_score
    except Exception as e:
        print(f"[recognize_crop Error]: {e}")
        
    return "", 0.0

def parse_form_with_ocr(ocr_engine, enhanced_image: np.ndarray, template: dict = None) -> dict:
    """
    High-Accuracy Direct Field-Box Extraction:
    Crops each calibrated template box directly and runs recognize_crop
    with identical padding and inference parameters as the live testing tool.
    """
    import time
    t_start = time.time()
    
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
        cleaned = strip_form_label(f_key, raw_text)
        
        if f_key == "barangay":
            cleaned = match_barangay(cleaned)
        elif f_key == "religion":
            cleaned = clean_religion(cleaned)
        elif f_key == "civil_status":
            cleaned = clean_civil_status(cleaned)
        elif f_key == "emergency_relationship":
            cleaned = clean_relationship(cleaned)
        elif f_key == "sex":
            cleaned = clean_sex(cleaned)
        elif f_key == "ext_name":
            cleaned = clean_extension(cleaned)
        elif f_key in ["contact_number", "emergency_number"]:
            cleaned = clean_phone_number(cleaned)
        elif f_key == "monthly_income":
            cleaned = clean_income(cleaned)
        elif f_key == "employment_status":
            cleaned, conf = detect_employment_status(enhanced_image, ocr_engine, fields_config)
        elif f_type == "date" or f_key == "birthdate":
            date_parsed = clean_date(cleaned)
            if date_parsed:
                cleaned = date_parsed
            else:
                # If unparseable date text exists, keep cleaned text for manual review and decouple confidence
                if cleaned:
                    conf = 0.0
                else:
                    conf = 0.0
            
        if not cleaned:
            conf = 0.0
            
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

    # 2. Extract Section II Circumstances (Code & Checkbox ROI Only)
    detected_circumstance = detect_encircled_circumstance(enhanced_image, ocr_engine, template)
    if detected_circumstance.get("detected"):
        print(f"[Circumstance Detected] Code: {detected_circumstance.get('code')} -> '{detected_circumstance.get('label')}' (conf: {detected_circumstance.get('confidence')})")
        
    # 3. Extract Section III/V Family Dependents Table (Dynamic Cell & Ink-Gated Extraction)
    family_members = []
    if ocr_engine is not None:
        table_cfg = fields_config.get("family_composition_table", {})
        t_bbox = table_cfg.get("bbox", [0.025, 0.485, 0.940, 0.150])
        tb_x, tb_y, tb_w, tb_h = t_bbox
        
        row_count = 5
        hdr_ratio = 0.16
        avail_h = tb_h * (1.0 - hdr_ratio)
        r_step = avail_h / float(row_count)
        
        cols_default = [
            ("name",         0.025, 0.230),
            ("sex",          0.255, 0.060),
            ("age",          0.315, 0.055),
            ("birthdate",    0.370, 0.135),
            ("civil_status", 0.505, 0.095),
            ("relationship", 0.600, 0.120),
            ("education",    0.720, 0.140),
            ("income",       0.860, 0.105)
        ]
        
        for r_idx in range(row_count):
            r_num = r_idx + 1
            r_top = tb_y + (tb_h * hdr_ratio) + (r_idx * r_step) + 0.001
            r_height = max(0.016, r_step - 0.003)
            
            # Helper to get dynamic bbox for this cell
            def get_cell_bbox(col_name: str, col_idx: int) -> list:
                custom_key = f"fam_row{r_num}_{col_name}"
                if custom_key in fields_config and "bbox" in fields_config[custom_key]:
                    return fields_config[custom_key]["bbox"]
                return [cols_default[col_idx][1], r_top, cols_default[col_idx][2], r_height]

            # 1. Pre-check: Does Name cell have handwriting ink?
            name_box = get_cell_bbox("name", 0)
            crop_name = crop_roi(enhanced_image, name_box)
            if not cell_has_ink(crop_name):
                # Row is blank, skip entire row in 0ms!
                continue
                
            raw_name, name_conf = recognize_crop(ocr_engine, crop_name)
            clean_m_name = clean_text(raw_name)
            
            header_filter = ["name", "pangalan", "kasarian", "sex", "age", "edad", "relasyon", "relationship", "family", "composition", "iii.", "v.", "status", "income", "birthdate"]
            if not clean_m_name or len(clean_m_name) < 2 or any(h in clean_m_name.lower() for h in header_filter):
                continue
                
            # 2. Sex
            clean_m_sex = "Female"
            sex_conf = 0.90
            crop_sex = crop_roi(enhanced_image, get_cell_bbox("sex", 1))
            if cell_has_ink(crop_sex):
                raw_sex, sex_conf = recognize_crop(ocr_engine, crop_sex)
                clean_m_sex = clean_sex(raw_sex)
                
            # 3. Age
            clean_m_age = "—"
            age_conf = 0.90
            crop_age = crop_roi(enhanced_image, get_cell_bbox("age", 2))
            if cell_has_ink(crop_age):
                raw_age, age_conf = recognize_crop(ocr_engine, crop_age)
                clean_m_age = clean_text(raw_age) or "—"
                
            # 4. Birthdate
            clean_m_dob = ""
            dob_conf = 0.90
            crop_dob = crop_roi(enhanced_image, get_cell_bbox("dob", 3))
            if cell_has_ink(crop_dob):
                raw_dob, dob_conf = recognize_crop(ocr_engine, crop_dob)
                clean_m_dob = clean_date(raw_dob)
                if not clean_m_dob and raw_dob:
                    dob_conf = 0.40
                
            # 5. Civil Status
            clean_m_civ = "Single"
            civ_conf = 0.90
            crop_civ = crop_roi(enhanced_image, get_cell_bbox("civ", 4))
            if cell_has_ink(crop_civ):
                raw_civ, civ_conf = recognize_crop(ocr_engine, crop_civ)
                clean_m_civ = clean_civil_status(raw_civ) or "Single"
                
            # 6. Relationship
            clean_m_rel = "Child"
            rel_conf = 0.90
            crop_rel = crop_roi(enhanced_image, get_cell_bbox("rel", 5))
            if cell_has_ink(crop_rel):
                raw_rel, rel_conf = recognize_crop(ocr_engine, crop_rel)
                clean_m_rel = clean_relationship(raw_rel) or "Child"
                
            # 7. Education / Job
            clean_m_edu = "—"
            edu_conf = 0.90
            crop_edu = crop_roi(enhanced_image, get_cell_bbox("edu", 6))
            if cell_has_ink(crop_edu):
                raw_edu, edu_conf = recognize_crop(ocr_engine, crop_edu)
                clean_m_edu = clean_text(raw_edu) or "—"
                
            # 8. Monthly Income
            clean_m_inc = "0"
            inc_conf = 0.90
            crop_inc = crop_roi(enhanced_image, get_cell_bbox("inc", 7))
            if cell_has_ink(crop_inc):
                raw_inc, inc_conf = recognize_crop(ocr_engine, crop_inc)
                clean_m_inc = clean_income(raw_inc) or "0"
                
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

def parse_family_table(all_detections: list, table_config: dict) -> List[Dict[str, str]]:
    """Groups text lines in Section III table into dependent rows."""
    members = []
    bbox = table_config.get("bbox", [0.035, 0.440, 0.930, 0.110])
    bx, by, bw, bh = bbox
    
    table_detections = [d for d in all_detections if (bx <= d["cx"] <= bx + bw) and (by <= d["cy"] <= by + bh)]
    table_detections.sort(key=lambda d: d["cy"])
    
    rows = []
    current_row = []
    last_y = -1.0
    
    for det in table_detections:
        if last_y < 0 or abs(det["cy"] - last_y) < 0.025:
            current_row.append(det)
        else:
            if current_row:
                rows.append(current_row)
            current_row = [det]
        last_y = det["cy"]
    if current_row:
        rows.append(current_row)
        
    for r in rows:
        r.sort(key=lambda d: d["cx"])
        row_text = " ".join(d["text"] for d in r)
        parts = clean_text(row_text).split()
        if len(parts) >= 2:
            members.append({
                "memberName": parts[0] + " " + (parts[1] if len(parts) > 1 else ""),
                "sex": parts[2] if len(parts) > 2 and parts[2].lower() in ["m", "f", "male", "female"] else "—",
                "age": parts[3] if len(parts) > 3 and parts[3].isdigit() else "—",
                "relationship": "Child",
                "civilStatus": "Single",
                "birthdate": "",
                "educationEmployment": "",
                "income": "0"
            })
            
    return members