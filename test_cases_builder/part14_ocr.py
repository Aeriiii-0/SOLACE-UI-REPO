# -*- coding: utf-8 -*-

def get_ocr_cases():
    return [
        (
            "OCR / Scanner Hardware Settings Configuration",
            "Configure scanner driver, DPI resolution, and color scanning mode in scanner settings modal. (Usability)",
            "1. In OcrScanDialog, click 'Scanner Settings' button.\n2. Select connected scanner from device dropdown.\n3. Set Resolution to 300 DPI, Mode to 'Color', Paper to '8.5 * 13'.\n4. Click 'Save Settings'; verify settings persist.",
            "Select Scanner: 'EPSON Perfection V39', DPI: '300', Mode: 'Color', Paper: '8.5 * 13', Click 'Save Settings'",
            "Usability",
            "Verified: ScannerSettingsDialog saves configuration parameters to user settings."
        ),
        (
            "OCR / WIA Hardware Acquisition Flow",
            "Trigger document scan directly from connected hardware scanner via Windows Image Acquisition (WIA). (Functional Suitability)",
            "1. Place CSWDO Solo Parent application form on physical scanner glass.\n2. In OcrScanDialog, click 'Scan from Hardware' button.\n3. Observe live acquisition progress bar and status updates.\n4. Verify scanned image loads into interactive document viewport.",
            "Click 'Scan from Hardware' button",
            "Functional Suitability",
            "Verified: WiaScannerService acquires image and loads into OcrDocumentViewportComponent."
        ),
        (
            "OCR / Disconnected Hardware Scanner Handling",
            "Attempt hardware scan when no scanner device is connected or powered on. (Reliability)",
            "1. Disconnect physical scanner USB / power cable.\n2. Click 'Scan from Hardware' button in OcrScanDialog.\n3. Observe system timeout and error dialog reaction.\n4. Verify friendly notice is displayed without application crash.",
            "Click 'Scan from Hardware' with no scanner connected",
            "Reliability",
            "Verified: WiaScannerService catches COMException and displays user-friendly hardware notice."
        ),
        (
            "OCR / Hardware Scan Acquisition Cancellation",
            "Cancel an active hardware scanning operation before acquisition completes. (Reliability)",
            "1. Initiate hardware scan via 'Scan from Hardware'.\n2. Cancel operation from Windows WIA scanning progress prompt.\n3. Verify application returns to idle state without error.",
            "Click 'Cancel' on Windows scanner acquisition prompt",
            "Reliability",
            "Verified: Handles user cancellation gracefully."
        ),
        (
            "OCR / Backend Service Health Indicator (Green Badge)",
            "Verify visual status badge when local Python PP-OCRv4 background service is active. (Usability)",
            "1. Open OcrScanDialog.\n2. Observe ServerStatusBadge at top header.\n3. Verify badge displays green indicator and tooltip: 'OCR Service Active'.",
            "Inspect ServerStatusBadge in OcrScanDialog",
            "Usability",
            "Verified: Health check endpoint confirms service is responsive."
        ),
        (
            "OCR / Engine Warmup & Auto-Start",
            "Automatically initialize and warm up OCR engine subprocess if found stopped upon dialog opening. (Reliability)",
            "1. Stop running Python OCR service process.\n2. Open OcrScanDialog.\n3. Observe badge transitioning from orange ('Warming Up Engine...') to green once healthy.",
            "Open OCR dialog while service process is stopped",
            "Reliability",
            "Verified: OcrService launches local Python subprocess and polls health endpoint."
        ),
        (
            "OCR / Manual Health Retry on Badge Click",
            "Click directly on red ServerStatusBadge to manually trigger health re-check and reconnect. (Maintainability)",
            "1. Simulate OCR service failure (badge turns red).\n2. Restart backend OCR service.\n3. Click directly on red ServerStatusBadge.\n4. Verify health check runs and badge turns green.",
            "Left-click on red ServerStatusBadge",
            "Maintainability",
            "Verified: Badge click re-invokes CheckHealthAsync()."
        ),
        (
            "OCR / Upload Scanned Image File",
            "Load an existing scanned Solo Parent application form from local filesystem storage. (Functional Suitability)",
            "1. In OcrScanDialog, click 'Upload File / Select Image' button.\n2. In OpenFileDialog, choose valid form scan (PNG/JPG/BMP).\n3. Confirm selection.\n4. Verify image renders in viewport and extraction pipeline commences.",
            "Select sample scan 'cswdo_form_sample.jpg', Click 'Open'",
            "Functional Suitability",
            "Verified: Loads image bitmap into viewport and initiates preprocessing."
        ),
        (
            "OCR / Invalid or Corrupted Image File Handling",
            "Attempt to load an unsupported or corrupt file type into OCR viewport. (Reliability)",
            "1. Click 'Upload File / Select Image'.\n2. Select non-image or damaged file header.\n3. Verify validation error alert is displayed and file is rejected safely.",
            "Select 'corrupted_file.txt' or damaged image file",
            "Reliability",
            "Verified: Catches bitmap decode exception and shows ActionConfirmDialog.Error."
        ),
        (
            "OCR / Auto-Deskew & Contrast Enhancement Pipeline",
            "Process slightly skewed or low-contrast document scan through PaddleOCR deskew and enhancement pipeline. (Performance Efficiency)",
            "1. Upload a tilted (5–10 degree skew) scanned form.\n2. Await Step 1 preprocessing stage completion.\n3. Verify document viewport re-renders corrected preview image aligned with template coordinates.",
            "Upload tilted scan 'tilted_form_sample.png'",
            "Performance Efficiency",
            "Verified: Executes Hough transform / text angle deskew in preprocessing."
        ),
        (
            "OCR / Interactive Viewport Zoom and Pan Controls",
            "Verify interactive viewing tools including Zoom In, Zoom Out, Zoom Fit, and mouse pan drag. (Usability)",
            "1. Load scanned form into viewport.\n2. Click 'Zoom In' (+) and 'Zoom Out' (-) buttons.\n3. Click 'Fit to Screen' (100%).\n4. Click and drag with mouse inside viewport to pan around image.",
            "Click Zoom In (+), Zoom Out (-), Zoom Fit, Left-click drag on DocScrollViewer",
            "Usability",
            "Verified: OcrDocumentViewportComponent handles zoom transforms and mouse drag pan."
        ),
        (
            "OCR / HITL Live Sync Field Focus & Bounding Box Highlight",
            "Verify focusing an input field automatically pans, zooms, and highlights its corresponding form zone. (Usability)",
            "1. In Step 1 review form, click into 'First Name' text box.\n2. Observe document viewport animation and red bounding box highlighting the First Name zone on physical form.\n3. Click into 'Date of Birth' date picker; observe viewport pan and highlight transition.",
            "Focus TxtFirstName, then focus DpBirthdate in OCR review form",
            "Usability",
            "Verified: Field focus triggers FocusFieldRequested event and centers bounding box on canvas."
        ),
        (
            "OCR / Template Customization (Adjust Bounding Boxes)",
            "Open Template Configuration dialog to adjust and re-align bounding boxes for shifted form printouts. (Maintainability)",
            "1. In OCR dialog, click 'Adjust Template' button.\n2. In OcrTemplateConfigWindow, drag and resize bounding boxes for specific fields.\n3. Click 'Save Template'.\n4. Verify OCR dialog re-processes active scan with updated coordinates.",
            "Click 'Adjust Template', adjust Last Name zone, click 'Save Template'",
            "Maintainability",
            "Verified: OcrTemplateConfigWindow saves updated coordinates to template.json."
        ),
        (
            "OCR / Demographics Text Extraction (Step 1)",
            "Verify automatic extraction and normalization of applicant demographics from clear form scan. (Functional Suitability)",
            "1. Ingest clean CSWDO Solo Parent Form scan.\n2. Verify Last Name, First Name, Middle Name, PhilSys ID, and Barangay populate.\n3. Check capitalization cleaning and phone number normalization.",
            "Scan 'cswdo_form_clean.jpg'",
            "Functional Suitability",
            "Verified: OcrPrimaryInfoComponent populates extracted demographic fields."
        ),
        (
            "OCR / Low-Confidence Scoring Badge Flagging",
            "Highlight ambiguous or low-confidence OCR text readings with warning badges for mandatory human review. (Functional Suitability)",
            "1. Ingest document with blurred or handwritten ambiguous PhilSys number.\n2. Observe confidence badge next to 'PhilSys Number' field.\n3. Verify badge color indicates low confidence (<60%) in amber/orange.",
            "Form scan with smudged PhilSys ID box",
            "Functional Suitability",
            "Verified: Flags fields where Confidence < 0.60 with review warning badge."
        ),
        (
            "OCR / Human-in-the-Loop Manual Field Correction",
            "Enable operator to override or correct extracted OCR text directly before proceeding to next step. (Usability)",
            "1. Ingest form with misread character in 'Last Name' (e.g. 'D0E' instead of 'DOE').\n2. Focus TxtLastName and edit text to 'DOE'.\n3. Click 'Next Step (Circumstances & Family)'.\n4. Verify corrected text persists.",
            "Keyboard input: replace 'D0E' with 'DOE', Click 'Next'",
            "Usability",
            "Verified: Operator edits override OCR predictions in model."
        ),
        (
            "OCR / Step 1 Validation Gate",
            "Prevent advancement to Step 2 when required demographic fields are missing or empty. (Functional Suitability)",
            "1. Clear 'Last Name' or 'First Name' text box.\n2. Click 'Next' button or click 'Step 2' header directly.\n3. Verify validation feedback and step lock prevent progression.",
            "Blank TxtLastName, Click 'Next'",
            "Functional Suitability",
            "Verified: Validates mandatory demographic fields before advancing to Step 2."
        ),
        (
            "OCR / Circumstance Encircled Code Recognition (Step 2)",
            "Automatically detect pen-encircled Solo Parent circumstances (Section II codes A1–F) and map to checkboxes. (Functional Suitability)",
            "1. Advance to Step 2 for form with encircled circumstance code 'A2' (Death of spouse).\n2. Verify 'A2' checkbox is automatically checked.\n3. Verify cause and date subfields populate if extracted.",
            "Form scan with encircled 'A2' in Section II",
            "Functional Suitability",
            "Verified: Circumstance detector maps detected circle bounding box to statutory code."
        ),
        (
            "OCR / Family Composition Table Extraction (Step 2)",
            "Extract multi-row dependent children table from Section III including Name, Relationship, Age, and Status. (Functional Suitability)",
            "1. In Step 2, inspect 'Family Composition / Dependents' items control.\n2. Verify each child listed on physical form appears as an editable row.\n3. Verify Name, Birthdate, Relationship, and Educational Attainment columns align.",
            "Form scan with 3 dependent children in table",
            "Functional Suitability",
            "Verified: OcrFamilyMembersComponent parses multi-row tabular dependent structures."
        ),
        (
            "OCR / Dependents Table Dynamic Row Manipulation",
            "Add new dependent child row, edit extracted child fields, and delete erroneous rows in Step 2. (Usability)",
            "1. In Family Dependents section, click '+ Add Dependent'.\n2. Enter additional child details.\n3. Click 'Delete' icon on an empty/extraneous row.\n4. Verify row collection updates cleanly.",
            "Click '+ Add Dependent', Enter Child Name, Click 'Delete Row' icon",
            "Usability",
            "Verified: OcrFamilyMembersComponent supports dynamic row addition and deletion."
        ),
        (
            "OCR / End-to-End Save and Registry Persistence",
            "Save verified OCR document data, persisting new solo parent and family members into the database registry. (Functional Suitability)",
            "1. Complete Step 1 and Step 2 verification in OcrScanDialog.\n2. Click 'Save & Ingest Record' button.\n3. Verify dialog closes and Solo Parent Records table refreshes.\n4. Verify newly scanned applicant appears at top of records table with all extracted dependents.",
            "Click 'Save & Ingest Record' button",
            "Functional Suitability",
            "Verified: Persists applicant and family composition to database and logs audit event."
        ),
        (
            "OCR / Unsaved Changes Discard Warning on Close",
            "Prompt confirmation dialog when closing OCR ingestion window with scanned or partially entered data. (Reliability)",
            "1. Ingest form or input data into fields.\n2. Click window close 'X' button or press Alt+F4.\n3. In 'Unsaved Changes' confirmation dialog, click 'No' (Cancel).\n4. Repeat and click 'Yes' (Discard).\n5. Verify dialog closes cleanly on discard.",
            "Click window close 'X', Click 'No', then Click 'Yes'",
            "Reliability",
            "Verified: Unsaved changes prompt protects acquired scan and review data."
        ),
        (
            "OCR / Multi-Page Document Ingestion",
            "Verify navigating between Page 1 (Front) and Page 2 (Back) for two-sided application forms. (Functional Suitability)",
            "1. Ingest two-page scanned application form.\n2. Click 'Page 2' thumbnail or tab.\n3. Verify viewport renders reverse side of form and Section III Family Composition extracts.",
            "Switch between Page 1 and Page 2 in OCR dialog",
            "Functional Suitability",
            "Verified: Manages multi-page document scans."
        ),
        (
            "OCR / Low-Resolution Warning (< 200 DPI)",
            "Verify notification warning when uploaded image has resolution below 200 DPI. (Usability)",
            "1. Upload a low-resolution thumbnail scan (72 DPI).\n2. Verify warning banner appears advising that scan quality may impair character extraction accuracy.",
            "Upload 72 DPI image",
            "Usability",
            "Verified: Image DPI inspector alerts user when image resolution is subpar."
        ),
        (
            "OCR / Discard Active Scan Action",
            "Verify clicking 'Discard Scan' clears the viewport and returns to file selection state. (Usability)",
            "1. Load scanned document.\n2. Click 'Discard Scan' button.\n3. Confirm discard prompt.\n4. Verify viewport clears and upload/hardware scan buttons re-appear.",
            "Click 'Discard Scan' button",
            "Usability",
            "Verified: Resets viewport state and unloads document bitmap."
        ),
        (
            "OCR / Text Capitalization Auto-Correction",
            "Verify extracted lowercase text in names and addresses is normalized to uppercase. (Functional Suitability)",
            "1. Ingest form where text was recognized in lowercase.\n2. Verify extracted text in name and address boxes formats automatically to uppercase.",
            "Scan form with lowercase text",
            "Functional Suitability",
            "Verified: Normalization pipeline enforces uppercase formatting for registry standards."
        ),
        (
            "OCR / Contact Number Auto-Dash Normalization",
            "Verify extracted 11-digit contact number is formatted automatically with standard dashes. (Functional Suitability)",
            "1. Ingest form with contact number '09171234567'.\n2. Verify text in Contact box formats to '0917-123-4567'.",
            "Scan form with continuous phone number digits",
            "Functional Suitability",
            "Verified: Normalizes phone number format upon extraction."
        ),
        (
            "OCR / PhilSys 16-Digit Formatting on Extraction",
            "Verify extracted PhilSys number formats into standard 4-4-4-4 dashed grouping. (Functional Suitability)",
            "1. Ingest form with PhilSys card number.\n2. Verify extracted ID renders as 'XXXX-XXXX-XXXX-XXXX'.",
            "Scan form with PhilSys ID",
            "Functional Suitability",
            "Verified: Formats extracted PhilSys numbers."
        ),
        (
            "OCR / Server Process Recovery after Crash",
            "Verify automatic process resurrection if Python PP-OCRv4 subprocess terminates unexpectedly. (Reliability)",
            "1. Force kill python ocr_server.exe in Task Manager while SOLACE is running.\n2. Click 'Upload File / Select Image' in OcrScanDialog.\n3. Verify SOLACE detects stopped subprocess and resurrects OCR engine automatically.",
            "Kill OCR service process, Trigger new scan",
            "Reliability",
            "Verified: Process supervisor detects dead process and relaunches engine."
        ),
        (
            "OCR / Audit Logging on Document Ingestion",
            "Verify saving scanned document logs OCR_DOCUMENT_INGEST audit record with citizen name and staff ID. (Security)",
            "1. Complete OCR document scan and click 'Save & Ingest Record'.\n2. Open ViewAllLogsOverlay.\n3. Verify top log records 'OCR_DOCUMENT_INGEST' event with metadata.",
            "Ingest document via OCR -> Inspect Audit Logs",
            "Security",
            "Verified: Server records audit event on automated OCR document intake."
        )
    ]
