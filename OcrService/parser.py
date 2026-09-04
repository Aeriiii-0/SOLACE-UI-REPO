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

# Safety padding (in pixels) around tight DBNet word polygons.
# Set to 0 to completely disable padding and use raw 0-pixel polygon bounds.
DBNET_CROP_PADDING_PX = 3

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

def is_ocr_na(raw_text: str) -> bool:
    """Checks if raw text represents N/A, None, or empty in handwritten/printed OCR."""
    if not raw_text:
        return True
    t = raw_text.strip().upper()
    t_no_punct = re.sub(r'[^A-Z0-9]', '', t)
    na_exact = {
        "N/A", "NA", "NLA", "N\\A", "N|A", "N1A", "NIA", "NLA", "N-A", "N_A", "N A",
        "N.A.", "N.A", "NONE", "NONE.", "WALA", "-", "--", "—", "N/A.", "N/A/",
        "NOT APPLICABLE", "NOT APPL", "N/0", "N0", "NO", "0", "0.00", "NULL", "N. A."
    }
    if t in na_exact:
        return True
    if t_no_punct in ["NA", "NLA", "NIA", "N1A", "NONE", "WALA", "NOTAPPLICABLE", "NOTAPPL", "NULL"]:
        return True
    return False

def match_barangay(raw_text: str) -> str:
    """
    Performs fuzzy matching on OCR extracted text against official Biñan barangays.
    Ensures OCR output is first matched using fuzzy similarity before being returned.
    Returns empty string if raw text is N/A or empty.
    """
    if not raw_text or is_ocr_na(raw_text):
        return ""
    
    cleaned = clean_text(raw_text)
    if not cleaned or is_ocr_na(cleaned):
        return ""

    norm_raw = normalize_for_matching(cleaned)
    if not norm_raw:
        return ""

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
    return "" if is_ocr_na(val) else val

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

def clean_person_name(raw_name: str) -> str:
    """
    Sanitizes person names:
    - Strips residual printed form label prefixes (e.g. 'Middle ame', 'Middle name', 'Last name')
    - Converts OCR digit confusions (4 -> A, 0 -> O)
    - Strips all non-letter characters except ñ/Ñ, space, period, hyphen, apostrophe
    - Removes duplicate whitespace and trims
    """
    if not raw_name or is_ocr_na(raw_name):
        return ""
    t = raw_name.strip()
    # Strip any residual printed label prefixes like 'Middle ame', 'Middle name', etc.
    t = re.sub(r'^\s*\(?\s*(m[i1lI]dd?[li1I]?e?\s+(a?m[eo]|name)|middle\b|last\s+(a?m[eo]|name)|last\b|first\s+(a?m[eo]|name)|first\b|gitnang\s+pangalan)\s*\)?\s*[:\-_.]*\s*', '', t, flags=re.IGNORECASE)
    t = re.sub(r'4', 'A', t)
    t = re.sub(r'0', 'O', t)
    t = re.sub(r'[^a-zA-ZñÑ\s\.\-\']', '', t)
    t = re.sub(r'\s+', ' ', t).strip()
    return t

def clean_family_age(raw_age: str) -> str:
    """
    Normalizes age to numbers only:
    - Strips common label prefixes like 'Age:' or 'Edad:'
    - If already pure digits, keeps them
    - If short token (<= 3 chars), converts OCR character misidentifications (G/g -> 6, O/o -> 0, etc.)
    - Extracts pure digits, ensuring 0 <= age <= 120
    """
    if not raw_age or is_ocr_na(raw_age):
        return ""
    t = raw_age.strip()
    t = re.sub(r'^(age|edad)\s*[:\-_.]*', '', t, flags=re.IGNORECASE).strip()
    pure_digits = re.findall(r'\b\d{1,3}\b', t)
    if pure_digits:
        val = int(pure_digits[0])
        if 0 <= val <= 120:
            return str(val)
    if len(t) <= 3:
        t = re.sub(r'[Gg]', '6', t)
        t = re.sub(r'[Oo]', '0', t)
        t = re.sub(r'[Il|/]', '1', t)
        t = re.sub(r'[Ss]', '5', t)
        t = re.sub(r'[Bb]', '8', t)
        digits = re.findall(r'\d+', t)
        if digits:
            val = int(digits[0])
            if 0 <= val <= 120:
                return str(val)
    return ""

def clean_date(raw_text: str) -> str:
    """
    Extracts and normalizes dates from handwritten or printed text.
    Handles 'June 19, 2004', OCR merged 'Jum192004', '19 June 2004', MM/DD/YYYY, YYYY-MM-DD, MMDDYYYY, MMDDYY.
    Returns normalized YYYY-MM-DD or empty string '' if no valid date is found.
    """
    if not raw_text or is_ocr_na(raw_text):
        return ""
    raw = raw_text.strip()
    
    # Strip common label prefixes
    raw = re.sub(r'^(date\s*of\s*(this\s*)?application|application\s*date|petsa(\s*ng\s*aplikasyon)?|date\s*of\s*birth|birthdate|dob|kaarawan|bday|date)\s*[:\-_.]*', '', raw, flags=re.IGNORECASE).strip()
    if not raw or is_ocr_na(raw):
        return ""

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
    if not raw_text or is_ocr_na(raw_text):
        return ""
    t = raw_text.strip().lower()
    if t in ["m", "male", "lalaki", "m.", "boy", "son", "father", "brother", "l"]:
        return "Male"
    if t in ["f", "female", "babae", "fem", "f.", "girl", "daughter", "mother", "sister"]:
        return "Female"
    if re.search(r'\b(female|babae|fem)\b', t) or t.startswith('f'):
        return "Female"
    if re.search(r'\b(male|lalaki)\b', t) or t.startswith('m'):
        return "Male"
    val = clean_text(raw_text)
    return "" if is_ocr_na(val) else val

def clean_phone_number(raw_text: str) -> str:
    """
    Normalizes Philippine mobile/contact numbers to visual hyphenated format (09XX-XXX-XXXX).
    Handles +639..., 639..., 9XXXXXXXXX, spaces, and dashes.
    """
    if not raw_text or is_ocr_na(raw_text):
        return ""
    digits = re.sub(r'\D', '', raw_text)
    if digits.startswith("63") and len(digits) == 12:
        digits = "0" + digits[2:]
    elif digits.startswith("9") and len(digits) == 10:
        digits = "0" + digits

    if len(digits) == 11:
        return f"{digits[0:4]}-{digits[4:7]}-{digits[7:11]}"
    elif len(digits) > 7:
        return f"{digits[0:4]}-{digits[4:7]}-{digits[7:]}"
    elif len(digits) > 4:
        return f"{digits[0:4]}-{digits[4:]}"
    elif digits:
        return digits

    val = clean_text(raw_text)
    return "" if is_ocr_na(val) else val

def clean_extension(raw_text: str) -> str:
    """Fuzzy normalization of Extension Name to predetermined list (Jr., Sr., II, III, IV, V)."""
    if not raw_text or is_ocr_na(raw_text):
        return ""
    t = raw_text.strip().upper()
    if is_ocr_na(t):
        return ""
    
    t = re.sub(r'\b(EXT|EXTENSION|NAME)\b', '', t).strip()
    if not t or is_ocr_na(t):
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
    if not raw_text or is_ocr_na(raw_text):
        return ""
    t = raw_text.strip().lower()
    t_clean = re.sub(r'[^a-z0-9]', '', t)
    if is_ocr_na(t) or is_ocr_na(t_clean):
        return ""
    
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
    return "" if is_ocr_na(val) else val

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
        r"^\s*([0-9]+\.?)?\s*\(?\s*(name\s*)?(last\s*name|last\s*a?m[eo]|surname|apelyido|last\b)\s*\)?\s*[:\-_.]*",
        r"^\s*\(?\s*(name|ame|surname)\b\s*\)?\s*[:\-_.]*"
    ],
    "first_name": [
        r"^\s*([0-9]+\.?)?\s*\(?\s*(first\s*name|first\s*a?m[eo]|pangalan|first\b)\s*\)?\s*[:\-_.]*",
        r"^\s*\(?\s*(first\b|pangalan)\s*\)?\s*[:\-_.]*",
        r"^\s*\(?\s*(name|ame)\b\s*\)?\s*[:\-_.]*"
    ],
    "middle_name": [
        r"^\s*([0-9]+\.?)?\s*\(?\s*m[i1lI]dd?[li1I]?e?\s*(a?m[eo]|name|gitnang\s*pangalan)?\s*\)?\s*[:\-_.]*",
        r"^\s*\(?\s*(middle\s*name|middle\s*a?m[eo]|middle\b|gitnang\s*pangalan)\s*\)?\s*[:\-_.]*",
        r"^\s*\(?\s*(m\.?i\.?|gitnang\s*pangalan)\s*\)?\s*[:\-_.]*",
        r"^\s*\(?\s*(name|ame)\b\s*\)?\s*[:\-_.]*"
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
        r"^\s*([0-9]+\.?)?\s*\(?\s*(person\s*to\s*be\s*contacted|emergency\s*(contact|person|name|a?m[eo])?)\s*\)?\s*[:\-_.]*",
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

def measure_checkbox_ink(image: np.ndarray, bbox: list) -> float:
    """Measures checkmark / handwriting ink inside a specific checkbox ROI."""
    if bbox is None or len(bbox) < 4:
        return 0.0
    crop = crop_roi(image, bbox)
    if crop is None or crop.size == 0:
        return 0.0
    gray = cv2.cvtColor(crop, cv2.COLOR_BGR2GRAY) if len(crop.shape) == 3 else crop
    h, w = gray.shape[:2]
    if h < 5 or w < 5:
        return 0.0

    # Isolate inner checkbox to ignore outer border frame lines
    margin_y = max(1, int(round(h * 0.20)))
    margin_x = max(1, int(round(w * 0.18)))
    inner = gray[margin_y:h - margin_y, margin_x:w - margin_x]
    if inner.size == 0:
        return 0.0

    paper_median = float(np.median(inner))
    if paper_median < 40:
        return 0.0

    # True pen ink is noticeably darker than surrounding white paper background
    ink_mask = (inner < (paper_median - 28)) & (inner < 190)
    return float(np.sum(ink_mask)) / float(inner.size)

def detect_employment_status(enhanced_image: np.ndarray, ocr_engine, fields_config: dict) -> tuple:
    """
    Detects which employment status checkbox is marked:
    1. Checkbox: Employed (is_employed)
    2. Checkbox: Self-Employed (is_self_employed)
    3. Checkbox: Not Employed (is_not_employed)
    Compares the actual ink density and OCR marks of the 3 calibrated boxes.
    Returns: (status_string, confidence)
    """
    # 1. Retrieve the adjusted bounding boxes from template configuration
    box_emp = fields_config.get("is_employed", {}).get("bbox")
    box_self = fields_config.get("is_self_employed", {}).get("bbox")
    box_not = fields_config.get("is_not_employed", {}).get("bbox")

    # 2. If child checkbox boxes are missing from template, dynamically derive from adjusted parent box
    parent_emp = fields_config.get("employment_status", {}).get("bbox")
    if parent_emp and len(parent_emp) == 4:
        px, py, pw, ph = parent_emp
        if not box_emp:
            box_emp = [px + pw * 0.07, py + ph * 0.35, pw * 0.05, ph * 0.50]
        if not box_self:
            box_self = [px + pw * 0.33, py + ph * 0.35, pw * 0.05, ph * 0.50]
        if not box_not:
            box_not = [px + pw * 0.66, py + ph * 0.35, pw * 0.06, ph * 0.50]

    # 3. Fallback default coordinates only if template is completely empty
    if not box_emp: box_emp = [0.620, 0.252, 0.017, 0.012]
    if not box_self: box_self = [0.718, 0.252, 0.017, 0.012]
    if not box_not: box_not = [0.845, 0.252, 0.019, 0.012]

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
    if max_score < 0.03:
        return "Employed", 0.70

    if score_not >= score_emp and score_not >= score_self:
        return "Not Employed", 0.95
    elif score_self >= score_emp and score_self >= score_not:
        return "Self-Employed", 0.95
    else:
        return "Employed", 0.95


def strip_form_label(field_key: str, text: str) -> str:
    """Removes the printed form label leaving only handwritten value."""
    if not text:
        return ""
    result = text
    patterns = FORM_LABEL_PREFIXES.get(field_key, [])
    for pat in patterns:
        result = re.sub(pat, "", result, flags=re.IGNORECASE)
    return clean_text(result)


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

def extract_detections_from_paddle(pred_results, img_w: int, img_h: int) -> list:
    """
    Extracts all detected text words/boxes with normalized coordinates from PaddleOCR / PaddleX output.
    Returns: list of dicts: {"text": str, "conf": float, "cx": float, "cy": float, "bbox": [x, y, w, h]}
    """
    detections = []
    if not pred_results:
        return detections

    w_f = float(img_w)
    h_f = float(img_h)

    for item in pred_results:
        # Case 1: PaddleX 3.x dict format
        if isinstance(item, dict) or hasattr(item, "keys"):
            res_dict = dict(item) if hasattr(item, "keys") else item
            texts = res_dict.get("rec_texts")
            scores = res_dict.get("rec_scores")
            polys = res_dict.get("dt_polys")
            if polys is None: polys = res_dict.get("rec_polys")
            if texts is None: texts = res_dict.get("rec_text", [])
            if scores is None: scores = res_dict.get("rec_score", [])

            if hasattr(texts, "tolist"): texts = texts.tolist()
            if hasattr(scores, "tolist"): scores = scores.tolist()

            if isinstance(texts, list) and len(texts) > 0:
                for idx, text in enumerate(texts):
                    score = float(scores[idx]) if idx < len(scores) else 0.90
                    poly = polys[idx] if (polys is not None and idx < len(polys)) else None
                    if poly is not None and len(poly) >= 4:
                        pts = np.array(poly)
                        xmin = float(np.min(pts[:, 0])) / w_f
                        xmax = float(np.max(pts[:, 0])) / w_f
                        ymin = float(np.min(pts[:, 1])) / h_f
                        ymax = float(np.max(pts[:, 1])) / h_f
                        cx = (xmin + xmax) / 2.0
                        cy = (ymin + ymax) / 2.0
                        detections.append({
                            "text": str(text).strip(),
                            "conf": score,
                            "cx": cx,
                            "cy": cy,
                            "bbox": [xmin, ymin, max(0.001, xmax - xmin), max(0.001, ymax - ymin)]
                        })

        # Case 2: Classic PaddleOCR format: [ [ [[x1,y1],[x2,y2],[x3,y3],[x4,y4]], ("text", score) ], ... ]
        elif isinstance(item, (list, tuple)):
            for line in item:
                if isinstance(line, (list, tuple)) and len(line) >= 2:
                    poly = line[0]
                    text_part = line[1]
                    text = text_part[0] if isinstance(text_part, (list, tuple)) else str(text_part)
                    score = float(text_part[1]) if isinstance(text_part, (list, tuple)) and len(text_part) > 1 else 0.90
                    if poly is not None and len(poly) >= 4:
                        pts = np.array(poly)
                        xmin = float(np.min(pts[:, 0])) / w_f
                        xmax = float(np.max(pts[:, 0])) / w_f
                        ymin = float(np.min(pts[:, 1])) / h_f
                        ymax = float(np.max(pts[:, 1])) / h_f
                        cx = (xmin + xmax) / 2.0
                        cy = (ymin + ymax) / 2.0
                        detections.append({
                            "text": str(text).strip(),
                            "conf": score,
                            "cx": cx,
                            "cy": cy,
                            "bbox": [xmin, ymin, max(0.001, xmax - xmin), max(0.001, ymax - ymin)]
                        })

    return detections

def assign_detections_to_field(field_key: str, cfg: dict, all_detections: list) -> tuple:
    """
    Finds all text detections that spatially intersect this field's target bounding box,
    filters out known printed form labels, sorts surviving words left-to-right,
    and returns (raw_text, confidence).
    """
    bbox = cfg.get("bbox", [0, 0, 0, 0])
    bx, by, bw, bh = bbox
    pad_x = 0.000
    pad_y = 0.000
    min_x, max_x = bx - pad_x, bx + bw + pad_x
    min_y, max_y = by - pad_y, by + bh + pad_y

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

def clean_field_value(f_key: str, f_type: str, raw_text: str, conf: float, enhanced_image: np.ndarray, ocr_engine, fields_config: dict) -> tuple:
    """Applies domain cleaners to the raw OCR text for a specific field."""
    if not raw_text or is_ocr_na(raw_text):
        if f_key == "monthly_income":
            return "0", conf
        return "", 0.0

    cleaned = strip_form_label(f_key, raw_text)
    if not cleaned or is_ocr_na(cleaned):
        if f_key == "monthly_income":
            return "0", conf
        return "", 0.0

    if f_key in ["last_name", "first_name", "middle_name", "emergency_name"]:
        cleaned = clean_person_name(cleaned)
    elif f_key == "barangay":
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
    elif f_type == "date" or f_key in ["birthdate", "application_date"]:
        date_parsed = clean_date(cleaned)
        if date_parsed:
            cleaned = date_parsed
        else:
            if not cleaned or is_ocr_na(cleaned):
                conf = 0.0

    if cleaned and (cleaned.startswith("{{") or cleaned.endswith("}}") or "{{" in cleaned):
        cleaned = ""
        conf = 0.0

    if not cleaned or is_ocr_na(cleaned):
        if f_key != "monthly_income":
            cleaned = ""
        conf = 0.0

    return cleaned, conf

def parse_family_table_grid(ocr_engine, enhanced_image: np.ndarray, fields_config: dict, all_detections: list = None) -> list:
    """Extracts Section III/V Family Dependents Table rows with ink gating and strict N/A filtering."""
    family_members = []
    if ocr_engine is None:
        return family_members

    table_cfg = fields_config.get("family_composition_table", {})
    t_bbox = table_cfg.get("bbox", [0.025, 0.485, 0.940, 0.150])
    tb_x, tb_y, tb_w, tb_h = t_bbox

    row_count = 5
    hdr_ratio = 0.16
    avail_h = tb_h * (1.0 - hdr_ratio)
    r_step = avail_h / float(row_count)

    cols_default = [
        ("name", 0.025, 0.230),
        ("sex",  0.255, 0.060),
        ("age",  0.315, 0.055),
        ("dob",  0.370, 0.135),
        ("civ",  0.505, 0.095),
        ("rel",  0.600, 0.120),
        ("edu",  0.720, 0.140),
        ("inc",  0.860, 0.105)
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

        # If Birthdate is valid YYYY-MM-DD, compute Age automatically if missing or misread
        if clean_m_dob and len(clean_m_dob) == 10 and clean_m_dob[4] == '-' and clean_m_dob[7] == '-':
            try:
                from datetime import datetime, date
                dt = datetime.strptime(clean_m_dob, "%Y-%m-%d").date()
                today = date.today()
                calc_age = today.year - dt.year - ((today.month, today.day) < (dt.month, dt.day))
                if 0 <= calc_age <= 120:
                    clean_m_age = str(calc_age)
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
        clean_m_edu = "" if is_ocr_na(raw_edu) else raw_edu
        if is_ocr_na(clean_m_edu): clean_m_edu = ""

        # 8. Income
        raw_inc, inc_conf = get_cell_text("inc", 7)
        clean_m_inc = "0" if is_ocr_na(raw_inc) else (clean_income(raw_inc) or "0")

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

# Native OpenVINO Recognition Engine
_global_ov_recognizer = None

class OpenVINOTextRecognizer:
    """
    Ultra-Fast Native OpenVINO Recognition Engine:
    - Zero Python concurrency (OpenVINO C++ backend has exclusive multi-core hardware control).
    - Enforces static 2-bucket shape compiling: Bucket 1 (48x320) and Bucket 2 (48x960).
    - ov::hint::performance_mode = THROUGHPUT for maximum batched tensor throughput.
    - Directly accepts pre-allocated NumPy tensor arrays with zero intermediate Python wrappers.
    """
    def __init__(self, model_xml_path: str = None, dict_path: str = None):
        if model_xml_path is None:
            model_xml_path = os.path.join(os.path.dirname(__file__), "models", "en_PP-OCRv4_rec.xml")
        if dict_path is None:
            dict_path = os.path.join(os.path.dirname(__file__), "models", "en_dict.txt")

        import openvino as ov
        self.core = ov.Core()
        self.core.set_property("CPU", {"PERFORMANCE_HINT": "THROUGHPUT",
        "NUM_STREAMS": "AUTO"})

        # Load character dictionary
        self.chars = []
        if os.path.exists(dict_path):
            with open(dict_path, "r", encoding="utf-8") as f:
                for line in f:
                    self.chars.append(line.strip("\r\n"))
        else:
            self.chars = ["blank", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9"]

        # Hardcoded Reshape & Compilation for Bucket 1: Short fields (N, 3, 48, 320)
        model_short = self.core.read_model(model_xml_path)
        model_short.reshape([-1, 3, 48, 320])
        self.compiled_short = self.core.compile_model(model_short, "CPU", {"PERFORMANCE_HINT": "THROUGHPUT"})

        # Hardcoded Reshape & Compilation for Bucket 2: Long fields (N, 3, 48, 960)
        model_long = self.core.read_model(model_xml_path)
        model_long.reshape([-1, 3, 48, 960])
        self.compiled_long = self.core.compile_model(model_long, "CPU", {"PERFORMANCE_HINT": "THROUGHPUT"})

        print("[OpenVINO Engine SUCCESS] Native OpenVINO Recognizer compiled with static 2-bucket tensors (48x320 & 48x960) and THROUGHPUT hint!")

    def recognize(self, tensor_batch: np.ndarray, bucket: str = "short") -> tuple:
        """
        Runs direct native OpenVINO C++ inference on a pre-allocated static NumPy tensor batch (N, 3, 48, W).
        """
        if tensor_batch is None or len(tensor_batch) == 0:
            return [], []

        compiled_model = self.compiled_short if bucket == "short" else self.compiled_long
        output_tensor = compiled_model([tensor_batch])[compiled_model.output(0)]

        # Vectorized CTC Greedy Decoding
        preds_idx = output_tensor.argmax(axis=-1)
        preds_prob = output_tensor.max(axis=-1)
        batch_size, time_steps = preds_idx.shape
        texts = []
        scores = []

        for b in range(batch_size):
            decoded_chars = []
            char_probs = []
            prev_idx = 0
            for t in range(time_steps):
                idx = preds_idx[b, t]
                prob = preds_prob[b, t]
                if idx != 0 and idx != prev_idx and idx < len(self.chars):
                    decoded_chars.append(self.chars[idx])
                    char_probs.append(float(prob))
                prev_idx = idx

            texts.append("".join(decoded_chars))
            scores.append(float(np.mean(char_probs)) if char_probs else 0.0)

        return texts, scores

def get_ov_recognizer():
    global _global_ov_recognizer
    if _global_ov_recognizer is None:
        try:
            _global_ov_recognizer = OpenVINOTextRecognizer()
        except Exception as e:
            print(f"[OpenVINO Init Error]: {e}")
    return _global_ov_recognizer

def prepare_static_bucket_canvas(crop_img: np.ndarray, target_w: int, target_h: int = 48) -> np.ndarray:
    """
    Pastes crop into a pre-allocated static (target_h, target_w, 3) canvas.
    Enforces strict static tensor shapes to eliminate dynamic shape re-allocation in the C++ backend.
    """
    if len(crop_img.shape) == 2:
        img_bgr = cv2.cvtColor(crop_img, cv2.COLOR_GRAY2BGR)
    else:
        img_bgr = crop_img.copy()

    h, w = img_bgr.shape[:2]
    if h <= 0 or w <= 0:
        return np.full((target_h, target_w, 3), 255, dtype=np.uint8)

    # Scale to fixed height target_h keeping aspect ratio
    scale = float(target_h) / float(h)
    new_w = min(target_w, max(1, int(w * scale)))
    resized = cv2.resize(img_bgr, (new_w, target_h), interpolation=cv2.INTER_LINEAR)

    # Pre-allocated fixed-dimension canvas
    canvas = np.full((target_h, target_w, 3), 255, dtype=np.uint8)
    canvas[:, :new_w] = resized
    return canvas

def parse_form_with_global_ocr(ocr_engine, enhanced_image: np.ndarray, template: dict = None) -> dict:
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
                for fk, cfg in fields_config.items():
                    bx, by, bw, bh = cfg.get("bbox", [0, 0, 0, 0])
                    if (bx <= cx <= bx + bw) and (by <= cy <= by + bh):
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

            # Generate and save Visual Debug Detections image (debug_detections.jpg)
            try:
                debug_img = img_bgr.copy()
                # 1. Draw yellow boxes for active template fields
                for fk, cfg in fields_config.items():
                    bx, by, bw, bh = cfg.get("bbox", [0, 0, 0, 0])
                    px1, py1 = int(bx * full_w), int(by * full_h)
                    px2, py2 = int((bx + bw) * full_w), int((by + bh) * full_h)
                    cv2.rectangle(debug_img, (px1, py1), (px2, py2), (0, 220, 255), 1)

                # 2. Draw gray contours for discarded non-target text
                for poly in polys:
                    if poly is not None and len(poly) >= 4:
                        pts_int = np.array(poly, dtype=np.int32)
                        cv2.polylines(debug_img, [pts_int], True, (170, 170, 170), 1)

                # 3. Draw bright green bounding boxes around accepted target word crops (including the 3px padding)
                for d in all_detections:
                    cx, cy = d["cx"], d["cy"]
                    bw, bh = d["bbox"][2], d["bbox"][3]
                    gx1 = max(0, int((cx - bw / 2.0) * full_w) - DBNET_CROP_PADDING_PX)
                    gy1 = max(0, int((cy - bh / 2.0) * full_h) - DBNET_CROP_PADDING_PX)
                    gx2 = min(full_w, int((cx + bw / 2.0) * full_w) + DBNET_CROP_PADDING_PX)
                    gy2 = min(full_h, int((cy + bh / 2.0) * full_h) + DBNET_CROP_PADDING_PX)
                    cv2.rectangle(debug_img, (gx1, gy1), (gx2, gy2), (0, 255, 0), 2)
                    cv2.putText(debug_img, d["text"][:15], (gx1, max(12, gy1 - 3)), cv2.FONT_HERSHEY_SIMPLEX, 0.35, (0, 180, 0), 1)

                debug_save_path = os.path.join(os.path.dirname(__file__), "debug_detections.jpg")
                cv2.imwrite(debug_save_path, debug_img)
                # Also save copy to bin/Debug folder if it exists
                bin_debug_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "bin", "Debug"))
                if os.path.exists(bin_debug_dir):
                    cv2.imwrite(os.path.join(bin_debug_dir, "debug_detections.jpg"), debug_img)
                print(f"[Debug Visualizer SUCCESS] Saved visual detections overlay to: {debug_save_path}")
            except Exception as vis_err:
                print(f"[Debug Visualizer Note]: {vis_err}")
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

    # 2. Spatial Bucketing into Template Fields
    for f_key, cfg in fields_config.items():
        f_type = cfg.get("type", "text")
        if f_type in ["table", "circumstance_group"]:
            continue

        bbox = cfg.get("bbox", [0, 0, 0, 0])

        if f_key == "employment_status":
            raw_text = ""
            conf = 0.95
        else:
            raw_text, conf = assign_detections_to_field(f_key, cfg, all_detections)
            if not raw_text:
                crop = crop_roi(enhanced_image, bbox)
                if cell_has_ink(crop):
                    raw_text, conf = recognize_crop(ocr_engine, crop)

        cleaned, conf = clean_field_value(f_key, f_type, raw_text, conf, enhanced_image, ocr_engine, fields_config)

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

    # 3. Extract Section II Circumstances (Pen Marks / Encircled detection)
    detected_circumstance = detect_encircled_circumstance(enhanced_image, ocr_engine, template)
    if detected_circumstance.get("detected"):
        print(f"[Circumstance Detected] Code: {detected_circumstance.get('code')} -> '{detected_circumstance.get('label')}' (conf: {detected_circumstance.get('confidence')})")

    # 4. Extract Section III/V Family Dependents Table
    family_members = parse_family_table_grid(ocr_engine, enhanced_image, fields_config, all_detections)

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

def parse_form_with_ocr(ocr_engine, enhanced_image: np.ndarray, template: dict = None, mode: str = "global") -> dict:
    """
    Main extraction dispatcher.
    mode='global': High-Performance Full-Page Spatial Bucketed Extractor (Default).
    mode='crop': Legacy Field-by-Field ROI Crop Extractor (Fallback).
    """
    if mode == "crop":
        return parse_form_with_crop_ocr(ocr_engine, enhanced_image, template)
    return parse_form_with_global_ocr(ocr_engine, enhanced_image, template)

