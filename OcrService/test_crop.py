import os
import sys

os.environ["PADDLE_PDX_ENABLE_MKLDNN_BYDEFAULT"] = "False"
os.environ["FLAGS_enable_pir_api"] = "0"
os.environ["FLAGS_use_mkldnn"] = "0"

import cv2
from paddleocr import PaddleOCR

def test_single_field(image_path: str, field_name="Last Name", bbox=[0.025, 0.180, 0.225, 0.040]):
    """
    Crops 1 singular field from the image, saves the crop as a JPG for visual inspection,
    and runs OCR directly on it.
    """
    print(f"==================================================")
    print(f"     TESTING 1 SINGULAR FIELD: '{field_name}'     ")
    print(f"==================================================")

    # 1. Load image
    if not os.path.exists(image_path):
        print(f"[Error] File not found: {image_path}")
        return

    img = cv2.imread(image_path)
    if img is None:
        print(f"[Error] Could not decode image from {image_path}")
        return

    h, w = img.shape[:2]
    print(f"[Step 1] Image Loaded: {w}x{h} px")

    # 2. Crop the exact field bounding box
    norm_x, norm_y, norm_w, norm_h = bbox
    x1 = int(norm_x * w)
    y1 = int(norm_y * h)
    x2 = int((norm_x + norm_w) * w)
    y2 = int((norm_y + norm_h) * h)

    crop = img[y1:y2, x1:x2]
    crop_filename = f"crop_{field_name.lower().replace(' ', '_')}.jpg"
    cv2.imwrite(crop_filename, crop)
    print(f"[Step 2] Cropped Region ({crop.shape[1]}x{crop.shape[0]}px) saved to: {crop_filename}")
    print(f"         (You can open '{crop_filename}' in Windows to see the exact pixels!)")

    # 3. Add white border margin for standard OCR contrast
    padded_crop = cv2.copyMakeBorder(crop, 25, 25, 25, 25, cv2.BORDER_CONSTANT, value=[255, 255, 255])

    # 4. Initialize PaddleOCR
    print("\n[Step 3] Initializing PaddleOCR...")
    ocr = PaddleOCR(
        use_doc_orientation_classify=False,
        use_doc_unwarping=False,
        use_textline_orientation=False,
        ocr_version="PP-OCRv4",
        lang="en"
    )

    # 5. Run OCR on this 1 field
    print(f"[Step 4] Running OCR on '{crop_filename}'...")
    res = list(ocr.predict(padded_crop))

    print("\n==================================================")
    print("                 OCR RESULT                       ")
    print("==================================================")
    print(f"Number of prediction results: {len(res)}")
    
    if res:
        for r_idx, first in enumerate(res):
            print(f"\n--- Result #{r_idx+1} ---")
            if hasattr(first, "keys") or isinstance(first, dict):
                res_dict = dict(first) if hasattr(first, "keys") else first
                
                texts = res_dict.get("rec_texts")
                scores = res_dict.get("rec_scores")
                boxes = res_dict.get("rec_boxes")
                
                if texts is None: texts = res_dict.get("rec_text", [])
                if scores is None: scores = res_dict.get("rec_score", [])
                if boxes is None: boxes = res_dict.get("dt_polys", [])
                
                if hasattr(texts, "tolist"): texts = texts.tolist()
                if hasattr(scores, "tolist"): scores = scores.tolist()
                if hasattr(boxes, "tolist"): boxes = boxes.tolist()
                
                print(f"\n==================================================")
                print(f" [SUCCESS] DETECTED {len(texts)} TEXT LINE(S) IN CROP:")
                print(f"==================================================")
                for i in range(len(texts)):
                    t = texts[i]
                    s = scores[i] if (isinstance(scores, (list, tuple)) and i < len(scores)) else 1.0
                    b = boxes[i] if (isinstance(boxes, (list, tuple)) and i < len(boxes)) else []
                    print(f"  [{i+1}] TEXT: \"{t}\" (Confidence: {s:.2f})")
                    print(f"      Bounding Box: {b}")
                print(f"==================================================")
            elif isinstance(first, (list, tuple)):
                print(f"List/tuple output: {first}")
            else:
                print(f"Raw object output: {first}")
    else:
        print("No text detected in crop.")
    print("==================================================\n")

if __name__ == "__main__":
    if len(sys.argv) > 1:
        path = sys.argv[1]
    else:
        candidates = [f for f in os.listdir(".") if f.lower().endswith((".jpg", ".png", ".jpeg"))]
        path = candidates[0] if candidates else "sample.jpg"
        
    test_single_field(path, "Last Name", [0.025, 0.180, 0.225, 0.040])
