# -*- coding: utf-8 -*-

def get_step1_cases():
    return [
        (
            "RecordDialog Step 1 / Initial Form Presentation",
            "Verify RecordDialog opens at Step 1 with identity details pre-filled from PreCheckDialog. (Functional Suitability)",
            "1. Complete PreCheckDialog with valid applicant identity.\n2. Observe RecordDialog initial state.\n3. Verify Step 1 ('Personal & Location') is active with maroon indicator circle.\n4. Verify Last Name, First Name, Middle Name, Birthdate, and PhilSys are pre-populated.",
            "Advance from PreCheckDialog to RecordDialog",
            "Functional Suitability",
            "Verified: RecordDialog constructor pre-populates fields and sets active step index to 1."
        ),
        (
            "RecordDialog Step 1 / Date of Application Default Value",
            "Verify Date of Application defaults to current calendar date (today). (Functional Suitability)",
            "1. Inspect Date of Application (DpDateOfApplication) in Step 1.\n2. Verify the date picker is set to today's date (DateTime.Today).\n3. Verify date format matches standard display.",
            "Inspect DpDateOfApplication default value",
            "Functional Suitability",
            "Verified: Date of Application initializes to DateTime.Today on new registration."
        ),
        (
            "RecordDialog Step 1 / Date of Application Required Validation",
            "Verify inline error when Date of Application is cleared in Step 1. (Negative / Validation)",
            "1. Clear Date of Application date picker.\n2. Click 'Next Step'.\n3. Verify ErrDateOfApplication displays 'Required.' and border turns red.",
            "Date of Application: null, Click 'Next Step'",
            "Functional Suitability",
            "Verified: ValidateStep1() checks DpDateOfApplication.SelectedDate.HasValue."
        ),
        (
            "RecordDialog Step 1 / Date of Application Future Date Rejection",
            "Verify selecting a future application date is rejected with inline error. (Negative / Boundary)",
            "1. In Date of Application picker, select tomorrow's date.\n2. Click 'Next Step'.\n3. Verify ErrDateOfApplication displays 'Cannot be in the future.'",
            "Application Date: DateTime.Today + 1 day, Click 'Next Step'",
            "Reliability",
            "Verified: Validates SelectedDate > DateTime.Today."
        ),
        (
            "RecordDialog Step 1 / Date of Application Cannot Precede Birthdate",
            "Verify Date of Application cannot precede the applicant's Date of Birth. (Negative / Boundary)",
            "1. Set Date of Birth to '1995-05-15'.\n2. Set Date of Application to '1990-01-01' (prior to birthdate).\n3. Click 'Next Step'.\n4. Verify ErrDateOfApplication displays 'Cannot precede applicant\'s birthdate.'",
            "Application Date set prior to Birthdate, Click 'Next Step'",
            "Reliability",
            "Verified: Validates DpDateOfApplication.SelectedDate < DpBirthdate.SelectedDate."
        ),
        (
            "RecordDialog Step 1 / Last Name Required Validation",
            "Verify inline error when Last Name is cleared in Step 1. (Negative / Validation)",
            "1. In Step 1, clear Last Name field.\n2. Click 'Next Step'.\n3. Verify ErrLastName displays 'Required.' and red border appears.",
            "Last Name: \"\" (Empty), Click 'Next Step'",
            "Functional Suitability",
            "Verified: MarkError(TxtLastName, ErrLastName, 'Required.') highlights control."
        ),
        (
            "RecordDialog Step 1 / Last Name Auto Uppercase Formatting",
            "Verify typing into Last Name field converts text automatically to uppercase. (Usability)",
            "1. In Last Name field, type 'delos santos'.\n2. Verify displayed text converts to 'DELOS SANTOS'.",
            "Type 'delos santos' in TxtLastName",
            "Usability",
            "Verified: TxtLastName_TextChanged calls ToUpperBox(TxtLastName)."
        ),
        (
            "RecordDialog Step 1 / Last Name Numeric Keystroke Blocking",
            "Verify numbers typed into Last Name field are blocked in real time. (Negative / Input Filtering)",
            "1. Focus Last Name field.\n2. Attempt typing 'Cruz123'.\n3. Verify digits '123' are suppressed and only 'CRUZ' is entered.",
            "Keystroke 'Cruz123' in TxtLastName",
            "Reliability",
            "Verified: PreviewTextInput suppresses digits using NameRegex."
        ),
        (
            "RecordDialog Step 1 / Last Name Real-Time Error Clearance",
            "Verify typing into Last Name field clears active validation error label and red border. (Usability)",
            "1. Trigger required validation error on Last Name.\n2. Type valid character into Last Name field.\n3. Verify ErrLastName collapses and red border returns to standard color.",
            "Trigger error -> Type into TxtLastName",
            "Usability",
            "Verified: ClearFieldError(TxtLastName, ErrLastName) executes on TextChanged."
        ),
        (
            "RecordDialog Step 1 / First Name Required Validation",
            "Verify inline error when First Name is cleared in Step 1. (Negative / Validation)",
            "1. Clear First Name field.\n2. Click 'Next Step'.\n3. Verify ErrFirstName displays 'Required.' and red border highlights field.",
            "First Name: \"\" (Empty), Click 'Next Step'",
            "Functional Suitability",
            "Verified: ValidateStep1() flags empty first name and blocks advance."
        ),
        (
            "RecordDialog Step 1 / First Name Auto Uppercase Formatting",
            "Verify typing into First Name field converts text automatically to uppercase. (Usability)",
            "1. In First Name field, type 'ana maria'.\n2. Verify text converts to 'ANA MARIA'.",
            "Type 'ana maria' in TxtFirstName",
            "Usability",
            "Verified: ToUpperBox(TxtFirstName) forces uppercase formatting."
        ),
        (
            "RecordDialog Step 1 / First Name Numeric Keystroke Blocking",
            "Verify numbers typed into First Name are blocked in real time. (Negative / Input Filtering)",
            "1. Attempt typing 'Ana999' into First Name.\n2. Verify '999' is blocked.",
            "Keystroke 'Ana999' in TxtFirstName",
            "Reliability",
            "Verified: Disallows numeric characters via NameRegex."
        ),
        (
            "RecordDialog Step 1 / First Name Real-Time Error Clearance",
            "Verify typing valid character clears First Name error label. (Usability)",
            "1. Trigger required error on First Name.\n2. Type any letter.\n3. Verify ErrFirstName collapses immediately.",
            "Type letter into TxtFirstName after error",
            "Usability",
            "Verified: ClearFieldError(TxtFirstName, ErrFirstName) resets styling."
        ),
        (
            "RecordDialog Step 1 / Middle Name Optional & Auto Uppercase",
            "Verify Middle Name is non-mandatory and auto-formats to uppercase. (Functional Suitability)",
            "1. Leave Middle Name empty and verify validation passes.\n2. Type 'gonzales'; verify it formats to 'GONZALES'.",
            "Middle Name: \"\" then 'gonzales'",
            "Functional Suitability",
            "Verified: Middle name is optional and formats to uppercase."
        ),
        (
            "RecordDialog Step 1 / Date of Birth Required Validation",
            "Verify inline error when Date of Birth is left blank in Step 1. (Negative / Validation)",
            "1. Clear Date of Birth date picker.\n2. Click 'Next Step'.\n3. Verify ErrBirthdate displays 'Required.'",
            "Date of Birth: null, Click 'Next Step'",
            "Functional Suitability",
            "Verified: MarkDateError(DpBirthdate, ErrBirthdate, 'Required.') highlights control."
        ),
        (
            "RecordDialog Step 1 / Date of Birth Future Date Rejection",
            "Verify future birthdate selection is rejected with inline error. (Negative / Boundary)",
            "1. In Date of Birth, select a future date.\n2. Click 'Next Step'.\n3. Verify ErrBirthdate displays 'Cannot be in the future.'",
            "Birthdate: DateTime.Today + 1 year, Click 'Next Step'",
            "Reliability",
            "Verified: Validates SelectedDate > DateTime.Today."
        ),
        (
            "RecordDialog Step 1 / Minimum Applicant Age Boundary (< 15 Years)",
            "Verify birthdate indicating applicant under 15 years old is rejected. (Negative / Boundary)",
            "1. In Date of Birth, select a date 13 years ago.\n2. Click 'Next Step'.\n3. Verify ErrBirthdate displays 'Must be at least 15 years old.'",
            "Birthdate: DateTime.Today - 13 years, Click 'Next Step'",
            "Reliability",
            "Verified: Calculates age from birthdate and enforces minimum age 15."
        ),
        (
            "RecordDialog Step 1 / Real-Time Age Auto-Calculation",
            "Verify auto-calculated Age updates dynamically when selecting a valid birthdate. (Functional Suitability)",
            "1. In Date of Birth, select a date exactly 32 years ago.\n2. Verify Age text box auto-populates '32'.\n3. Select a date 45 years ago; verify Age updates to '45'.",
            "Select birthdate 32 years ago, then 45 years ago",
            "Functional Suitability",
            "Verified: AgeCalculation handler updates TxtAge label in real time."
        ),
        (
            "RecordDialog Step 1 / Age Field Read-Only Enforcement",
            "Verify Age field is strictly read-only and cannot be manually overwritten. (Functional Suitability)",
            "1. In Step 1, click into the Age input box.\n2. Attempt to type characters to alter the age value.\n3. Verify field is read-only and keystrokes are ignored.",
            "Attempt text input in TxtAge",
            "Functional Suitability",
            "Verified: TxtAge.IsReadOnly = true guarantees mathematical integrity with birthdate."
        ),
        (
            "RecordDialog Step 1 / PhilSys Number Optional When Empty",
            "Verify leaving PhilSys Card Number empty passes Step 1 validation. (Functional Suitability)",
            "1. Leave PhilSys Number empty.\n2. Ensure other required Step 1 fields are valid.\n3. Click 'Next Step'.\n4. Verify validation passes and wizard advances to Step 2.",
            "PhilSys: \"\" (Empty), Click 'Next Step'",
            "Functional Suitability",
            "Verified: PhilSys is optional under RA 11861."
        ),
        (
            "RecordDialog Step 1 / PhilSys Number Length Validation (< 16 Digits)",
            "Verify inline error when PhilSys Number has fewer than 16 digits. (Negative / Validation)",
            "1. In PhilSys field, type 10 digits '1234-5678-90'.\n2. Click 'Next Step'.\n3. Verify ErrPhilSys displays 'PhilSys must be 16 digits (XXXX-XXXX-XXXX-XXXX).' and border turns red.",
            "PhilSys: '1234-5678-90' (10 digits), Click 'Next Step'",
            "Functional Suitability",
            "Verified: Strips non-digits; checks philsysRaw.Length != 16."
        ),
        (
            "RecordDialog Step 1 / PhilSys Number Digits Only Enforcement",
            "Verify non-numeric characters typed into PhilSys are blocked in real time. (Negative / Input Filtering)",
            "1. In PhilSys field, attempt typing letters 'PHIL-SYS-CARD'.\n2. Verify letters are suppressed and only digits are accepted.",
            "Keystroke 'PHIL' in TxtPhilSys",
            "Reliability",
            "Verified: NumberOnlyRegex blocks non-digit keystrokes."
        ),
        (
            "RecordDialog Step 1 / PhilSys Number 16-Digit Formatting",
            "Verify entering 16 digits into PhilSys automatically formats into 4 groups of 4 digits. (Usability)",
            "1. In PhilSys field, type '1111222233334444'.\n2. Verify displayed text formats to '1111-2222-3333-4444'.",
            "Input '1111222233334444' in TxtPhilSys",
            "Usability",
            "Verified: Auto-dash formatting groups into XXXX-XXXX-XXXX-XXXX."
        ),
        (
            "RecordDialog Step 1 / PhilSys Number Real-Time Error Clearance",
            "Verify typing valid 16 digits clears active PhilSys error label. (Usability)",
            "1. Trigger PhilSys length error.\n2. Complete the 16 digits.\n3. Verify ErrPhilSys collapses and border returns to normal.",
            "Complete 16 digits in TxtPhilSys",
            "Usability",
            "Verified: ClearFieldError(TxtPhilSys, ErrPhilSys) resets styling."
        ),
        (
            "RecordDialog Step 1 / Validation Gate All Mandatory Fields Empty",
            "Verify clicking 'Next Step' with all mandatory Step 1 fields empty highlights all errors. (Negative / Validation)",
            "1. Clear all fields in Step 1.\n2. Click 'Next Step'.\n3. Verify red error borders appear on Last Name, First Name, Date of Birth, and Date of Application.",
            "Clear all Step 1 fields, Click 'Next Step'",
            "Functional Suitability",
            "Verified: ValidateStep1() returns false and marks all empty required controls."
        ),
        (
            "RecordDialog Step 1 / Header Click Lock",
            "Verify clicking directly on Step 2 or Step 3 header with invalid Step 1 is locked. (Negative / Navigation)",
            "1. In Step 1, leave mandatory fields blank.\n2. Click directly on 'Step 2 (Circumstances & Family)' header indicator circle.\n3. Verify wizard remains locked on Step 1 and does not skip validation.",
            "Click Step 2 header with incomplete Step 1",
            "Functional Suitability",
            "Verified: Header navigation calls ValidateStep1() before permitting step transitions."
        ),
        (
            "RecordDialog Step 1 / Step Indicator State",
            "Verify Step 1 indicator circle displays maroon active fill (#702943) while Step 2 and Step 3 are inactive. (Visual Verification)",
            "1. Inspect step wizard header.\n2. Verify Circle 1 has maroon fill and white number '1'.\n3. Verify Circles 2 and 3 have grey outlines and grey text.",
            "Inspect StepWizard Header at Step 1",
            "Usability",
            "Verified: Step indicators bind to current step index."
        ),
        (
            "RecordDialog Step 1 / PhilSys Existing Duplicate Check on Save",
            "Verify server conflict (409) on duplicate PhilSys highlights PhilSys field with specific error. (Reliability)",
            "1. Enter valid Step 1 data with PhilSys matching an existing record in the database.\n2. Advance and complete form.\n3. Simulate backend 409 conflict response.\n4. Verify SetPhilSysError displays 'PhilSys ID is already registered to an existing record.'",
            "Duplicate PhilSys triggering server 409 conflict",
            "Reliability",
            "Verified: Catches 409 / PhilSys conflict and routes error message directly to ErrPhilSys."
        ),
        (
            "RecordDialog Step 1 / Disallowed Special Characters in Names",
            "Verify characters like '/', '\\', '<', '>', '{', '}' are blocked in all name fields. (Security / Negative)",
            "1. Attempt typing '<script>' into First Name or Last Name.\n2. Verify symbols are completely rejected by NameRegex.",
            "Keystroke '<script>' in name fields",
            "Security",
            "Verified: Special script characters are suppressed, preventing injection."
        ),
        (
            "RecordDialog Step 1 / Single Character Name Boundary",
            "Verify entering a single letter in Last Name or First Name is permitted (e.g. 'U'). (Functional Suitability)",
            "1. In Last Name, enter single character 'U'.\n2. In First Name, enter 'Al'.\n3. Click 'Next Step'; verify single character names are accepted.",
            "Last Name: 'U', First Name: 'Al'",
            "Functional Suitability",
            "Verified: Validates non-empty string; accepts short legal names."
        ),
        (
            "RecordDialog Step 1 / Long Name Handling (50+ Characters)",
            "Verify entering long compound names (50+ characters) is handled cleanly without clipping. (Usability)",
            "1. In First Name field, enter 'Maria Consolacion Esperanza Trinidad De Los Angeles'.\n2. Verify text wraps or scrolls horizontally within text box without visual clipping.",
            "Long compound name in TxtFirstName",
            "Usability",
            "Verified: Text box layout accommodates long strings gracefully."
        ),
        (
            "RecordDialog Step 1 / Advance to Step 2 with Valid Inputs",
            "Verify clicking 'Next Step' with all valid Step 1 inputs advances wizard to Step 2. (Successful)",
            "1. Fill valid Application Date, Last Name, First Name, Date of Birth (Age >= 15), and valid PhilSys.\n2. Click 'Next Step' button.\n3. Verify wizard advances smoothly to Step 2 (Family Composition & Emergency Contact).\n4. Verify Step 2 indicator circle turns active.",
            "Valid Step 1 fields, Click 'Next Step'",
            "Functional Suitability",
            "Verified: ValidateStep1() returns true and sets active step index to 2."
        ),
        (
            "RecordDialog Step 1 / Date Picker Direct Keyboard Input Validation",
            "Verify typing invalid date string directly into DatePicker text box triggers validation handling. (Negative / Input Handling)",
            "1. In Date of Birth text area, type invalid date '99/99/9999'.\n2. Click outside or press Tab.\n3. Verify DatePicker_DateValidationError handles invalid date and reverts or flags error.",
            "Type '99/99/9999' into DpBirthdate",
            "Reliability",
            "Verified: DateValidationError event intercepts unparseable date strings."
        ),
        (
            "RecordDialog Step 1 / Cancel Discard Warning Dialog",
            "Verify clicking 'Cancel' in RecordDialog with modified Step 1 data opens Unsaved Changes warning. (Data Protection)",
            "1. Enter data into Step 1 fields.\n2. Click 'Cancel' button (or close '✕').\n3. Verify 'Unsaved Changes' confirmation dialog opens asking 'Discard unsaved changes?'.\n4. Click 'No'; verify dialog remains open with entered data preserved.",
            "Click 'Cancel' -> Click 'No' in Unsaved Changes dialog",
            "Reliability",
            "Verified: PromptUnsavedChanges checks FormSnapshot and prevents accidental data loss."
        ),
        (
            "RecordDialog Step 1 / Confirm Discard Unsaved Changes",
            "Verify clicking 'Yes' on Unsaved Changes dialog closes RecordDialog and returns to table. (Data Protection)",
            "1. Enter data into Step 1.\n2. Click 'Cancel' button.\n3. When Unsaved Changes warning appears, click 'Yes' (Discard).\n4. Verify RecordDialog closes and user returns to records table.",
            "Click 'Cancel' -> Click 'Yes' in Unsaved Changes dialog",
            "Reliability",
            "Verified: Discards changes, sets DialogResult = false, and closes cleanly."
        )
    ]
