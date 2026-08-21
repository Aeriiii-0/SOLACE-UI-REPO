import cv2
import numpy as np
import base64

def deskew(image: np.ndarray) -> np.ndarray:
    """
    Detects the tilt angle of the document and rotates it straight (only if between 0.8 and 15 deg).
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
    
    # Slight bilateral filter to reduce paper grain noise while preserving sharp pen strokes
    denoised = cv2.bilateralFilter(enhanced, d=5, sigmaColor=50, sigmaSpace=50)
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

def resize_for_ocr(image: np.ndarray, max_dim: int = 1200) -> np.ndarray:
    """
    Downscales scans to ~1200px (150 DPI) for ~1.5s CPU inference
    and optimal DBNet text line detection accuracy.
    """
    h, w = image.shape[:2]
    if max(h, w) > max_dim:
        scale = max_dim / float(max(h, w))
        new_w = int(w * scale)
        new_h = int(h * scale)
        return cv2.resize(image, (new_w, new_h), interpolation=cv2.INTER_AREA)
    return image

def auto_crop_paper_boundaries(image: np.ndarray) -> np.ndarray:
    """
    Detects paper borders in flatbed scans that include dark/black scanner glass edges,
    and crops cleanly to the document paper rectangle so field ratios match.
    """
    try:
        h, w = image.shape[:2]
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY) if len(image.shape) == 3 else image
        
        # Check if outer edges have dark scanner glass margins (< 70 mean intensity)
        border_thickness = max(5, int(min(h, w) * 0.02))
        top_edge = np.mean(gray[:border_thickness, :])
        bottom_edge = np.mean(gray[-border_thickness:, :])
        left_edge = np.mean(gray[:, :border_thickness])
        right_edge = np.mean(gray[:, -border_thickness:])
        
        if top_edge < 70 or bottom_edge < 70 or left_edge < 70 or right_edge < 70:
            _, thresh = cv2.threshold(gray, 75, 255, cv2.THRESH_BINARY)
            contours, _ = cv2.findContours(thresh, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
            if contours:
                largest = max(contours, key=cv2.contourArea)
                x, y, cw, ch = cv2.boundingRect(largest)
                # Only crop if paper area covers at least 60% of image
                if (cw * ch) > (w * h * 0.60):
                    return image[y:y+ch, x:x+cw]
    except Exception:
        pass
    return image

def preprocess_pipeline(image_bytes: bytes):
    """
    Decodes raw image bytes, crops scanner borders, resizes large scans to optimal resolution,
    applies deskewing, and enhances contrast.
    Returns: (color_deskewed_image, enhanced_grayscale_image)
    """
    nparr = np.frombuffer(image_bytes, np.uint8)
    image = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    if image is None:
        return None, None
        
    # 1. Auto-crop dark scanner borders if present
    cropped_paper = auto_crop_paper_boundaries(image)

    # 2. Resize large scans to ~1200px for rapid CPU processing
    optimized = resize_for_ocr(cropped_paper, max_dim=1200)
    
    # 3. Deskew
    deskewed = deskew(optimized)
    
    # 4. Contrast enhance for OCR reading
    enhanced = enhance_contrast(deskewed)
    
    return deskewed, enhanced

def image_to_base64(image: np.ndarray, format: str = ".jpg") -> str:
    """
    Encodes an OpenCV image to a Base64 string for WPF UI preview.
    """
    success, buffer = cv2.imencode(format, image)
    if not success:
        return ""
    return base64.b64encode(buffer).decode("utf-8")