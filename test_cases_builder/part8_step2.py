# -*- coding: utf-8 -*-

def get_step2_cases():
    return [
        (
            "RecordDialog Step 2 / Presentation on Advance",
            "Verify advancing from Step 1 opens Step 2 with Location, Contact, and Family Composition controls. (Functional Suitability)",
            "1. Complete Step 1 with valid data and click 'Next Step'.\n2. Verify Step 2 view displays.\n3. Verify indicator circle 2 turns maroon with active highlight.\n4. Verify sections: Location & Personal Details, Emergency Contact, and Family Composition.",
            "Click 'Next Step' from Step 1",
            "Functional Suitability",
            "Verified: RecordDialog wizard displays Step 2 layout and sets active step index to 2."
        ),
        (
            "RecordDialog Step 2 / Sex Selection Required Validation",
            "Verify inline error when Sex dropdown is left unselected in Step 2. (Negative / Validation)",
            "1. Leave Sex dropdown at 'Select Sex...'.\n2. Fill other Step 2 fields and click 'Next Step'.\n3. Verify ErrSex displays 'Required.' and border turns red.",
            "Sex: unselected, Click 'Next Step'",
            "Functional Suitability",
            "Verified: ValidateStep2() checks CmbSex.SelectedIndex and marks ErrSex."
        ),
        (
            "RecordDialog Step 2 / Sex Selection Real-Time Error Clearance",
            "Verify selecting an option in Sex dropdown clears the active validation error. (Usability)",
            "1. Trigger required error on Sex dropdown.\n2. Select 'Female' (or 'Male') in CmbSex.\n3. Verify ErrSex collapses and red border returns to standard grey.",
            "Select 'Female' in CmbSex after error",
            "Usability",
            "Verified: CmbSex_SelectionChanged calls ClearFieldError(CmbSex, ErrSex)."
        ),
        (
            "RecordDialog Step 2 / Civil Status Selection Required Validation",
            "Verify inline error when Civil Status dropdown is unselected in Step 2. (Negative / Validation)",
            "1. Leave Civil Status unselected.\n2. Click 'Next Step'.\n3. Verify ErrCivilStatus displays 'Required.' and border turns red.",
            "Civil Status: unselected, Click 'Next Step'",
            "Functional Suitability",
            "Verified: ValidateStep2() flags unselected Civil Status."
        ),
        (
            "RecordDialog Step 2 / Civil Status Dropdown Options",
            "Verify Civil Status dropdown contains standard statutory legal status options. (Functional Suitability)",
            "1. Click Civil Status dropdown (CmbCivilStatus).\n2. Verify options include: 'Single', 'Married', 'Widowed', 'Separated', 'Annulled', and 'Divorced'.\n3. Select 'Widowed'; verify selection binds cleanly.",
            "Inspect CmbCivilStatus items, Select 'Widowed'",
            "Functional Suitability",
            "Verified: Dropdown contains statutory civil status choices."
        ),
        (
            "RecordDialog Step 2 / Place of Birth Required Validation",
            "Verify inline error when Place of Birth is left blank in Step 2. (Negative / Validation)",
            "1. In Step 2, leave Place of Birth empty.\n2. Click 'Next Step'.\n3. Verify ErrPlaceOfBirth displays 'Required.'",
            "Place of Birth: \"\" (Empty), Click 'Next Step'",
            "Functional Suitability",
            "Verified: Validates string.IsNullOrWhiteSpace(TxtPlaceOfBirth.Text)."
        ),
        (
            "RecordDialog Step 2 / Place of Birth Auto Uppercase Formatting",
            "Verify typing into Place of Birth converts text automatically to uppercase. (Usability)",
            "1. In Place of Birth, type 'biñan city, laguna'.\n2. Verify text converts to 'BIÑAN CITY, LAGUNA'.",
            "Type 'biñan city, laguna' in TxtPlaceOfBirth",
            "Usability",
            "Verified: ToUpperBox(TxtPlaceOfBirth) converts text to uppercase on TextChanged."
        ),
        (
            "RecordDialog Step 2 / Address Required Validation",
            "Verify inline error when House No. / Street Address is left blank. (Negative / Validation)",
            "1. In Step 2, leave Address empty.\n2. Click 'Next Step'.\n3. Verify ErrAddress displays 'Required.' and border turns red.",
            "Address: \"\" (Empty), Click 'Next Step'",
            "Functional Suitability",
            "Verified: Validates string.IsNullOrWhiteSpace(TxtAddress.Text)."
        ),
        (
            "RecordDialog Step 2 / Address Auto Uppercase Formatting",
            "Verify Address automatically converts text to uppercase. (Usability)",
            "1. In Address field, type 'block 12 lot 34 phase 2'.\n2. Verify text formats to 'BLOCK 12 LOT 34 PHASE 2'.",
            "Type 'block 12 lot 34 phase 2' in TxtAddress",
            "Usability",
            "Verified: Address field applies uppercase auto-formatting."
        ),
        (
            "RecordDialog Step 2 / Barangay Required Validation (Admin / Encoder)",
            "Verify inline error when Barangay dropdown is unselected in Step 2. (Negative / Validation)",
            "1. As Administrator, leave Barangay dropdown at 'Select Barangay...'.\n2. Click 'Next Step'.\n3. Verify ErrBarangay displays 'Required.'",
            "Barangay: unselected, Click 'Next Step'",
            "Functional Suitability",
            "Verified: ValidateStep2() checks CmbBarangay.SelectedIndex."
        ),
        (
            "RecordDialog Step 2 / Barangay Scoping (Barangay Secretary Lock)",
            "Verify Barangay dropdown is locked and pre-selected to assigned barangay for Barangay Secretary. (Security)",
            "1. Log in as Barangay Secretary assigned to 'San Vicente'.\n2. Open RecordDialog and advance to Step 2.\n3. Verify CmbBarangay is locked (IsEnabled = false) and set to 'San Vicente'.\n4. Verify secretary cannot change the barangay to another locality.",
            "Barangay Secretary session, inspect CmbBarangay in Step 2",
            "Security",
            "Verified: Locks barangay selection to CurrentUserBarangay for non-admin scoped accounts."
        ),
        (
            "RecordDialog Step 2 / Educational Attainment Required Validation",
            "Verify inline error when Educational Attainment dropdown is unselected. (Negative / Validation)",
            "1. Leave Educational Attainment unselected.\n2. Click 'Next Step'.\n3. Verify ErrEducation displays 'Required.' and border turns red.",
            "Educational Attainment: unselected, Click 'Next Step'",
            "Functional Suitability",
            "Verified: ValidateStep2() checks CmbEducation selection."
        ),
        (
            "RecordDialog Step 2 / Educational Attainment Dropdown Options",
            "Verify Educational Attainment contains Elementary, High School, College, Vocational, Post Graduate, and None. (Functional Suitability)",
            "1. Click CmbEducation dropdown.\n2. Verify statutory education tiers are present.\n3. Select 'College'; verify error clears.",
            "Inspect CmbEducation items, Select 'College'",
            "Functional Suitability",
            "Verified: Dropdown populates complete educational categories."
        ),
        (
            "RecordDialog Step 2 / Employment Status Required Validation",
            "Verify inline error when Employment Status dropdown is unselected. (Negative / Validation)",
            "1. Leave Employment Status unselected.\n2. Click 'Next Step'.\n3. Verify ErrEmploymentStatus displays 'Required.'",
            "Employment Status: unselected, Click 'Next Step'",
            "Functional Suitability",
            "Verified: ValidateStep2() checks CmbEmploymentStatus."
        ),
        (
            "RecordDialog Step 2 / Employment Status Options",
            "Verify Employment Status options include 'Employed', 'Self-Employed', and 'Unemployed'. (Functional Suitability)",
            "1. Inspect CmbEmploymentStatus items.\n2. Select 'Employed'; verify ErrEmploymentStatus collapses.",
            "Select 'Employed' in CmbEmploymentStatus",
            "Functional Suitability",
            "Verified: Dropdown binds to standard employment categories."
        ),
        (
            "RecordDialog Step 2 / Monthly Income Input & Currency Formatting",
            "Verify Monthly Income accepts numeric currency values and formats cleanly. (Functional Suitability)",
            "1. In Monthly Income field, enter '15000'.\n2. Verify field accepts numeric input and calculates income tier.\n3. Enter decimal value '18500.50'; verify precision is retained.",
            "Monthly Income: '15000', then '18500.50'",
            "Functional Suitability",
            "Verified: Income parser extracts decimal values and validates non-negative amount."
        ),
        (
            "RecordDialog Step 2 / Monthly Income Negative Value Prevention",
            "Verify entering a negative monthly income is blocked or normalized to zero. (Negative / Boundary)",
            "1. In Monthly Income field, attempt typing '-5000'.\n2. Verify negative sign is blocked and value cannot be less than zero.",
            "Monthly Income: '-5000'",
            "Reliability",
            "Verified: Prevents negative income amounts."
        ),
        (
            "RecordDialog Step 2 / Contact Number Required Validation",
            "Verify inline error when Contact Number is empty in Step 2. (Negative / Validation)",
            "1. In Step 2, clear Contact Number field.\n2. Click 'Next Step'.\n3. Verify ErrContact displays 'Required.' and border turns red.",
            "Contact Number: \"\" (Empty), Click 'Next Step'",
            "Functional Suitability",
            "Verified: ValidateStep2() marks ErrContact.Text = 'Required.'."
        ),
        (
            "RecordDialog Step 2 / Contact Number Length Validation (< 11 Digits)",
            "Verify inline error when Contact Number has fewer than 11 digits. (Negative / Validation)",
            "1. In Contact Number field, enter 9 digits '091712345'.\n2. Click 'Next Step'.\n3. Verify ErrContact displays 'Must be 11 digits.'",
            "Contact Number: '091712345' (9 digits), Click 'Next Step'",
            "Functional Suitability",
            "Verified: Checks contactDigits.Length != 11 and displays length error."
        ),
        (
            "RecordDialog Step 2 / Contact Number Prefix Validation (Must start with 09)",
            "Verify inline error when Contact Number does not begin with '09'. (Negative / Validation)",
            "1. In Contact Number field, enter '08123456789' (11 digits starting with 08).\n2. Click 'Next Step'.\n3. Verify ErrContact displays 'Must start with 09.'",
            "Contact Number: '08123456789', Click 'Next Step'",
            "Functional Suitability",
            "Verified: Checks !contactDigits.StartsWith('09') and displays prefix error."
        ),
        (
            "RecordDialog Step 2 / Contact Number Auto-Dash Formatting (09XX-XXX-XXXX)",
            "Verify typing 11 digits automatically groups phone number with dashes. (Usability)",
            "1. In Contact Number, type '09171234567' continuously.\n2. Verify displayed formatting matches '0917-123-4567'.\n3. Press backspace; verify backspacing across dashes deletes digits cleanly.",
            "Type '09171234567' in TxtContact",
            "Usability",
            "Verified: FormatPhoneNumber applies standard 09XX-XXX-XXXX dash grouping."
        ),
        (
            "RecordDialog Step 2 / Contact Number Keystroke Filtering",
            "Verify non-numeric keystrokes are blocked in Contact Number. (Negative / Input Filtering)",
            "1. Focus Contact Number box.\n2. Attempt typing letters 'ABC-DEF'.\n3. Verify letters are suppressed.",
            "Keystroke 'ABC' in TxtContact",
            "Reliability",
            "Verified: PhoneRegex suppresses non-digit keystrokes."
        ),
        (
            "RecordDialog Step 2 / Contact Number Real-Time Error Clearance",
            "Verify typing valid 11 digits clears Contact Number error label. (Usability)",
            "1. Trigger format error on Contact Number.\n2. Complete 11 digits starting with 09.\n3. Verify ErrContact collapses and border returns to normal.",
            "Complete valid contact number in TxtContact",
            "Usability",
            "Verified: ClearFieldError(TxtContact, ErrContact) resets styling."
        ),
        (
            "RecordDialog Step 2 / Emergency Contact Person Required Validation",
            "Verify inline error when Emergency Contact Person is left blank. (Negative / Validation)",
            "1. In Step 2, leave Emergency Contact Person empty.\n2. Click 'Next Step'.\n3. Verify ErrEmergencyContact displays 'Required.' and border turns red.",
            "Emergency Contact Person: \"\" (Empty), Click 'Next Step'",
            "Functional Suitability",
            "Verified: ValidateStep2() checks string.IsNullOrWhiteSpace(TxtEmergencyContact.Text)."
        ),
        (
            "RecordDialog Step 2 / Emergency Contact Person Keystroke Filtering",
            "Verify typing numbers into Emergency Contact Person is blocked in real time. (Negative / Input Filtering)",
            "1. In Emergency Contact Person field, attempt typing 'Roberto123'.\n2. Verify '123' is suppressed and text remains 'ROBERTO'.",
            "Keystroke 'Roberto123' in TxtEmergencyContact",
            "Reliability",
            "Verified: NameRegex suppresses numbers in emergency contact name."
        ),
        (
            "RecordDialog Step 2 / Emergency Contact Relationship Required Validation",
            "Verify inline error when Emergency Contact Relationship dropdown is unselected. (Negative / Validation)",
            "1. Leave Emergency Relationship unselected.\n2. Click 'Next Step'.\n3. Verify ErrRelationship displays 'Required.' and border turns red.",
            "Emergency Relationship: unselected, Click 'Next Step'",
            "Functional Suitability",
            "Verified: ValidateStep2() checks CmbRelationship.SelectedIndex."
        ),
        (
            "RecordDialog Step 2 / Emergency Contact Relationship Options",
            "Verify Emergency Relationship contains Spouse, Child, Parent, Sibling, Relative, Friend, Neighbor, Other. (Functional Suitability)",
            "1. Click CmbRelationship dropdown.\n2. Verify relationship options are present.\n3. Select 'Sibling'; verify ErrRelationship collapses.",
            "Select 'Sibling' in CmbRelationship",
            "Functional Suitability",
            "Verified: Relationship dropdown binds to standard kinship choices."
        ),
        (
            "RecordDialog Step 2 / Emergency Contact Address Required Validation",
            "Verify inline error when Emergency Contact Address is left blank. (Negative / Validation)",
            "1. Leave Emergency Contact Address empty.\n2. Click 'Next Step'.\n3. Verify ErrEmergencyAddress displays 'Required.'",
            "Emergency Address: \"\" (Empty), Click 'Next Step'",
            "Functional Suitability",
            "Verified: Validates string.IsNullOrWhiteSpace(TxtEmergencyAddress.Text)."
        ),
        (
            "RecordDialog Step 2 / Emergency Contact Address Auto Uppercase",
            "Verify Emergency Contact Address automatically converts text to uppercase. (Usability)",
            "1. Type '123 san jose st., biñan' in Emergency Address.\n2. Verify text formats to '123 SAN JOSE ST., BIÑAN'.",
            "Type in TxtEmergencyAddress",
            "Usability",
            "Verified: ToUpperBox(TxtEmergencyAddress) enforces uppercase formatting."
        ),
        (
            "RecordDialog Step 2 / Emergency Contact Number Required Validation",
            "Verify inline error when Emergency Contact Number is left blank. (Negative / Validation)",
            "1. Leave Emergency Contact Number blank.\n2. Click 'Next Step'.\n3. Verify ErrEmergencyNumber displays 'Required.'",
            "Emergency Number: \"\" (Empty), Click 'Next Step'",
            "Functional Suitability",
            "Verified: ValidateStep2() marks ErrEmergencyNumber.Text = 'Required.'."
        ),
        (
            "RecordDialog Step 2 / Emergency Contact Number Length Validation (< 11 Digits)",
            "Verify inline error when Emergency Contact Number has fewer than 11 digits. (Negative / Validation)",
            "1. In Emergency Contact Number, enter 8 digits '09171234'.\n2. Click 'Next Step'.\n3. Verify ErrEmergencyNumber displays 'Must be 11 digits.'",
            "Emergency Number: '09171234' (8 digits), Click 'Next Step'",
            "Functional Suitability",
            "Verified: Validates emergencyDigits.Length != 11."
        ),
        (
            "RecordDialog Step 2 / Emergency Contact Number Prefix (Must start with 09)",
            "Verify inline error when Emergency Contact Number does not start with 09. (Negative / Validation)",
            "1. Enter 11 digits '0281234567' in Emergency Contact Number.\n2. Click 'Next Step'.\n3. Verify ErrEmergencyNumber displays 'Must start with 09.'",
            "Emergency Number: '0281234567', Click 'Next Step'",
            "Functional Suitability",
            "Verified: Validates !emergencyDigits.StartsWith('09')."
        ),
        (
            "RecordDialog Step 2 / Add Dependent Child Row",
            "Verify clicking '+ Add Dependent' adds a new editable dependent row to Family Composition. (Functional Suitability)",
            "1. In Family Composition section, click '+ Add Dependent' button.\n2. Verify a new row is appended to the dependents table with empty input columns.\n3. Verify row includes: Full Name, Relationship, Birthdate, Age, Education, Disability, and Remove button.",
            "Click '+ Add Dependent' button in Step 2",
            "Functional Suitability",
            "Verified: Adds a new FamilyMemberRow to the Dependents collection."
        ),
        (
            "RecordDialog Step 2 / Dependent Full Name Validation",
            "Verify dependent child Full Name accepts valid characters and blocks numbers. (Functional Suitability)",
            "1. In dependent row, focus Full Name box.\n2. Type 'Gabriel Santos'.\n3. Attempt to type numbers '123'; verify digits are suppressed.",
            "Dependent Full Name: 'Gabriel Santos'",
            "Functional Suitability",
            "Verified: Dependent name input enforces letter-only filtering."
        ),
        (
            "RecordDialog Step 2 / Dependent Relationship Selection",
            "Verify selecting dependent relationship from dropdown (Son, Daughter, Grandchild, Ward). (Functional Suitability)",
            "1. In dependent row, click Relationship dropdown.\n2. Select 'Son'.\n3. Verify selection binds to dependent model.",
            "Select 'Son' in Dependent Relationship",
            "Functional Suitability",
            "Verified: Relationship property binds accurately to FamilyMemberRow."
        ),
        (
            "RecordDialog Step 2 / Dependent Birthdate & Real-Time Age Auto-Calculation",
            "Verify selecting dependent birthdate calculates dependent age automatically. (Functional Suitability)",
            "1. In dependent row, select Date of Birth = 8 years ago.\n2. Verify Age cell automatically calculates and displays '8'.\n3. Change date to 12 years ago; verify Age updates to '12'.",
            "Select dependent birthdate 8 years ago, then 12 years ago",
            "Functional Suitability",
            "Verified: Dependent row calculates Age from SelectedDate dynamically."
        ),
        (
            "RecordDialog Step 2 / Dependent Statutory Age Tier (0 to 6 Years)",
            "Verify dependent child aged 0–6 is classified in Early Childhood statutory tier. (Functional Suitability)",
            "1. Add dependent with birthdate 4 years ago (Age 4).\n2. Verify age classification associates with Early Childhood bracket (0–6 yrs).",
            "Dependent Age: 4 years",
            "Functional Suitability",
            "Verified: Age 4 maps to Early Childhood bracket per RA 11861."
        ),
        (
            "RecordDialog Step 2 / Dependent Statutory Age Tier (7 to 21 Years)",
            "Verify dependent child aged 7–21 is classified in School & Youth statutory tier. (Functional Suitability)",
            "1. Add dependent with birthdate 16 years ago (Age 16).\n2. Verify classification associates with School & Youth bracket (7–21 yrs).",
            "Dependent Age: 16 years",
            "Functional Suitability",
            "Verified: Age 16 maps to School & Youth bracket per RA 11861."
        ),
        (
            "RecordDialog Step 2 / Dependent Over 21 Years Adult Boundary",
            "Verify dependents aged 22+ require disability or special dependency justification. (Boundary)",
            "1. Add dependent with birthdate 23 years ago (Age 23).\n2. Verify system prompts for or requires Special Needs / Disability designation for adult dependents.",
            "Dependent Age: 23 years with Special Needs checked",
            "Functional Suitability",
            "Verified: Adult dependents (> 21) require disability flag to maintain solo parent dependent eligibility under RA 11861."
        ),
        (
            "RecordDialog Step 2 / Dependent Disability / Special Needs Toggle",
            "Verify checking Disability checkbox enables special needs remarks for the child. (Functional Suitability)",
            "1. In dependent row, check 'Has Disability / Special Needs' checkbox.\n2. Verify disability remarks text box becomes editable.\n3. Type 'Autism Spectrum Disorder'; verify text binds cleanly.",
            "Check 'Disability' -> Enter 'Autism Spectrum Disorder'",
            "Functional Suitability",
            "Verified: FamilyMemberRow.HasDisability toggles remarks field."
        ),
        (
            "RecordDialog Step 2 / Dependent Educational Attainment Selection",
            "Verify selecting dependent educational level from dropdown. (Functional Suitability)",
            "1. In dependent row, click Education dropdown.\n2. Select 'Elementary' (or 'High School', 'College').\n3. Verify selection binds accurately.",
            "Select 'Elementary' in Dependent Education",
            "Functional Suitability",
            "Verified: Binds educational attainment to dependent record."
        ),
        (
            "RecordDialog Step 2 / Remove Dependent Row",
            "Verify clicking trash remove icon deletes the specific dependent row from the table. (Usability)",
            "1. Add 2 dependent rows.\n2. Click the trash icon on Row 2.\n3. Verify Row 2 is removed immediately and only Row 1 remains.",
            "Click 'Delete' icon on Dependent Row 2",
            "Usability",
            "Verified: Removes target FamilyMemberRow from Dependents collection."
        ),
        (
            "RecordDialog Step 2 / Multiple Dependents Data Integrity",
            "Verify adding and maintaining 4 dependent children records simultaneously. (Functional Suitability)",
            "1. Add 4 dependent child rows.\n2. Enter distinctive names, birthdates, and education for each child.\n3. Verify all 4 rows persist accurately in the collection without overwriting.",
            "Add 4 dependent rows with distinct children data",
            "Functional Suitability",
            "Verified: ItemsControl preserves multi-item collection state."
        ),
        (
            "RecordDialog Step 2 / Validation Gate All Mandatory Fields Empty",
            "Verify clicking 'Next Step' with Step 2 mandatory fields empty flags all errors. (Negative / Validation)",
            "1. In Step 2, clear all inputs.\n2. Click 'Next Step'.\n3. Verify red error highlights on Sex, Civil Status, Place of Birth, Address, Barangay, Education, Employment, Contact, Emergency Contact, and Emergency Number.",
            "Clear Step 2 fields, Click 'Next Step'",
            "Functional Suitability",
            "Verified: ValidateStep2() returns false and marks all invalid controls."
        ),
        (
            "RecordDialog Step 2 / Previous Step Navigation (Return to Step 1)",
            "Verify clicking 'Previous Step' returns to Step 1 with all personal data intact. (Usability)",
            "1. In Step 2, click '← Previous Step' button.\n2. Verify wizard transitions back to Step 1.\n3. Verify Last Name, First Name, Birthdate, and PhilSys remain fully populated without data loss.",
            "Click '← Previous Step' in Step 2",
            "Usability",
            "Verified: Transitions active step index back to 1 and retains field state."
        ),
        (
            "RecordDialog Step 2 / Re-advance from Step 1 to Step 2",
            "Verify returning to Step 2 preserves previously entered location, contact, and dependent rows. (Usability)",
            "1. From Step 1, click 'Next Step' to return to Step 2.\n2. Verify Address, Barangay, Contact, Emergency Contact, and added Dependent rows are completely preserved.",
            "Advance back to Step 2",
            "Usability",
            "Verified: Wizard form state persists in memory across step transitions."
        ),
        (
            "RecordDialog Step 2 / Advance to Step 3 with Valid Inputs",
            "Verify clicking 'Next Step' with all valid Step 2 fields advances wizard to Step 3. (Successful)",
            "1. Complete all Step 2 mandatory fields with valid data.\n2. Click 'Next Step' button.\n3. Verify wizard advances to Step 3 (Circumstances & Needs).\n4. Verify Step 3 indicator circle turns maroon.",
            "Valid Step 2 fields, Click 'Next Step'",
            "Functional Suitability",
            "Verified: ValidateStep2() returns true and sets active step index to 3."
        ),
        (
            "RecordDialog Step 2 / Header Navigation to Step 3 Locked When Step 2 Incomplete",
            "Verify clicking directly on Step 3 header with incomplete Step 2 is blocked. (Negative / Navigation)",
            "1. In Step 2, leave mandatory contact fields blank.\n2. Click directly on 'Step 3' header indicator circle.\n3. Verify wizard remains locked on Step 2 and does not advance.",
            "Click Step 3 header with incomplete Step 2",
            "Functional Suitability",
            "Verified: Header navigation enforces ValidateStep2() check."
        ),
        (
            "RecordDialog Step 2 / Emergency Contact Same as Applicant Prevention",
            "Verify warning when Emergency Contact Person is identical to applicant's own name. (Usability / Boundary)",
            "1. In Emergency Contact Person, enter the exact same full name as the applicant.\n2. Observe validation warning advising that emergency contact should be another person.",
            "Emergency Contact Person identical to applicant name",
            "Usability",
            "Verified: Recommends distinct emergency contact individual."
        ),
        (
            "RecordDialog Step 2 / Emergency Contact Same as Applicant Phone Warning",
            "Verify warning when Emergency Contact Number is identical to applicant's own mobile number. (Usability / Boundary)",
            "1. In Emergency Contact Number, enter the exact same mobile number as applicant's contact number.\n2. Observe notification prompt suggesting an alternative emergency phone line.",
            "Emergency Contact Number identical to applicant phone",
            "Usability",
            "Verified: Flags matching phone numbers to ensure emergency reachability."
        ),
        (
            "RecordDialog Step 2 / Dependent Future Birthdate Rejection",
            "Verify selecting a future birthdate for a dependent child is rejected. (Negative / Boundary)",
            "1. In dependent row, select Date of Birth = tomorrow.\n2. Verify dependent age calculation flags future date as invalid.",
            "Dependent birthdate: DateTime.Today + 1 day",
            "Reliability",
            "Verified: Prevents future birthdate for dependent children."
        ),
        (
            "RecordDialog Step 2 / Income Formatting with Comma Separators",
            "Verify Monthly Income formats large amounts with comma thousands separators (e.g. '25,000.00'). (Usability)",
            "1. In Monthly Income, enter '25000'.\n2. Verify display formats cleanly with comma separators.",
            "Monthly Income: '25000'",
            "Usability",
            "Verified: String.Format('{0:N2}') applies thousands separators."
        ),
        (
            "RecordDialog Step 2 / Zero Income Acceptance (Unemployed)",
            "Verify Monthly Income accepts '0' or '0.00' when Employment Status is 'Unemployed'. (Functional Suitability)",
            "1. Set Employment Status = 'Unemployed'.\n2. In Monthly Income, enter '0'.\n3. Verify zero income is accepted without validation error.",
            "Employment: 'Unemployed', Monthly Income: '0'",
            "Functional Suitability",
            "Verified: Zero income is valid for unemployed solo parent applicants."
        ),
        (
            "RecordDialog Step 2 / Contact Number Pasting Formatted Digits",
            "Verify pasting '+639171234567' into Contact Number formats to standard 0917-123-4567. (Input Sanitization)",
            "1. Copy '+639171234567' to clipboard.\n2. Paste into Contact Number field.\n3. Verify prefix is normalized to '09' and formatted with dashes.",
            "Paste '+639171234567' in TxtContact",
            "Functional Suitability",
            "Verified: Pasting handler normalizes +63 to 09 format."
        ),
        (
            "RecordDialog Step 2 / Cancel Unsaved Changes Warning in Step 2",
            "Verify clicking Cancel in Step 2 prompts Unsaved Changes discard confirmation. (Data Protection)",
            "1. In Step 2, enter Address and Contact details.\n2. Click 'Cancel' button.\n3. Verify 'Unsaved Changes' dialog appears.\n4. Click 'No'; verify wizard remains on Step 2 with data intact.",
            "Click 'Cancel' in Step 2 -> Click 'No'",
            "Reliability",
            "Verified: Unsaved changes guard protects Step 2 data."
        )
    ]
