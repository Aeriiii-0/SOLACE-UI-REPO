import os
import cv2
import numpy as np
import base64

def suppress_scanner_margins(image: np.ndarray) -> np.ndarray:
    """
    Removes thin dark shadow/border artifacts along the extreme outer edges (5px)
    commonly produced by flatbed glass frames or ADF rollers, without altering dimensions.
    """
    if image is None or image.size == 0:
        return image
    try:
        h, w = image.shape[:2]
        if h < 100 or w < 100:
            return image
        cleaned = image.copy()
        border_px = 5
        white_val = [255, 255, 255] if len(cleaned.shape) == 3 else 255
        cleaned[0:border_px, :] = white_val
        cleaned[h - border_px:h, :] = white_val
        cleaned[:, 0:border_px] = white_val
        cleaned[:, w - border_px:w] = white_val
        return cleaned
    except Exception:
        return image


def crop_document_contour(image: np.ndarray) -> np.ndarray:
    """
    Detects the 4-corner boundary of a paper document in a photo
    and warps it to fit the frame, eliminating desk/camera borders.
    If no clear paper boundary exists (e.g. flatbed/ADF scan), returns original image unchanged.
    """
    if image is None or image.size == 0:
        return image
    try:
        h, w = image.shape[:2]
        gray_full = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY) if len(image.shape) == 3 else image

        # 1. Flatbed / ADF Scanner Guard:
        # Check the 4 corners (inset slightly past any 5px border shadow).
        # If the corners are already bright paper (mean >= 180), this is a direct full-page scan,
        # NOT a camera photo on a desk. Never crop internal form tables on a direct scan!
        pad = max(8, int(min(h, w) * 0.015))
        patch = max(20, int(min(h, w) * 0.04))
        if h > (pad + patch) * 2 and w > (pad + patch) * 2:
            c_tl = float(np.mean(gray_full[pad:pad + patch, pad:pad + patch]))
            c_tr = float(np.mean(gray_full[pad:pad + patch, w - pad - patch:w - pad]))
            c_bl = float(np.mean(gray_full[h - pad - patch:h - pad, pad:pad + patch]))
            c_br = float(np.mean(gray_full[h - pad - patch:h - pad, w - pad - patch:w - pad]))
            bright_corners = sum(1 for c in (c_tl, c_tr, c_bl, c_br) if c >= 180.0)
            if bright_corners >= 3:
                return image

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
            if area < 0.50 * img_area:
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

        # 2. Internal Table Rejection Guard:
        # Ensure the contour encompasses the full vertical span of the document (including header)
        # rather than snapping onto the Section I or Section III internal table borders.
        min_y_norm = float(min(tl[1], tr[1])) / float(max(1, h))
        max_y_norm = float(max(bl[1], br[1])) / float(max(1, h))
        if min_y_norm > 0.10 or (max_y_norm - min_y_norm) < 0.82:
            return image

        width_top = np.linalg.norm(tr - tl)
        width_bot = np.linalg.norm(br - bl)
        max_w = int(max(width_top, width_bot))

        height_left = np.linalg.norm(bl - tl)
        height_right = np.linalg.norm(br - tr)
        max_h = int(max(height_left, height_right))

        aspect = float(max_w) / float(max(1, max_h))
        if not (0.55 <= aspect <= 0.80) or max_w < 300 or max_h < 400:
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
    Detects the physical tilt angle of the document using a robust 2-stage estimator:
      1. Macro Stage: Canny edge + Hough segment voting detects macroscopic paper tilts
         between 0.8 and 12.0 degrees across form boundaries and table outlines.
      2. Micro Stage: Morphological horizontal rule kernel detects micro-tilts down to 0.10 degrees.
    Deadband: ignores micro-tilts below 0.10 degrees.
    Rotates straight, preserving exact document dimensions and aspect ratio.
    """
    try:
        if image is None or image.size == 0:
            return image

        (h, w) = image.shape[:2]
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY) if len(image.shape) == 3 else image

        # Normalize working resolution to ~1200px max dimension for consistent, ultra-fast (<20ms)
        # line detection across 150 DPI, 200 DPI, 300 DPI, and 600 DPI scans
        work_scale = 1200.0 / float(max(h, w))
        if work_scale < 1.0:
            work_gray = cv2.resize(gray, (int(w * work_scale), int(h * work_scale)), interpolation=cv2.INTER_AREA)
        else:
            work_gray = gray

        wh, ww = work_gray.shape[:2]

        # Ignore extreme 2% outer border so scanner frame lines never bias angle detection
        my = max(6, int(wh * 0.02))
        mx = max(6, int(ww * 0.02))
        inner_gray = work_gray[my:wh - my, mx:ww - mx]
        if inner_gray.size == 0:
            return image

        iw_h, iw_w = inner_gray.shape[:2]
        total_angle = 0.0

        # Stage 1: Macro Tilt Detection (Canny edges + Hough lines)
        edges = cv2.Canny(inner_gray, 50, 150, apertureSize=3)
        macro_lines = cv2.HoughLinesP(
            edges, 1, np.pi / 1800.0,
            threshold=45,
            minLineLength=max(35, iw_w // 10),
            maxLineGap=20
        )

        macro_angle = 0.0
        if macro_lines is not None and len(macro_lines) >= 4:
            m_angles = []
            m_weights = []
            for l in macro_lines:
                x1, y1, x2, y2 = l[0]
                if x2 < x1:
                    x1, y1, x2, y2 = x2, y2, x1, y1
                dx = float(x2 - x1)
                dy = float(y2 - y1)
                deg = float(np.degrees(np.arctan2(dy, dx)))
                if -12.0 <= deg <= 12.0:
                    m_angles.append(deg)
                    m_weights.append(np.hypot(dx, dy))
            if len(m_angles) >= 4:
                med = float(np.median(m_angles))
                inliers = [a for a in m_angles if abs(a - med) <= 1.5]
                inlier_w = [w_ for a, w_ in zip(m_angles, m_weights) if abs(a - med) <= 1.5]
                if inliers and sum(inlier_w) > 0:
                    cand_macro = float(np.average(inliers, weights=inlier_w))
                    if abs(cand_macro) >= 0.80 and abs(cand_macro) <= 12.0:
                        macro_angle = cand_macro

        curr_inner = inner_gray
        if abs(macro_angle) >= 0.80:
            total_angle += macro_angle
            M_pre = cv2.getRotationMatrix2D((iw_w // 2, iw_h // 2), macro_angle, 1.0)
            curr_inner = cv2.warpAffine(curr_inner, M_pre, (iw_w, iw_h), borderValue=255)

        # Stage 2: Micro Tilt Detection (Morphological horizontal rule kernel)
        thresh = cv2.adaptiveThreshold(
            curr_inner, 255,
            cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
            cv2.THRESH_BINARY_INV, 15, 8
        )
        h_kernel_len = max(25, iw_w // 18)
        h_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (h_kernel_len, 1))
        h_lines_mask = cv2.morphologyEx(thresh, cv2.MORPH_OPEN, h_kernel)

        micro_lines = cv2.HoughLinesP(
            h_lines_mask,
            rho=1,
            theta=np.pi / 1800.0,
            threshold=40,
            minLineLength=max(35, iw_w // 10),
            maxLineGap=15
        )

        micro_angle = 0.0
        if micro_lines is not None and len(micro_lines) >= 2:
            angles = []
            weights = []
            for line in micro_lines:
                x1, y1, x2, y2 = line[0]
                if x2 < x1:
                    x1, y1, x2, y2 = x2, y2, x1, y1
                dx = float(x2 - x1)
                dy = float(y2 - y1)
                length = np.hypot(dx, dy)
                if abs(dx) < 30 or length < 30:
                    continue
                deg = float(np.degrees(np.arctan2(dy, dx)))
                if -8.0 <= deg <= 8.0:
                    angles.append(deg)
                    weights.append(length)

            if len(angles) >= 2:
                angles_arr = np.array(angles, dtype=np.float64)
                med_angle = float(np.median(angles_arr))
                inliers = [a for a, l in zip(angles, weights) if abs(a - med_angle) <= 1.0]
                inlier_weights = [l for a, l in zip(angles, weights) if abs(a - med_angle) <= 1.0]
                if inliers and sum(inlier_weights) > 0:
                    micro_angle = float(np.average(inliers, weights=inlier_weights))
                else:
                    micro_angle = med_angle

        if abs(micro_angle) >= 0.10:
            total_angle += micro_angle

        # Deadband check: do not rotate if combined tilt is negligible (< 0.10 deg) or unrealistic (> 12 deg)
        if abs(total_angle) < 0.10 or abs(total_angle) > 12.0:
            return image

        print(f"[Deskew] Straightening document tilt of {total_angle:+.2f} deg (macro: {macro_angle:+.2f}, micro: {micro_angle:+.2f}).")
        center = (w // 2, h // 2)
        M = cv2.getRotationMatrix2D(center, total_angle, 1.0)
        border_val = (255, 255, 255) if len(image.shape) == 3 else 255
        rotated = cv2.warpAffine(
            image, M, (w, h),
            flags=cv2.INTER_CUBIC,
            borderMode=cv2.BORDER_CONSTANT,
            borderValue=border_val
        )
        return rotated
    except Exception as e:
        print(f"[Deskew Note]: {e}")
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

def resize_for_ocr(image: np.ndarray, max_dim: int = 1350) -> np.ndarray:
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

def preprocess_pipeline(image_bytes: bytes, return_full: bool = False):
    """
    Decodes raw image bytes:
    1. Straightens document tilt if skewed (preserving exact geometry and aspect ratio).
    2. Resizes proportionally (preserving exact aspect ratio so UI canvas coordinates match 1:1).
    3. Contrast-enhances with CLAHE and bilateral filtering.
    Returns: (color_optimized_image, enhanced_grayscale_image) or
             (color_optimized_image, enhanced_grayscale_image, deskewed_full) if return_full=True.
    """
    nparr = np.frombuffer(image_bytes, np.uint8)
    image = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    if image is None:
        return (None, None, None) if return_full else (None, None)

    # 0. Auto-detect paper boundaries and dewarp if photo contains desk/margins
    cropped_doc = crop_document_contour(image)

    # 0.5 Suppress thin scanner glass/ADF edge shadows
    margin_cleaned = suppress_scanner_margins(cropped_doc)

    # 1. Straighten document tilt if skewed (preserves exact dimensions)
    deskewed = deskew(margin_cleaned)
        
    # 2. Resize proportionally (preserves exact aspect ratio and normalized coords)
    optimized = resize_for_ocr(deskewed, max_dim=1350)
    
    # 3. Contrast enhancement for OCR reading
    enhanced = enhance_contrast(optimized)
    
    if return_full:
        return optimized, enhanced, deskewed
    return optimized, enhanced

def detect_form_grid_lines(image_gray: np.ndarray) -> tuple:
    """
    Extracts binary masks of physical horizontal and vertical table lines printed on the form.
    Returns: (h_lines_mask, v_lines_mask)
    """
    if image_gray is None or image_gray.size == 0:
        return None, None
    try:
        thresh = cv2.adaptiveThreshold(image_gray, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY_INV, 15, 8)
        h_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (40, 1))
        v_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (1, 40))
        h_lines = cv2.morphologyEx(thresh, cv2.MORPH_OPEN, h_kernel)
        v_lines = cv2.morphologyEx(thresh, cv2.MORPH_OPEN, v_kernel)
        return h_lines, v_lines
    except Exception:
        return None, None

def snap_box_to_grid(bbox: list, h_lines_mask: np.ndarray, v_lines_mask: np.ndarray, img_w: int, img_h: int, max_search_px: int = 10, has_top_label: bool = False) -> list:
    """
    Snaps a bounding box [bx, by, bw, bh] directly within physical printed cell boundaries,
    ensuring that query crops never cross adjacent rows or table headers.
    If has_top_label=True (e.g. Address or Emergency fields), offsets the top down to exclude
    the printed header banner and focus strictly on the handwriting line.
    """
    if h_lines_mask is None or img_w <= 0 or img_h <= 0:
        return bbox

    max_search_px = min(max_search_px, 10)
    bx, by, bw, bh = bbox
    px1 = int(bx * img_w)
    py1 = int(by * img_h)
    px2 = int((bx + bw) * img_w)
    py2 = int((by + bh) * img_h)

    # Search for top horizontal boundary across the middle 60% of the box width
    mid_x1 = px1 + int((px2 - px1) * 0.20)
    mid_x2 = px2 - int((px2 - px1) * 0.20)

    if mid_x2 > mid_x1:
        sub_h = h_lines_mask[:, mid_x1:mid_x2]

        # Top line search
        t_start = max(0, py1 - max_search_px)
        t_end = min(img_h, py1 + max_search_px)
        if t_end > t_start:
            t_prof = np.mean(sub_h[t_start:t_end, :], axis=1)
            if len(t_prof) > 0 and np.max(t_prof) > 40:
                best_t = int(np.argmax(t_prof))
                py1 = t_start + best_t + 2  # 2px clearance inside cell

        # Bottom line search
        b_start = max(0, py2 - max_search_px)
        b_end = min(img_h, py2 + max_search_px)
        if b_end > b_start:
            b_prof = np.mean(sub_h[b_start:b_end, :], axis=1)
            if len(b_prof) > 0 and np.max(b_prof) > 40:
                best_b = int(np.argmax(b_prof))
                py2 = b_start + best_b - 2  # 2px clearance inside cell

    # If the cell has a printed header inside it (like Address or Emergency Contact),
    # offset py1 downwards by 30% of cell height so the crop starts below the printed header
    if has_top_label and py2 > py1 + 10:
        cell_h = py2 - py1
        py1 = py1 + int(cell_h * 0.30)

    if py2 <= py1 + 4:
        return bbox

    return [
        round(float(px1) / float(img_w), 4),
        round(float(py1) / float(img_h), 4),
        round(float(px2 - px1) / float(img_w), 4),
        round(float(py2 - py1) / float(img_h), 4)
    ]

def image_to_base64(image: np.ndarray, format: str = ".jpg") -> str:
    """
    Encodes an OpenCV image to a Base64 string for WPF UI preview.
    """
    success, buffer = cv2.imencode(format, image)
    if not success:
        return ""
    return base64.b64encode(buffer).decode("utf-8")
