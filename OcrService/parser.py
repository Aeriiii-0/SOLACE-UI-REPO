import os

# Configure Paddle runtime environment before any other imports
os.environ["PADDLE_PDX_ENABLE_MKLDNN_BYDEFAULT"] = "False"
os.environ["PADDLE_PDX_USE_PIR_TRT"] = "False"
os.environ["FLAGS_enable_pir_api"] = "0"
os.environ["FLAGS_use_mkldnn"] = "0"

"""
parser.py - Unified Facade for Solace OCR Processing Engine.

Decomposed into dedicated single-responsibility modules:
- cleaners.py: Sanitization, fuzzy dictionary matching, Philippine date/name/barangay normalizers.
- inference.py: Native OpenVINO 2-bucket engine, PP-OCRv5 RepSVTR recognition model.
- labels.py: Printed form label noise filters, dynamic label anchors, spatial detection mapping.
- sections.py: Circumstance detection (Section II), checkbox ink analysis, family table grid.
- pipeline.py: Dynamic scanner alignment, full-page global extraction, crop fallback pipeline.
"""

from cleaners import (
    BINAN_BARANGAYS,
    BARANGAY_ALIASES,
    normalize_for_matching,
    clean_text,
    is_ocr_na,
    match_barangay,
    clean_income,
    clean_civil_status,
    clean_relationship,
    clean_education,
    parse_month_word,
    clean_person_name,
    clean_family_age,
    normalize_handwritten_year,
    clean_date,
    clean_sex,
    clean_philsys_number,
    clean_phone_number,
    clean_extension,
    clean_religion,
    get_confidence_rating,
)

from inference import (
    OpenVINOTextRecognizer,
    get_ov_recognizer,
    prepare_static_bucket_canvas,
    get_v5_recognizer,
    recognize_with_v5_model,
    recognize_with_server_model,
    recognize_crop,
    extract_detections_from_paddle,
)

from labels import (
    ENABLE_ROBUST_LABEL_FILTER,
    GLOBAL_PRINTED_LABEL_PATTERNS,
    FORM_LABEL_PREFIXES,
    LABEL_ANCHOR_DEFINITIONS,
    is_printed_label_word,
    strip_form_label,
    extract_by_anchor_label,
    assign_detections_to_field,
    clean_field_value,
)

from sections import (
    CIRCUMSTANCES_TABLE,
    cell_has_ink,
    detect_encircled_circumstance,
    measure_checkbox_ink,
    detect_employment_status,
    parse_family_table_grid,
)

from pipeline import (
    DBNET_CROP_PADDING_PX,
    ANCHOR_REGISTRATION_TARGETS,
    load_template,
    calculate_dynamic_page_offset,
    calculate_dynamic_page_transform,
    parse_form_with_global_ocr,
    parse_form_with_crop_ocr,
    parse_form_with_ocr,
)

__all__ = [
    # Cleaners
    "BINAN_BARANGAYS",
    "BARANGAY_ALIASES",
    "normalize_for_matching",
    "clean_text",
    "is_ocr_na",
    "match_barangay",
    "clean_income",
    "clean_civil_status",
    "clean_relationship",
    "clean_education",
    "parse_month_word",
    "clean_person_name",
    "clean_family_age",
    "normalize_handwritten_year",
    "clean_date",
    "clean_sex",
    "clean_philsys_number",
    "clean_phone_number",
    "clean_extension",
    "clean_religion",
    "get_confidence_rating",
    # Inference
    "OpenVINOTextRecognizer",
    "get_ov_recognizer",
    "prepare_static_bucket_canvas",
    "get_v5_recognizer",
    "recognize_with_v5_model",
    "recognize_with_server_model",
    "recognize_crop",
    "extract_detections_from_paddle",
    # Labels
    "ENABLE_ROBUST_LABEL_FILTER",
    "GLOBAL_PRINTED_LABEL_PATTERNS",
    "FORM_LABEL_PREFIXES",
    "LABEL_ANCHOR_DEFINITIONS",
    "is_printed_label_word",
    "strip_form_label",
    "extract_by_anchor_label",
    "assign_detections_to_field",
    "clean_field_value",
    # Sections
    "CIRCUMSTANCES_TABLE",
    "cell_has_ink",
    "detect_encircled_circumstance",
    "measure_checkbox_ink",
    "detect_employment_status",
    "parse_family_table_grid",
    # Pipeline
    "DBNET_CROP_PADDING_PX",
    "ANCHOR_REGISTRATION_TARGETS",
    "load_template",
    "calculate_dynamic_page_offset",
    "calculate_dynamic_page_transform",
    "parse_form_with_global_ocr",
    "parse_form_with_crop_ocr",
    "parse_form_with_ocr",
]
