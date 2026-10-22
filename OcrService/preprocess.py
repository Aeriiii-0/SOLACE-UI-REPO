import os
import cv2
import numpy as np
import base64

def crop_document_contour(image: np.ndarray) -> np.ndarray:
    """
    Detects the 4-corner boundary of a paper document in a photo
    and warps it to fit the frame, eliminating desk/camera borders.
    If no clear paper boundary exists (e.g. flatbed scan), returns original image unchanged.
    """
    if image is None or image.size == 0:
        return image
    try:
        h, w = image.shape[:2]
        scale = 800.0 / float(max(h, w))
        if scale < 1.0:
            small = cv2.resize(image, (int(w * scale), int(h * scale)), interpolation=cv2.INTER_AREA)
        else:
            small = image.copy()
            scale = 1.0

        sh, sw = small.shape[:2]
        gray = cv2.cvtColor(small, cv2.COLOR_BGR2GRAY) if len(small.shape) == 3 else small
        blurred = cv2.GaussianBlur(gray, (5, 5), 0)
        edged = cv2.Canny(blurred, 30, 120)
        kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (5, 5))
        closed = cv2.morphologyEx(edged, cv2.MORPH_CLOSE, kernel)

        contours, _ = cv2.findContours(closed, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        if not contours:
            return image

        contours = sorted(contours, key=cv2.contourArea, reverse=True)
        img_area = float(sh * sw)
        doc_pts = None

        for c in contours[:5]:
            area = cv2.contourArea(c)
            if area < 0.35 * img_area:
                continue
            peri = cv2.arcLength(c, True)
            approx = cv2.approxPolyDP(c, 0.02 * peri, True)
            if len(approx) == 4:
                doc_pts = approx.reshape(4, 2) / scale
                break

        if doc_pts is None:
            return image

        pts = np.zeros((4, 2), dtype="float32")
        s = doc_pts.sum(axis=1)
        pts[0] = doc_pts[np.argmin(s)]
        pts[2] = doc_pts[np.argmax(s)]
        diff = np.diff(doc_pts, axis=1)
        pts[1] = doc_pts[np.argmin(diff)]
        pts[3] = doc_pts[np.argmax(diff)]

        (tl, tr, br, bl) = pts
        width_top = np.linalg.norm(tr - tl)
        width_bot = np.linalg.norm(br - bl)
        max_w = int(max(width_top, width_bot))

        height_left = np.linalg.norm(bl - tl)
        height_right = np.linalg.norm(br - tr)
        max_h = int(max(height_left, height_right))

        aspect = float(max_w) / float(max(1, max_h))
        if not (0.50 <= aspect <= 0.90) or max_w < 300 or max_h < 400:
            return image

        dst = np.array([
            [0, 0],
            [max_w - 1, 0],
            [max_w - 1, max_h - 1],
            [0, max_h - 1]
        ], dtype="float32")

        M = cv2.getPerspectiveTransform(pts, dst)
        warped = cv2.warpPerspective(image, M, (max_w, max_h), flags=cv2.INTER_CUBIC)
        print(f"[Document Dewarp SUCCESS] Auto-cropped paper from photo: {w}x{h} -> {max_w}x{max_h} (aspect: {aspect:.2f})")
        return warped
    except Exception as e:
        print(f"[Document Dewarp Note]: {e}")
        return image


def deskew(image: np.ndarray) -> np.ndarray:
    """
    Detects the tilt angle of the document and rotates it straight (only if between 0.8 and 15 deg).
    Preserves exact document dimensions and aspect ratio.
    """
    try:
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY) if len(image.shape) == 3 else image

        # Invert and threshold to find text contours
        thresh = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)[1]
        coords = np.column_stack(np.where(thresh > 0))
        if len(coords) == 0:
            return image
        
        angle = cv2.minAreaRect(coords)[-1]

        if angle < -45:
            angle = -(90 + angle)
        elif angle > 45:
            angle = -(angle - 90)
        else:
            angle = -angle
        
        # Only rotate if tilt is between 0.8 and 15 degrees
        if abs(angle) < 0.8 or abs(angle) > 15:
            return image
            
        (h, w) = image.shape[:2]
        center = (w // 2, h // 2)
        M = cv2.getRotationMatrix2D(center, angle, 1.0)
        rotated = cv2.warpAffine(
            image, M, (w, h),
            flags=cv2.INTER_CUBIC,
            borderMode=cv2.BORDER_REPLICATE
        )
        return rotated
    except Exception:
        return image

def enhance_contrast(image: np.ndarray) -> np.ndarray:
    """
    Converts to grayscale and applies CLAHE to make handwritten text legible.
    """
    if len(image.shape) == 3:
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    else:
        gray = image.copy()
        
    # Contrast Limited Adaptive Histogram Equalization
    clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
    enhanced = clahe.apply(gray)
    
    # Gentle filter that removes paper grain without blurring out fine 1-2px pen strokes
    denoised = cv2.bilateralFilter(enhanced, d=3, sigmaColor=15, sigmaSpace=15)
    return denoised

def crop_roi(image: np.ndarray, bbox: list) -> np.ndarray:
    """
    Crops a region from the image using normalized coordinates [x, y, w, h] (0.0 to 1.0).
    """
    h, w = image.shape[:2]
    norm_x, norm_y, norm_w, norm_h = bbox
    
    # Convert normalized (0.0 - 1.0) to pixel coordinates
    x1 = max(0, int(norm_x * w))
    y1 = max(0, int(norm_y * h))
    x2 = min(w, int((norm_x + norm_w) * w))
    y2 = min(h, int((norm_y + norm_h) * h))
    
    # Safety check for valid bounding box
    if x2 <= x1 or y2 <= y1:
        return np.zeros((10, 10), dtype=np.uint8)
        
    return image[y1:y2, x1:x2]

def suppress_horizontal_lines(image: np.ndarray) -> np.ndarray:
    """
    Suppresses pre-printed horizontal form underlines that touch or interfere
    with handwritten characters without removing vertical/curved strokes.
    """
    if image is None or image.size == 0:
        return image
    try:
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY) if len(image.shape) == 3 else image
        thresh = cv2.adaptiveThreshold(gray, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY_INV, 15, 8)
        line_len = max(20, int(image.shape[1] * 0.20))
        h_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (line_len, 1))
        lines = cv2.morphologyEx(thresh, cv2.MORPH_OPEN, h_kernel)
        if np.count_nonzero(lines) == 0:
            return image
        result = image.copy()
        if len(result.shape) == 3:
            result[lines > 0] = [255, 255, 255]
        else:
            result[lines > 0] = 255
        return result
    except Exception:
        return image

def resize_for_ocr(image: np.ndarray, max_dim: int = 1800) -> np.ndarray:
    """
    Resizes scans uniformly to max_dim, strictly preserving aspect ratio
    so normalized coordinates (0.0 to 1.0) remain 100% invariant.
    """
    h, w = image.shape[:2]
    if max(h, w) > max_dim:
        scale = max_dim / float(max(h, w))
        new_w = int(w * scale)
        new_h = int(h * scale)
        return cv2.resize(image, (new_w, new_h), interpolation=cv2.INTER_AREA)
    return image

def preprocess_pipeline(image_bytes: bytes):
    """
    Decodes raw image bytes:
    1. Straightens document tilt if skewed (preserving exact geometry and aspect ratio).
    2. Resizes proportionally (preserving exact aspect ratio so UI canvas coordinates match 1:1).
    3. Contrast-enhances with CLAHE and bilateral filtering.
    Returns: (color_optimized_image, enhanced_grayscale_image)
    """
    nparr = np.frombuffer(image_bytes, np.uint8)
    image = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    if image is None:
        return None, None

    # 0. Auto-detect paper boundaries and dewarp if photo contains desk/margins
    cropped_doc = crop_document_contour(image)

    # 1. Straighten document tilt if skewed (preserves exact dimensions)
    deskewed = deskew(cropped_doc)
        
    # 2. Resize proportionally (preserves exact aspect ratio and normalized coords)
    optimized = resize_for_ocr(deskewed, max_dim=1800)
    
    # 3. Contrast enhancement for OCR reading
    enhanced = enhance_contrast(optimized)
    
    return optimized, enhanced

def image_to_base64(image: np.ndarray, format: str = ".jpg") -> str:
    """
    Encodes an OpenCV image to a Base64 string for WPF UI preview.
    """
    success, buffer = cv2.imencode(format, image)
    if not success:
        return ""
    return base64.b64encode(buffer).decode("utf-8")
