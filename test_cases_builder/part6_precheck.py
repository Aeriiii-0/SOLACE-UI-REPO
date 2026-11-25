# -*- coding: utf-8 -*-

def get_precheck_cases():
    return [
        (
            "Entry Method / Branch Selection (Modal Trigger)",
            "Verify clicking '+ Add New Record' opens EntryMethodDialog with 'Scan Document' and 'Manual Entry' options. (Usability)",
            "1. On Solo Parent Records page, click '+ Add New Record' button.\n2. Verify EntryMethodDialog opens with choices: 'Scan Document' and 'Manual Entry'.\n3. Verify descriptive icons and guidance text are displayed.",
            "Click '+ Add New Record' in toolbar",
            "Usability",
            "Verified: Opens EntryMethodDialog providing user choice between automated OCR and manual intake."
        ),
        (
            "Entry Method / Branch Selection (Scan Document)",
            "Verify selecting 'Scan Document' closes EntryMethodDialog and opens OcrScanDialog. (Functional Suitability)",
            "1. On EntryMethodDialog, click 'Scan Document' card.\n2. Verify EntryMethodDialog closes and OcrScanDialog opens centered on the window.",
            "Click 'Scan Document' card",
            "Functional Suitability",
            "Verified: EntryMethodDialog returns OCR selection and opens OcrScanDialog."
        ),
        (
            "Entry Method / Branch Selection (Manual Entry)",
            "Verify selecting 'Manual Entry' closes EntryMethodDialog and opens PreCheckDialog identity gate. (Functional Suitability)",
            "1. On EntryMethodDialog, click 'Manual Entry' card.\n2. Verify EntryMethodDialog closes and PreCheckDialog opens for identity verification.",
            "Click 'Manual Entry' card",
            "Functional Suitability",
            "Verified: Launches PreCheckDialog as Step 1 identity verification gate."
        ),
        (
            "Entry Method / Dismiss Dialog Without Action",
            "Verify dismissing EntryMethodDialog via close icon or Escape key cancels record creation. (Usability)",
            "1. In EntryMethodDialog, click '✕' close button or press keyboard [Esc].\n2. Verify dialog closes cleanly and application returns to records table without error.",
            "Click '✕' or press [Esc]",
            "Usability",
            "Verified: Sets DialogResult = false and returns focus to records grid."
        ),
        (
            "Pre-Check / First Name Required Validation",
            "Verify inline error when First Name is left blank in PreCheckDialog. (Negative / Validation)",
            "1. In PreCheckDialog, leave First Name empty.\n2. Fill Last Name and Birthdate.\n3. Click 'Continue'.\n4. Verify ErrFirstName displays 'Required.' and progression is blocked.",
            "First Name: \"\" (Empty), Click 'Continue'",
            "Functional Suitability",
            "Verified: PreCheckDialog enforces string.IsNullOrWhiteSpace(firstName) check."
        ),
        (
            "Pre-Check / First Name Numeric Keystroke Blocking",
            "Verify numbers typed into First Name in PreCheckDialog are blocked in real time. (Negative / Input Filtering)",
            "1. Focus First Name field in PreCheckDialog.\n2. Attempt to type 'Juan123'.\n3. Verify '123' is suppressed and only 'Juan' is entered.",
            "Keystroke 'Juan123' in TxtFirstName",
            "Reliability",
            "Verified: NameRegex ^[a-zA-ZñÑ\\s\\.\\-']+$ blocks numeric characters."
        ),
        (
            "Pre-Check / First Name Auto Uppercase Formatting",
            "Verify First Name characters are automatically converted to uppercase upon typing. (Usability)",
            "1. In First Name field, type 'juan carlos'.\n2. Verify text automatically converts to 'JUAN CARLOS'.",
            "Type 'juan carlos' into TxtFirstName",
            "Usability",
            "Verified: CharacterCasing = CharacterCasing.Upper or ToUpper formatting handler."
        ),
        (
            "Pre-Check / Last Name Required Validation",
            "Verify inline error when Last Name is left blank in PreCheckDialog. (Negative / Validation)",
            "1. In PreCheckDialog, leave Last Name empty.\n2. Fill First Name and Birthdate.\n3. Click 'Continue'.\n4. Verify ErrLastName displays 'Required.'",
            "Last Name: \"\" (Empty), Click 'Continue'",
            "Functional Suitability",
            "Verified: Validates string.IsNullOrWhiteSpace(lastName) and displays required error."
        ),
        (
            "Pre-Check / Last Name Numeric Keystroke Blocking",
            "Verify numbers and special symbols typed into Last Name in PreCheckDialog are blocked. (Negative / Input Filtering)",
            "1. In Last Name field, attempt to type 'DelaCruz!@#456'.\n2. Verify symbols and digits are rejected and only 'DELACRUZ' is entered.",
            "Keystroke 'DelaCruz!@#456' in TxtLastName",
            "Reliability",
            "Verified: Disallowed keystrokes are suppressed by NameRegex."
        ),
        (
            "Pre-Check / Last Name Auto Uppercase Formatting",
            "Verify Last Name automatically capitalizes to uppercase characters. (Usability)",
            "1. In Last Name field, type 'dela cruz'.\n2. Verify text automatically formats to 'DELA CRUZ'.",
            "Type 'dela cruz' into TxtLastName",
            "Usability",
            "Verified: Auto uppercase formatting applied to surname."
        ),
        (
            "Pre-Check / Middle Name Handling",
            "Verify Middle Name field is optional and auto-capitalizes when entered. (Functional Suitability)",
            "1. In PreCheckDialog, leave Middle Name blank and click 'Continue'; verify validation does not flag middle name as required.\n2. Enter 'reyes' in Middle Name; verify text formats to 'REYES'.",
            "Middle Name: \"\" (Empty), then 'reyes'",
            "Functional Suitability",
            "Verified: Middle name is non-mandatory and formats to uppercase when provided."
        ),
        (
            "Pre-Check / Date of Birth Required Validation",
            "Verify inline error when Date of Birth is left blank in PreCheckDialog. (Negative / Validation)",
            "1. In PreCheckDialog, leave Date of Birth unselected.\n2. Click 'Continue'.\n3. Verify ErrBirthdate displays 'Required.' and border turns red.",
            "Date of Birth: null (Unselected), Click 'Continue'",
            "Functional Suitability",
            "Verified: Validates DpBirthdate.SelectedDate.HasValue."
        ),
        (
            "Pre-Check / Date of Birth Future Date Rejection",
            "Verify selecting a future date in Date of Birth is rejected with clear error. (Negative / Boundary)",
            "1. In Date of Birth picker, select tomorrow's date.\n2. Click 'Continue'.\n3. Verify ErrBirthdate displays 'Cannot be in the future.'",
            "Birthdate: DateTime.Today + 1 day, Click 'Continue'",
            "Reliability",
            "Verified: Validates SelectedDate > DateTime.Today."
        ),
        (
            "Pre-Check / Applicant Minimum Age Boundary (< 15 Years Old)",
            "Verify applicants younger than 15 years old are blocked with age requirement error. (Negative / Boundary)",
            "1. In Date of Birth, select a date indicating applicant is 14 years old (e.g., DateTime.Today - 14 years).\n2. Click 'Continue'.\n3. Verify ErrBirthdate displays 'Must be at least 15 years old.' and progression is blocked.",
            "Birthdate: DateTime.Today - 14 years, Click 'Continue'",
            "Reliability",
            "Verified: Minimum age check enforces statutory solo parent lower boundary (age >= 15)."
        ),
        (
            "Pre-Check / Real-Time Age Auto-Calculation",
            "Verify selecting a valid Date of Birth calculates applicant age automatically. (Functional Suitability)",
            "1. In Date of Birth picker, select a date exactly 28 years ago.\n2. Verify auto-calculated age label immediately displays '28 yrs old'.\n3. Change date to 35 years ago; verify label updates to '35 yrs old'.",
            "Select birthdate 28 years ago, then 35 years ago",
            "Functional Suitability",
            "Verified: DatePicker SelectedDateChanged calculates age and updates TxtAge."
        ),
        (
            "Pre-Check / PhilSys Number Optional Format",
            "Verify leaving PhilSys Number blank allows applicant to proceed if other fields are valid. (Functional Suitability)",
            "1. Leave PhilSys Number blank.\n2. Fill valid names and birthdate.\n3. Click 'Continue'.\n4. Verify pre-check proceeds without requiring PhilSys ID.",
            "PhilSys: \"\" (Empty), Valid names & birthdate, Click 'Continue'",
            "Functional Suitability",
            "Verified: PhilSys is optional under RA 11861 guidelines."
        ),
        (
            "Pre-Check / PhilSys Number Incomplete Digits (< 16 Digits)",
            "Verify inline error when PhilSys Number has fewer than 16 digits. (Negative / Validation)",
            "1. In PhilSys Number field, enter 12 digits '1234-5678-9012'.\n2. Click 'Continue'.\n3. Verify ErrPhilSys displays 'PhilSys must be 16 digits (XXXX-XXXX-XXXX-XXXX).' and border turns red.",
            "PhilSys: '1234-5678-9012' (12 digits), Click 'Continue'",
            "Functional Suitability",
            "Verified: Strips non-digits; checks rawDigits.Length != 16."
        ),
        (
            "Pre-Check / PhilSys Number Auto-Dash Formatting",
            "Verify entering 16 digits into PhilSys automatically inserts dashes (XXXX-XXXX-XXXX-XXXX). (Usability)",
            "1. In PhilSys field, type '1234567890123456' continuously without dashes.\n2. Verify field formats automatically to '1234-5678-9012-3456'.\n3. Press backspace; verify backspacing across dashes behaves smoothly.",
            "Input 16 digits: '1234567890123456'",
            "Usability",
            "Verified: TextChanged handler applies 4-4-4-4 dashed grouping."
        ),
        (
            "Pre-Check / PhilSys Non-Numeric Keystroke Blocking",
            "Verify typing letters or punctuation into PhilSys field is blocked in real time. (Negative / Input Filtering)",
            "1. Focus PhilSys field.\n2. Attempt to type letters 'ABCD-EFGH'.\n3. Verify keystrokes are suppressed and field remains empty.",
            "Keystroke 'ABCD' in TxtPhilSys",
            "Reliability",
            "Verified: NumberOnlyRegex blocks non-digit keystrokes."
        ),
        (
            "Pre-Check / Duplicate Detection by PhilSys Number",
            "Verify duplicate check flags and blocks registration when PhilSys matches an existing record. (Duplicate Pre-Check)",
            "1. Enter valid names and birthdate.\n2. In PhilSys Number, enter an ID already registered to an existing solo parent in the database.\n3. Click 'Continue'.\n4. Verify pre-check API identifies duplicate and displays warning banner: 'PhilSys ID is already registered to <Name>.'",
            "PhilSys: existing registered PhilSys ID, Click 'Continue'",
            "Functional Suitability",
            "Verified: Queries API duplicate pre-check; flags duplicate PhilSys and blocks advance."
        ),
        (
            "Pre-Check / Duplicate Detection by Full Name and Birthdate",
            "Verify duplicate check flags matching First Name, Last Name, and Birthdate. (Duplicate Pre-Check)",
            "1. In PreCheckDialog, enter First Name, Last Name, and Date of Birth matching an existing active solo parent.\n2. Leave PhilSys blank.\n3. Click 'Continue'.\n4. Verify warning banner appears: 'A solo parent with the same name and birthdate is already registered.'",
            "Matching First Name + Last Name + Birthdate, Click 'Continue'",
            "Functional Suitability",
            "Verified: Backend duplicate check matches canonical FullName + DateOfBirth."
        ),
        (
            "Pre-Check / View Existing Record Action from Duplicate Banner",
            "Verify clicking 'View Existing Record' on duplicate warning card opens RecordViewDialog. (Usability)",
            "1. Trigger a duplicate record warning in PreCheckDialog.\n2. Verify 'View Existing Record' button becomes visible on the warning card.\n3. Click 'View Existing Record'.\n4. Verify PreCheckDialog closes and RecordViewDialog opens showing the existing solo parent profile.",
            "Click 'View Existing Record' button on duplicate warning card",
            "Usability",
            "Verified: PreCheckDialog sets ExistingRecordToView and opens RecordViewDialog."
        ),
        (
            "Pre-Check / Network Failure Fault Tolerance",
            "Verify friendly error notice when network disconnects during duplicate verification. (Reliability)",
            "1. Fill valid pre-check details.\n2. Disconnect network or stop API service.\n3. Click 'Continue'.\n4. Verify application does not crash and displays friendly 'No Internet Connection' warning notice.",
            "Simulated network failure, Click 'Continue'",
            "Reliability",
            "Verified: Catches network error and informs user without corrupting state."
        ),
        (
            "Pre-Check / Re-entrancy Multi-Click Prevention",
            "Verify clicking 'Continue' rapidly disables button and prevents concurrent search requests. (Reliability)",
            "1. Fill valid identity fields.\n2. Rapidly double-click 'Continue' button.\n3. Verify _searching flag prevents duplicate API requests and loading indicator displays.",
            "Rapid double-click on 'BtnContinue'",
            "Reliability",
            "Verified: Guard flag _searching prevents duplicate concurrent queries."
        ),
        (
            "Pre-Check / Real-Time Error Clearance on User Correction",
            "Verify error borders and error text collapse immediately when user begins correcting the field. (Usability)",
            "1. Trigger validation errors on Last Name and Birthdate.\n2. Type into Last Name box; verify ErrLastName collapses.\n3. Pick valid date in DatePicker; verify ErrBirthdate collapses.",
            "Correct fields after error",
            "Usability",
            "Verified: TextChanged and SelectedDateChanged handlers clear errors."
        ),
        (
            "Pre-Check / Advance Clean Applicant to Full Record Form",
            "Verify unique applicant passes pre-check and opens RecordDialog with pre-populated identity data. (Successful)",
            "1. Enter unique First Name ('Carlos'), Last Name ('Mendoza'), Birthdate (30 years ago), and unique PhilSys.\n2. Click 'Continue'.\n3. Verify PreCheckDialog closes with ProceedToFullForm = true.\n4. Verify RecordDialog opens at Step 1 with names, birthdate, and PhilSys pre-populated.",
            "Valid unique identity details, Click 'Continue'",
            "Functional Suitability",
            "Verified: PreCheckResult data binds to RecordDialog fields, advancing to Step 1 seamlessly."
        ),
        (
            "Pre-Check / Cancel Pre-Check Dialog",
            "Verify clicking 'Cancel' in PreCheckDialog aborts registration cleanly. (Usability)",
            "1. In PreCheckDialog, enter partial identity data.\n2. Click 'Cancel' button (or '✕').\n3. Verify dialog closes, no record is created, and records table remains displayed.",
            "Click 'Cancel' in PreCheckDialog",
            "Usability",
            "Verified: Sets ProceedToFullForm = false and closes window."
        ),
        (
            "Pre-Check / Date Picker Watermark Suppression",
            "Verify DatePicker does not display confusing default watermark text when empty. (Usability)",
            "1. Open PreCheckDialog and inspect Date of Birth picker.\n2. Verify placeholder displays clean prompt 'Select birthdate' without garbled watermarks.",
            "Inspect DpBirthdate styling in PreCheckDialog",
            "Usability",
            "Verified: SuppressDatePickerWatermarks and custom FormDate style provide clean placeholder."
        ),
        (
            "Pre-Check / Keyboard Enter Key Navigation",
            "Verify pressing Enter key on PhilSys field triggers 'Continue' verification. (Usability)",
            "1. Fill required identity fields in PreCheckDialog.\n2. Focus PhilSys field and press keyboard [Enter].\n3. Verify application triggers duplicate check without requiring mouse click.",
            "Focus TxtPhilSys, Press [Enter]",
            "Usability",
            "Verified: TxtPhilSys_KeyDown triggers verification on Key.Enter."
        ),
        (
            "Pre-Check / Middle Name Numeric Keystroke Blocking",
            "Verify numbers typed into Middle Name are blocked in real time. (Negative / Input Filtering)",
            "1. In Middle Name field, attempt to type '123'.\n2. Verify keystrokes are blocked and text box remains empty.",
            "Keystroke '123' in TxtMiddleName",
            "Reliability",
            "Verified: NameRegex suppresses numbers in middle name input."
        ),
        (
            "Pre-Check / Special Character Blocking in Names",
            "Verify symbols like '@', '#', '$', '%', '&' are blocked in all name fields. (Negative / Input Filtering)",
            "1. In First Name, Last Name, and Middle Name fields, attempt typing '@#$%^'.\n2. Verify all symbols are rejected across all three fields.",
            "Keystroke '@#$%^' across name fields",
            "Reliability",
            "Verified: PreviewTextInput uniformly enforces NameRegex on all name inputs."
        ),
        (
            "Pre-Check / Hyphenated Surnames Acceptance",
            "Verify Last Name accepts hyphenated surnames (e.g. 'Cruz-Alvarez'). (Functional Suitability)",
            "1. In Last Name field, enter 'Cruz-Alvarez'.\n2. Verify hyphen is accepted and text capitalizes to 'CRUZ-ALVAREZ'.",
            "Last Name: 'Cruz-Alvarez'",
            "Functional Suitability",
            "Verified: Hyphen character is included in NameRegex pattern."
        ),
        (
            "Pre-Check / Filipino Character 'Ñ' Acceptance",
            "Verify names with Filipino letter 'Ñ' (e.g. 'Peñaflor') are accepted without error. (Functional Suitability)",
            "1. In Last Name field, enter 'Peñaflor'.\n2. In First Name field, enter 'Niño'.\n3. Verify characters are accepted and formatted accurately.",
            "Last Name: 'Peñaflor', First Name: 'Niño'",
            "Functional Suitability",
            "Verified: Regex includes ñ and Ñ character codes."
        ),
        (
            "Pre-Check / 100-Year Maximum Age Boundary Check",
            "Verify entering an applicant birthdate over 120 years ago triggers reasonable age warning. (Negative / Boundary)",
            "1. In Date of Birth, select a date from year 1890 (age 136).\n2. Click 'Continue'.\n3. Verify age boundary warning is raised.",
            "Birthdate: Year 1890, Click 'Continue'",
            "Reliability",
            "Verified: Validates age falls within plausible human boundary (< 120 years)."
        ),
        (
            "Pre-Check / Dim Overlay Presentation",
            "Verify full-window dim overlay renders behind PreCheckDialog. (Usability)",
            "1. Observe application background when PreCheckDialog is open.\n2. Verify background window is dimmed (ShowDimOverlay) to focus attention on identity verification.",
            "Inspect MainWindow dim overlay during PreCheckDialog",
            "Usability",
            "Verified: MainWindow.ShowDimOverlay ensures focused modal interaction."
        )
    ]
