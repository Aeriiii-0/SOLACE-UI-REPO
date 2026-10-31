import re
import difflib

# Official list of 24 Barangays of the City of Biñan
BINAN_BARANGAYS = [
    "Biñan (Poblacion)", "Bungahan", "Canlalay", "Casile", "De La Paz",
    "Ganado", "Langkiwa", "Loma", "Malaban", "Malamig", "Mambog",
    "Platero", "Poblacion", "San Antonio", "San Francisco (Halang)",
    "San Jose", "San Vicente", "Santo Domingo", "Santo Niño", "Santo Tomas (Calabuso)",
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
    "sto domingo": "Santo Domingo",
    "sto. domingo": "Santo Domingo",
    "santo domingo": "Santo Domingo",
    "sto nino": "Santo Niño",
    "sto. nino": "Santo Niño",
    "sto niño": "Santo Niño",
    "sto. niño": "Santo Niño",
    "santo nino": "Santo Niño",
    "delapaz": "De La Paz",
    "dela paz": "De La Paz",
    "mambog": "Mambog",
    "mampalasan": "Mambog",
    "mamplasan": "Mambog",
    "sorosoro": "Soro-Soro",
    "soro soro": "Soro-Soro",
    "binan poblacion": "Biñan (Poblacion)",
    "binan (poblacion)": "Biñan (Poblacion)",
    "biñan (poblacion)": "Biñan (Poblacion)",
    "binan": "Biñan (Poblacion)",
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

def clean_text(text: str) -> str:
    """Removes stray OCR artifacts, non-Latin/non-Filipino chars, and trims whitespace."""
    if not text:
        return ""
    text = re.sub(r'[^\x00-\x7F\u00f1\u00d1\u20b1]+', ' ', text)
    cleaned = re.sub(r'[\r\n\t_]+', ' ', text)
    cleaned = re.sub(r'\s+', ' ', cleaned)
    return cleaned.strip().upper()

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

    # If raw text contains barangay followed by city/province suffix (e.g. "San Jose, Biñan, Laguna"),
    # strip city/province suffix so the actual barangay name is accurately matched.
    stripped_suffix = re.sub(r'[\s,]+(city\s*of\s*)?bi[nñ]an\b.*$', '', cleaned, flags=re.IGNORECASE).strip()
    stripped_suffix = re.sub(r'[\s,]+laguna\b.*$', '', stripped_suffix, flags=re.IGNORECASE).strip()
    if stripped_suffix and len(stripped_suffix) >= 3:
        cleaned = stripped_suffix

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
    """Fuzzy normalization of Civil Status, including shortened letters (S, M, W, D, A, SEP)."""
    if not raw_text or is_ocr_na(raw_text):
        return ""
    t = raw_text.strip().lower()
    t = re.sub(r'^(civil\s*s?tatus|status|kasarian)\s*[:\-_.]*', '', t, flags=re.IGNORECASE).strip()
    t = re.sub(r'civil\s*s?tatus', '', t, flags=re.IGNORECASE).strip()
    
    t_clean = re.sub(r'[^a-z0-9]', '', t)
    t_alpha = re.sub(r'[^a-z]', '', t)
    
    # 1. Single letter or exact shortened abbreviations
    if t_alpha in ["s", "sin", "sngl", "sing", "simgle", "simglt", "singl"] or t_clean in ["s", "5", "s5", "5s"]:
        return "Single"
    if t_alpha in ["m", "mar", "marr", "mared", "marrd", "mrd"] or t_clean in ["m", "rn"]:
        return "Married"
    if t_alpha in ["d", "div", "dv"] or t_clean in ["d"]:
        return "Divorced"
    if t_alpha in ["a", "ann", "anl", "anul", "anulled", "annul"] or t_clean in ["a", "4"]:
        return "Annulled"
    if t_alpha in ["w", "wid", "wdw"] or t in ["biyuda", "biyudo", "balo"] or t_clean in ["w", "vv"]:
        return "Widowed"
    if t_alpha in ["sep", "sp"] or "s/p" in t or t in ["hiwalay", "separated"] or t_clean in ["sep", "sp"]:
        return "Separated"
    if t_alpha in ["c", "live", "livein", "kinakasama"]:
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
    if any(k in t for k in ["divorc", "diborsyo"]):
        return "Divorced"
    if any(k in t for k in ["annul", "nullity", "pawalang"]):
        return "Annulled"
    if any(k in t for k in ["common", "live-in", "live in", "kinakasama"]):
        return "Common-law / Live-in"

    # 3. Fuzzy similarity
    for standard in ["Single", "Married", "Widowed", "Separated", "Divorced", "Annulled"]:
        if difflib.SequenceMatcher(None, t_alpha, standard.lower()).ratio() >= 0.65:
            return standard

    val = clean_text(raw_text)
    return val if val else ""

def clean_relationship(raw_text: str) -> str:
    """
    Fuzzy normalization of Relationship snapping to exact RecordDialog dropdown items:
    Child, Spouse, Parent, Sibling, Grandchild, Grandparent, Relative, Ward, Other.
    """
    if not raw_text or is_ocr_na(raw_text):
        return ""
    t = raw_text.strip().lower()
    t_clean = re.sub(r'[^a-z]', '', t)
    
    if any(k in t for k in ["child", "anak", "son", "daughter", "dunghter", "daught", "vannaht", "vanahter", "uunanter", "uumoto", "bata"]):
        return "Child"
    if any(k in t for k in ["spouse", "asawa", "husband", "wife", "partner"]):
        return "Spouse"
    if any(k in t for k in ["mother", "father", "nanay", "tatay", "ina", "ama", "mom", "dad", "parent", "mama", "papa"]):
        return "Parent"
    if any(k in t for k in ["sister", "brother", "ate", "kuya", "kapatid", "sibling", "bro", "sis"]):
        return "Sibling"
    if any(k in t for k in ["grandchild", "apo", "grandson", "granddaughter"]):
        return "Grandchild"
    if any(k in t for k in ["grandmother", "grandfather", "lolo", "lola", "grandparent"]):
        return "Grandparent"
    if any(k in t for k in ["relative", "aunt", "uncle", "tita", "tito", "cousin", "pinsan", "in-law", "in law", "hipag", "bayaw", "kamag"]):
        return "Relative"
    if any(k in t for k in ["ward", "alaga", "guardian", "tagapangalaga"]):
        return "Ward"
    if any(k in t for k in ["other", "friend", "kaibigan", "neighbor", "kapitbahay"]):
        return "Other"

    std_relationships = ["Child", "Spouse", "Parent", "Sibling", "Grandchild", "Grandparent", "Relative", "Ward", "Other"]
    for rel in std_relationships:
        if difflib.SequenceMatcher(None, t_clean, rel.lower()).ratio() >= 0.65:
            return rel

    val = clean_text(raw_text)
    return "" if is_ocr_na(val) else val

def clean_education(raw_text: str) -> str:
    """
    Fuzzy normalization of Educational Attainment snapping to exact RecordDialog dropdown items:
    Elementary, High School, Vocational, College, Post Graduate, or Student/None.
    """
    if not raw_text:
        return ""
    if is_ocr_na(raw_text) and raw_text.strip().lower() not in ["none", "wala", "not attending"]:
        return ""
    t = raw_text.strip().lower()
    t_clean = re.sub(r'[^a-z0-9]', '', t)

    # 1. Early Childhood / Kindergarten / Preschool
    if any(k in t for k in ["kinder", "nursery", "day care", "daycare", "pre-school", "preschool", "prep", "pre-elem"]):
        return "Elementary"

    # 2. Grade levels (G1-G6 -> Elementary, G7-G12 -> High School)
    if re.search(r'\b(g|gr|grade)[\.\s\-]*(1[0-2]|[7-9])\b', t) or re.search(r'\b(jhs|shs|junior\s*high|senior\s*high)\b', t):
        return "High School"
    if re.search(r'\b(g|gr|grade)[\.\s\-]*[1-6]\b', t):
        return "Elementary"

    # 3. Keyword containment & Philippine education terminology
    if any(k in t for k in ["elem", "primary", "grade", "elementaria"]):
        return "Elementary"
    if any(k in t for k in ["high", "hs", "h.s", "sec", "secondary", "junior high", "senior high", "highschool"]):
        return "High School"
    if any(k in t for k in ["voc", "tesda", "tech", "trade", "course", "vocational"]):
        return "Vocational"
    if any(k in t for k in ["post", "grad", "master", "doctor", "phd", "m.s", "m.a", "post graduate"]):
        return "Post Graduate"
    if any(k in t for k in ["col", "univ", "bach", "bs", "ba", "degree", "undergrad", "college", "tertiary"]):
        return "College"
    if any(k in t for k in ["none", "wala", "not attending"]):
        return "None"
    if any(k in t for k in ["student", "estudyante", "pupil"]):
        return "Student"

    # 4. Fuzzy similarity across standard dropdown choices
    options = ["Elementary", "High School", "Vocational", "College", "Post Graduate"]
    best_opt = ""
    best_score = 0.0
    for opt in options:
        score = difflib.SequenceMatcher(None, t_clean, opt.lower().replace(" ", "")).ratio()
        if score > best_score:
            best_score = score
            best_opt = opt
    if best_opt and best_score >= 0.55:
        return best_opt

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
    - Strips residual printed form label prefixes
    - Converts OCR digit confusions (4 -> A, 0 -> O)
    - Strips all non-letter characters except ñ/Ñ, space, period, hyphen, apostrophe
    - Removes duplicate whitespace and trims
    """
    if not raw_name or is_ocr_na(raw_name):
        return ""
    t = raw_name.strip()
    t = re.sub(r'^\s*\(?\s*([iIvVxX]+|[0-9]+)\s*[\.\)]\s*', '', t)
    t = re.sub(r'(person\s*t?\s*o?\s*be\s*contact(ed|ea)?|in\s*(c?ase)?\s*of\s*(emergency|cmergency|e?mergen[a-z]*)|emergency\s*(contact|person|name|a?m[eo])?)', '', t, flags=re.IGNORECASE)
    t = re.sub(r'^\s*\(?\s*(name\s*)?(last\s*(name|a?m[eo])?|surname|first\s*(name|a?m[eo])?|m[i1lI]dd?[li1I]?e?\s*(name|a?m[eo])?|middle|gitnang\s*pangalan)\b\s*\)?\s*[:\-_.]*\s*', '', t, flags=re.IGNORECASE)
    t = re.sub(r'4', 'A', t)
    t = re.sub(r'0', 'O', t)
    t = re.sub(r'(?<=[a-zA-Z])1(?=[a-zA-Z])', 'L', t)
    t = re.sub(r'\bZOR\b', 'ZORA', t, flags=re.IGNORECASE)
    t = re.sub(r"[^a-zA-ZñÑ\s\.\-']", "", t)
    t = re.sub(r'\s+', ' ', t).strip()
    placeholder_labels = {
        'LAST NAME', 'FIRST NAME', 'MIDDLE NAME', 'MIDDLENAME', 'EXT NAME', 'EXTENSION NAME',
        'NAME', 'MEMBER NAME', 'MEMBERNAME', 'MEMBERNAMEA', 'CONTACT', 'PERSON TO BE CONTACTED',
        'EMERGENCY CONTACT', 'SURNAME', 'APELYIDO', 'PANGALAN', 'GITNANG PANGALAN'
    }
    if t.upper() in placeholder_labels:
        return ''
    return t.upper()

def clean_family_age(raw_age: str) -> str:
    """Normalizes age to numbers only (0 <= age <= 120)."""
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

def normalize_handwritten_year(yr: int) -> int:
    """Corrects common OCR misreads of 19XX/20XX century digits (e.g. 1473, 1073, 1773)."""
    if 1920 <= yr <= 2030:
        return yr
    last2 = yr % 100
    if 25 <= last2 <= 99:
        return 1900 + last2
    elif last2 <= 24:
        return 2000 + last2
    return yr

def clean_date(raw_text: str) -> str:
    """
    Extracts and normalizes dates from handwritten or printed text.
    Handles 'June 19, 2004', OCR merged 'Jum192004', '19 June 2004', MM/DD/YYYY, YYYY-MM-DD, MMDDYYYY, MMDDYY.
    Tolerates OCR character confusion in days ('Q'->'2', 'Z'->'2', 'O'->'0', etc.) and century misreads ('1473'->'1973').
    Returns normalized YYYY-MM-DD or empty string '' if no valid date is found.
    """
    if not raw_text or is_ocr_na(raw_text):
        return ""
    raw = raw_text.strip()
    
    # Strip common label prefixes even if preceded by noise
    raw = re.sub(r'^(.*?)(date\s*of\s*(this\s*)?application|application\s*date|petsa(\s*ng\s*aplikasyon)?|date\s*of\s*birth|birthdate|dob|kaarawan|bday|date)\s*[:\-_.]*', '', raw, flags=re.IGNORECASE).strip()
    raw = re.sub(r'^\s*\(?\s*m+[/.]?d+[/.]?y+\s*\)?\s*', '', raw, flags=re.IGNORECASE).strip()
    if not raw or is_ocr_na(raw):
        return ""

    # 1. Text month + day + year (e.g. 'June 19, 2004', 'May 2, 2002', '19 June 2004', '2-May-2002', 'Jum192004', 'MAY 21973')
    if re.search(r'[a-zA-Z]{3,}', raw):
        DIGIT_CONFUSIONS = {'Q': '2', 'q': '2', 'Z': '2', 'z': '2', 'O': '0', 'o': '0', 'l': '1', 'I': '1', 'i': '1', '|': '1', 'S': '5', 's': '5', 'B': '8'}
        def replace_char_day(m):
            m_txt = m.group(1)
            char_d = m.group(2)
            yr = m.group(3)
            num_d = "".join(DIGIT_CONFUSIONS.get(c, c) for c in char_d)
            return f"{m_txt} {num_d}, {yr}"
        raw = re.sub(r'\b([a-zA-Z]{3,})[\s\-_,]+([QZOlISB0-9]{1,2})[\s\-_,]+(\d{4}|\d{2})\b', replace_char_day, raw, flags=re.IGNORECASE)

        # Day followed by Month word and year (e.g. '19 June 2004', '2 May 2002', '2-May-2002', '19th June 2004')
        m_day_first = re.search(r'\b(\d{1,2})(?:st|nd|rd|th)?[\s\-_,]+([a-zA-Z]{3,})[\s\-_,]+(\d{4}|\d{2})\b', raw)
        if m_day_first:
            day_str, m_word, yr_str = m_day_first.groups()
            m_num = parse_month_word(m_word)
            if m_num and 1 <= int(day_str) <= 31:
                yr = int(yr_str) if len(yr_str) == 4 else (2000 + int(yr_str) if int(yr_str) < 40 else 1900 + int(yr_str))
                yr = normalize_handwritten_year(yr)
                return f"{yr:04d}-{m_num:02d}-{int(day_str):02d}"

        # Month word followed by day and year (e.g. 'June 19, 2004', 'May 2, 2002', 'MAY 21973', 'May 2 1973')
        m_month_first = re.search(r'\b([a-zA-Z]{3,})[\s\-_,]*(\d{1,2})(?:st|nd|rd|th)?[\s\-_,]*(\d{4}|\d{2})\b', raw)
        if m_month_first:
            m_word, day_str, yr_str = m_month_first.groups()
            m_num = parse_month_word(m_word)
            if m_num and 1 <= int(day_str) <= 31:
                yr = int(yr_str) if len(yr_str) == 4 else (2000 + int(yr_str) if int(yr_str) < 40 else 1900 + int(yr_str))
                yr = normalize_handwritten_year(yr)
                return f"{yr:04d}-{m_num:02d}-{int(day_str):02d}"

        # Concatenated Month and digits (e.g. 'Jum192004', 'June192004')
        m_concat = re.search(r'\b([a-zA-Z]{3,})(\d{1,2})(\d{4})\b', raw)
        if m_concat:
            m_word, day_str, yr_str = m_concat.groups()
            m_num = parse_month_word(m_word)
            if m_num and 1 <= int(day_str) <= 31:
                yr = normalize_handwritten_year(int(yr_str))
                return f"{yr:04d}-{m_num:02d}-{int(day_str):02d}"

    # 2. Numeric with separators: DD/MM/YYYY, MM/DD/YYYY, YYYY-MM-DD
    m1 = re.search(r'(\d{1,4})[/\-.](\d{1,2})[/\-.](\d{2,4})', raw)
    if m1:
        p1, p2, p3 = m1.groups()
        if len(p1) == 4:
            yr = normalize_handwritten_year(int(p1))
            if 1 <= int(p2) <= 12 and 1 <= int(p3) <= 31:
                return f"{yr:04d}-{int(p2):02d}-{int(p3):02d}"
        else:
            yr = int(p3) if len(p3) == 4 else (2000 + int(p3) if int(p3) < 40 else 1900 + int(p3))
            yr = normalize_handwritten_year(yr)
            if int(p1) > 12 and int(p2) <= 12 and int(p1) <= 31:
                return f"{yr:04d}-{int(p2):02d}-{int(p1):02d}"
            elif int(p2) > 12 and int(p1) <= 12 and int(p2) <= 31:
                return f"{yr:04d}-{int(p1):02d}-{int(p2):02d}"
            elif int(p1) <= 31 and int(p2) <= 12:
                return f"{yr:04d}-{int(p2):02d}-{int(p1):02d}"
            elif int(p1) <= 12 and int(p2) <= 31:
                return f"{yr:04d}-{int(p1):02d}-{int(p2):02d}"

    # 2b. Missing one separator: e.g. '1/252020', '01/252020', '0125/2020'
    m_missing1 = re.search(r'(\d{1,2})[/\-.](\d{1,2})(\d{4})', raw)
    if m_missing1:
        p1, p2, yr = m_missing1.groups()
        yr_norm = normalize_handwritten_year(int(yr))
        if int(p1) > 12 and int(p2) <= 12:
            p1, p2 = p2, p1
        if 1 <= int(p1) <= 12 and 1 <= int(p2) <= 31:
            return f"{yr_norm:04d}-{int(p1):02d}-{int(p2):02d}"

    m_missing2 = re.search(r'(\d{1,2})(\d{2})[/\-.](\d{4})', raw)
    if m_missing2:
        p1, p2, yr = m_missing2.groups()
        yr_norm = normalize_handwritten_year(int(yr))
        if int(p1) > 12 and int(p2) <= 12:
            p1, p2 = p2, p1
        if 1 <= int(p1) <= 12 and 1 <= int(p2) <= 31:
            return f"{yr_norm:04d}-{int(p1):02d}-{int(p2):02d}"

    # 3. Pure 8-digit or 6-digit string
    digits_only = re.sub(r'\D', '', raw)
    if len(digits_only) == 8:
        y_cand, m_cand, d_cand = digits_only[:4], digits_only[4:6], digits_only[6:]
        yr_norm = normalize_handwritten_year(int(y_cand))
        if 1 <= int(m_cand) <= 12 and 1 <= int(d_cand) <= 31:
            return f"{yr_norm:04d}-{int(m_cand):02d}-{int(d_cand):02d}"
        m_s, d_s, y_s = digits_only[:2], digits_only[2:4], digits_only[4:]
        yr_norm2 = normalize_handwritten_year(int(y_s))
        if 1 <= int(m_s) <= 12 and 1 <= int(d_s) <= 31:
            return f"{yr_norm2:04d}-{int(m_s):02d}-{int(d_s):02d}"
            
    if len(digits_only) == 7:
        m_s, d_s, y_s = digits_only[:1], digits_only[1:3], digits_only[3:]
        yr_norm = normalize_handwritten_year(int(y_s))
        if 1 <= int(m_s) <= 9 and 1 <= int(d_s) <= 31:
            return f"{yr_norm:04d}-{int(m_s):02d}-{int(d_s):02d}"

    if len(digits_only) == 6:
        m_s, d_s, y_s = digits_only[:2], digits_only[2:4], digits_only[4:]
        if 1 <= int(m_s) <= 12 and 1 <= int(d_s) <= 31:
            yr = 2000 + int(y_s) if int(y_s) < 40 else 1900 + int(y_s)
            return f"{yr:04d}-{int(m_s):02d}-{int(d_s):02d}"

    return ""

def clean_sex(raw_text: str) -> str:
    """Fuzzy normalization of Sex field, robust to shortened F/M, OCR visual confusion, and punctuation."""
    if not raw_text or is_ocr_na(raw_text):
        return ""
    t = raw_text.strip().lower()
    t = re.sub(r'^(sex|gender|kasarian)\s*[:\-_.]*', '', t, flags=re.IGNORECASE).strip()
    t_alpha = re.sub(r'[^a-z0-9]', '', t)
    
    # Visual confusion mappings for handwriting in single-letter table cells:
    # Male: M often misread as H, N, IV, U, 11, 1 (due to table border lines or vertical strokes)
    # Female: F often misread as P, PL, E, T, 7 (due to horizontal crossbars or curved top)
    if t_alpha in ["m", "male", "lalaki", "boy", "son", "father", "brother", "l", "masc", "mas", "man", "h", "n", "iv", "u", "11", "1"]:
        return "Male"
    if t_alpha in ["f", "female", "babae", "fem", "girl", "daughter", "mother", "sister", "b", "woman", "p", "pl", "e", "t", "7"]:
        return "Female"
    if t_alpha.startswith("fem") or re.search(r'\b(female|babae|fem)\b', t):
        return "Female"
    if t_alpha.startswith("mas") or re.search(r'\b(male|lalaki)\b', t):
        return "Male"
    if t_alpha.startswith("f"):
        return "Female"
    if t_alpha.startswith("m") and t_alpha != "mother":
        return "Male"
    val = clean_text(raw_text)
    return "" if is_ocr_na(val) else val

def clean_philsys_number(raw_text: str) -> str:
    """
    Normalizes 16-digit PhilSys Card Number (XXXX-XXXX-XXXX-XXXX).
    Must contain at least 12-16 digits to be considered a valid PhilSys number.
    If the field only contains label noise (e.g. 'PHILYS CAR MBER', 'PHILSYS CARD NUMBER')
    or < 12 digits, it returns empty string '' to prevent label hallucination.
    """
    if not raw_text or is_ocr_na(raw_text):
        return ""
    t = raw_text.strip()

    # Strip label prefix
    t = re.sub(r'^(.*?)(philsys|philys)\s*(card|car)?\s*(no|number|mber)?\s*[:\-_.]*', '', t, flags=re.IGNORECASE).strip()
    if not t or is_ocr_na(t):
        return ""

    digits_raw = re.sub(r'\D', '', t)
    
    trans = str.maketrans({"O": "0", "o": "0", "I": "1", "i": "1", "l": "1", "L": "1", "|": "1", "S": "5", "s": "5", "B": "8", "Z": "2", "z": "2"})
    t_trans = t.translate(trans)
    digits = re.sub(r'\D', '', t_trans)

    if len(digits) == 16 and len(digits_raw) >= 6:
        return f"{digits[0:4]}-{digits[4:8]}-{digits[8:12]}-{digits[12:16]}"
    elif len(digits) >= 12 and len(digits_raw) >= 6:
        parts = [digits[i:i+4] for i in range(0, len(digits), 4)]
        return "-".join(parts)

    return ""

def clean_phone_number(raw_text: str) -> str:
    """
    Normalizes Philippine mobile/contact numbers to visual hyphenated format (09XX-XXX-XXXX).
    Handles +639..., 639..., 9XXXXXXXXX, spaces, dashes, and OCR letter confusion.
    """
    if not raw_text or is_ocr_na(raw_text):
        return ""
    t = raw_text.strip()
    trans = str.maketrans({"O": "0", "o": "0", "I": "1", "i": "1", "l": "1", "|": "1", "S": "5", "s": "5", "B": "8"})
    t = t.translate(trans)
    digits = re.sub(r'\D', '', t)
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
    elif len(digits) >= 7:
        return digits
    return ""

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
    
    # 1. Direct and fuzzy keyword matches for Catholic (Ctolc, Catholic, Katoliko, RC, etc.)
    cath_keywords = ["cath", "katol", "roman", "rc", "ctolc", "catol", "katlk", "cthlc", "cathl", "cato", "kato", "ctc"]
    if any(k in t_clean for k in cath_keywords) or any(k in t for k in ["catholic", "katoliko", "roman catholic", "r.c.", "r.c"]):
        return "Catholic"
        
    if difflib.SequenceMatcher(None, t_clean, "catholic").ratio() >= 0.45 or \
       difflib.SequenceMatcher(None, t_clean, "katoliko").ratio() >= 0.45:
        return "Catholic"

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

def get_confidence_rating(score: float) -> str:
    """Returns High, Moderate, or Low rating based on confidence score."""
    if score >= 0.80:
        return "High"
    elif score >= 0.50:
        return "Moderate"
    return "Low"
