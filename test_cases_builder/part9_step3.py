# -*- coding: utf-8 -*-

def get_step3_cases():
    return [
        (
            "RecordDialog Step 3 / Presentation on Advance",
            "Verify advancing from Step 2 opens Step 3 with Circumstances, Needs, and Review controls. (Functional Suitability)",
            "1. Complete Step 2 with valid data and click 'Next Step'.\n2. Verify Step 3 ('Circumstances & Needs') is displayed.\n3. Verify indicator circle 3 is active with maroon fill.\n4. Verify Circumstance codes A1–F, Needs & Problems, and Assistance Needed sections appear.",
            "Click 'Next Step' from Step 2",
            "Functional Suitability",
            "Verified: RecordDialog wizard advances to Step 3 layout and sets active step index to 3."
        ),
        (
            "RecordDialog Step 3 / Circumstance Selection Required Validation",
            "Verify inline error when no circumstance category (A1–F) is selected upon clicking 'Review & Save'. (Negative / Validation)",
            "1. In Step 3, leave all circumstance checkboxes/options unselected.\n2. Click 'Review & Save' button.\n3. Verify validation error banner appears requiring selection of at least one statutory circumstance code.",
            "Circumstance: None selected, Click 'Review & Save'",
            "Functional Suitability",
            "Verified: ValidateFields() checks circumstance selection and displays statutory requirement notice."
        ),
        (
            "RecordDialog Step 3 / Circumstance A1 Selection (Rape Victim)",
            "Verify selecting statutory code A1 (Rape) captures circumstance and enables sensitive case handling. (Functional Suitability)",
            "1. In Step 3, check Circumstance 'A1' (Birth as a consequence of rape).\n2. Verify option is checked and statutory code binds to applicant dossier.",
            "Select Circumstance 'A1'",
            "Functional Suitability",
            "Verified: Binds RA 11861 Code A1 to solo parent classification."
        ),
        (
            "RecordDialog Step 3 / Circumstance A2 Selection (Death of Spouse)",
            "Verify selecting statutory code A2 (Death of Spouse) captures widow/widower classification and date of death. (Functional Suitability)",
            "1. In Step 3, check Circumstance 'A2' (Death of spouse).\n2. Verify death of spouse classification binds.\n3. Enter date of spouse's death in subfield if applicable.",
            "Select Circumstance 'A2'",
            "Functional Suitability",
            "Verified: Binds RA 11861 Code A2 (Death of spouse)."
        ),
        (
            "RecordDialog Step 3 / Circumstance A3 Selection (Detention of Spouse)",
            "Verify selecting statutory code A3 (Detention / Imprisonment of spouse for at least 3 months). (Functional Suitability)",
            "1. In Step 3, check Circumstance 'A3' (Detention of spouse).\n2. Verify imprisonment criteria (minimum 3 months) binds to classification.",
            "Select Circumstance 'A3'",
            "Functional Suitability",
            "Verified: Binds RA 11861 Code A3 (Detention of spouse)."
        ),
        (
            "RecordDialog Step 3 / Circumstance A4 Selection (Incapacity of Spouse)",
            "Verify selecting statutory code A4 (Physical / Mental Incapacity of spouse). (Functional Suitability)",
            "1. In Step 3, check Circumstance 'A4' (Physical/mental incapacity of spouse).\n2. Verify medical incapacity classification binds to applicant profile.",
            "Select Circumstance 'A4'",
            "Functional Suitability",
            "Verified: Binds RA 11861 Code A4 (Physical/mental incapacity)."
        ),
        (
            "RecordDialog Step 3 / Circumstance A5 Selection (Legal Separation)",
            "Verify selecting statutory code A5 (Legal or de facto separation for at least 6 months). (Functional Suitability)",
            "1. In Step 3, check Circumstance 'A5' (Legal separation).\n2. Verify 6-month statutory separation condition binds cleanly.",
            "Select Circumstance 'A5'",
            "Functional Suitability",
            "Verified: Binds RA 11861 Code A5 (Legal/de facto separation)."
        ),
        (
            "RecordDialog Step 3 / Circumstance A6 Selection (Annulment / Nullity)",
            "Verify selecting statutory code A6 (Declaration of nullity or annulment of marriage). (Functional Suitability)",
            "1. In Step 3, check Circumstance 'A6' (Annulment/nullity of marriage).\n2. Verify court-declared nullity classification binds to applicant profile.",
            "Select Circumstance 'A6'",
            "Functional Suitability",
            "Verified: Binds RA 11861 Code A6 (Annulment/Nullity)."
        ),
        (
            "RecordDialog Step 3 / Circumstance A7 Selection (Abandonment)",
            "Verify selecting statutory code A7 (Abandonment of spouse for at least 6 months). (Functional Suitability)",
            "1. In Step 3, check Circumstance 'A7' (Abandonment of spouse).\n2. Verify 6-month statutory abandonment condition binds cleanly.",
            "Select Circumstance 'A7'",
            "Functional Suitability",
            "Verified: Binds RA 11861 Code A7 (Abandonment)."
        ),
        (
            "RecordDialog Step 3 / Circumstance B Selection (Unmarried Mother/Father)",
            "Verify selecting statutory code B (Unmarried parent who kept and reared child). (Functional Suitability)",
            "1. In Step 3, check Circumstance 'B' (Unmarried parent).\n2. Verify unmarried solo parent classification binds to record.",
            "Select Circumstance 'B'",
            "Functional Suitability",
            "Verified: Binds RA 11861 Code B (Unmarried parent)."
        ),
        (
            "RecordDialog Step 3 / Circumstance C Selection (Legal Guardian / Foster Parent)",
            "Verify selecting statutory code C (Legal guardian, adoptive or foster parent). (Functional Suitability)",
            "1. In Step 3, check Circumstance 'C' (Legal guardian / foster parent).\n2. Verify guardian solo parent classification binds to record.",
            "Select Circumstance 'C'",
            "Functional Suitability",
            "Verified: Binds RA 11861 Code C (Legal guardian)."
        ),
        (
            "RecordDialog Step 3 / Circumstance D Selection (Relative Custody)",
            "Verify selecting statutory code D (Relative within fourth civil degree providing sole care). (Functional Suitability)",
            "1. In Step 3, check Circumstance 'D' (Relative caregiver within 4th civil degree).\n2. Verify relative caregiver classification binds.",
            "Select Circumstance 'D'",
            "Functional Suitability",
            "Verified: Binds RA 11861 Code D (Relative caregiver)."
        ),
        (
            "RecordDialog Step 3 / Circumstance E Selection (OFW Spouse Continuous Absence)",
            "Verify selecting statutory code E (Spouse is low/semi-skilled OFW away continuously). (Functional Suitability)",
            "1. In Step 3, check Circumstance 'E' (OFW spouse away continuously).\n2. Verify overseas worker spouse category binds to record.",
            "Select Circumstance 'E'",
            "Functional Suitability",
            "Verified: Binds RA 11861 Code E (OFW spouse)."
        ),
        (
            "RecordDialog Step 3 / Circumstance F Selection (Other Statutory Circumstances)",
            "Verify selecting statutory code F requires entering specific circumstance details. (Functional Suitability)",
            "1. In Step 3, check Circumstance 'F' (Other circumstances).\n2. Verify remarks text box becomes mandatory for specifying details.\n3. Enter 'De facto single parent due to prolonged medical abandonment'; verify text binds.",
            "Select Circumstance 'F' -> Enter details in remarks",
            "Functional Suitability",
            "Verified: Requires textual explanation for 'Other' statutory code."
        ),
        (
            "RecordDialog Step 3 / Circumstance Details & Remarks Input",
            "Verify typing into Circumstance Details / Remarks text box. (Functional Suitability)",
            "1. In Remarks / Details box, enter case background narrative.\n2. Verify multi-line text input scrolls smoothly and preserves line breaks.",
            "Multi-line narrative in TxtRemarks",
            "Functional Suitability",
            "Verified: Captures case background notes in SoloParentRecord.Remarks."
        ),
        (
            "RecordDialog Step 3 / Needs and Problems Input",
            "Verify entering applicant's identified needs and problems. (Functional Suitability)",
            "1. In Needs / Problems section, enter 'High cost of school supplies and daily nutritional needs for 3 young children'.\n2. Verify input is stored and binds to NeedsAndProblems property.",
            "Text in TxtNeedsAndProblems",
            "Functional Suitability",
            "Verified: Captures economic and social needs for caseworkers."
        ),
        (
            "RecordDialog Step 3 / Family Resources and Housing Condition",
            "Verify selecting housing condition (Owned, Rented, Living with Relatives, Informal Settler). (Functional Suitability)",
            "1. In Housing Condition dropdown, select 'Rented'.\n2. Verify housing category binds accurately to socioeconomic profile.",
            "Select 'Rented' in Housing Condition",
            "Functional Suitability",
            "Verified: Binds housing tenure to applicant profile."
        ),
        (
            "RecordDialog Step 3 / Assistance Needed Categorization",
            "Verify selecting or entering primary assistance requested (Livelihood, Educational, Medical, Legal). (Functional Suitability)",
            "1. In Assistance Needed section, check 'Educational Assistance' and 'Medical Subsidy'.\n2. Verify requested assistance tags bind to the case file.",
            "Check 'Educational Assistance' and 'Medical Subsidy'",
            "Functional Suitability",
            "Verified: Associates welfare assistance codes with solo parent record."
        ),
        (
            "RecordDialog Step 3 / Previous Step Navigation (Return to Step 2)",
            "Verify clicking 'Previous Step' returns to Step 2 with all location, contact, and dependent rows preserved. (Usability)",
            "1. In Step 3, click '← Previous Step' button.\n2. Verify wizard returns to Step 2.\n3. Verify dependent child rows and contact details remain fully populated.",
            "Click '← Previous Step' in Step 3",
            "Usability",
            "Verified: Transitions step index to 2 and retains all Step 2 and Step 3 selections."
        ),
        (
            "RecordDialog Step 3 / Review & Save Button Action (Open ConfirmDialog)",
            "Verify clicking 'Review & Save' with valid Step 3 opens ConfirmDialog summary review modal. (Usability)",
            "1. Complete Step 3 with valid circumstance code and needs.\n2. Click 'Review & Save' button.\n3. Verify ConfirmDialog opens centered on the window with summary card.",
            "Click 'Review & Save' button",
            "Usability",
            "Verified: Launches ConfirmDialog modal displaying complete dossier summary."
        ),
        (
            "ConfirmDialog / Full Applicant Details Summary Verification",
            "Verify ConfirmDialog accurately displays applicant Name, Age, Sex, Civil Status, Address, Barangay, and Contact. (Functional Suitability)",
            "1. Inspect applicant summary card in ConfirmDialog.\n2. Verify Full Name matches 'MENDOZA, CARLOS'.\n3. Verify Age, Sex, Civil Status, Barangay, and Contact Number reflect Step 1 and Step 2 inputs.",
            "Inspect applicant summary in ConfirmDialog",
            "Functional Suitability",
            "Verified: ConfirmDialog binds to FormSnapshot and displays comprehensive personal details."
        ),
        (
            "ConfirmDialog / Family Dependents List Summary Verification",
            "Verify ConfirmDialog lists all added dependent children with names, ages, and relationships. (Functional Suitability)",
            "1. In ConfirmDialog, inspect Dependents section.\n2. Verify each child added in Step 2 is rendered with Full Name, Age, and Relationship.\n3. Verify dependent count badge displays correct total.",
            "Inspect dependents summary in ConfirmDialog",
            "Functional Suitability",
            "Verified: ConfirmDialog iterates through Dependents snapshot collection."
        ),
        (
            "ConfirmDialog / Circumstance Classification Summary Verification",
            "Verify ConfirmDialog displays selected circumstance code and statutory description. (Functional Suitability)",
            "1. In ConfirmDialog, inspect Circumstance section.\n2. Verify statutory code (e.g., 'A2 - Death of spouse') and entered remarks are displayed.",
            "Inspect circumstance summary in ConfirmDialog",
            "Functional Suitability",
            "Verified: Displays statutory classification in confirmation card."
        ),
        (
            "ConfirmDialog / Cancel Action (Return to Step 3)",
            "Verify clicking 'Cancel' in ConfirmDialog returns to Step 3 without saving or losing entered data. (Usability)",
            "1. In ConfirmDialog, click 'Cancel' (or 'Go Back').\n2. Verify ConfirmDialog closes.\n3. Verify RecordDialog re-appears at Step 3 with all entered data completely preserved.",
            "Click 'Cancel' in ConfirmDialog",
            "Usability",
            "Verified: ConfirmDialog returns false; RecordDialog remains open for further editing."
        ),
        (
            "ConfirmDialog / Confirm & Save New Record (Happy Path)",
            "Verify clicking 'Confirm & Save' commits record to database, shows success toast, and closes dialog. (Successful)",
            "1. In ConfirmDialog, click 'Confirm & Save' button.\n2. Verify loading spinner is displayed during API roundtrip.\n3. Verify API creates record (201 Created).\n4. Verify success toast notification confirms 'Record Created Successfully'.\n5. Verify ConfirmDialog and RecordDialog close and records table refreshes with new record.",
            "Click 'Confirm & Save' in ConfirmDialog",
            "Functional Suitability",
            "Verified: SoloParentApiService.CreateSoloParentAsync commits record, invalidates cache, and updates table."
        ),
        (
            "ConfirmDialog / Duplicate PhilSys Server Conflict Handling (409)",
            "Verify server 409 conflict on duplicate PhilSys returns user to Step 1 and highlights PhilSys field. (Reliability)",
            "1. Submit form where PhilSys causes server-side 409 Conflict.\n2. Verify ConfirmDialog closes and RecordDialog navigates back to Step 1.\n3. Verify ErrPhilSys displays 'PhilSys ID is already registered to an existing record.' with red border.",
            "Submit record causing server 409 duplicate conflict",
            "Reliability",
            "Verified: Catches 409 status code, navigates to Step 1, and marks PhilSys error."
        ),
        (
            "ConfirmDialog / Network Error Handling on Save",
            "Verify friendly error notice when network fails during record submission. (Reliability)",
            "1. In ConfirmDialog, click 'Confirm & Save'.\n2. Simulate network disconnect or API outage.\n3. Verify ActionConfirmDialog displays error notice without application crash.\n4. Verify dialog remains open so data is not lost.",
            "Network failure during Confirm & Save",
            "Reliability",
            "Verified: Catches network exception, shows error alert, and preserves form state."
        ),
        (
            "RecordDialog / Edit Mode Initial Presentation",
            "Verify opening RecordDialog in edit mode pre-populates existing record details across all 3 steps. (Functional Suitability)",
            "1. Click 'Edit' button on an existing solo parent row.\n2. Verify RecordDialog opens in edit mode (title reads 'Edit Solo Parent Record').\n3. Verify Step 1, Step 2, and Step 3 contain existing applicant values.",
            "Click 'Edit' on eligible record row",
            "Functional Suitability",
            "Verified: Populates fields from existing SoloParentRecord model and sets editMode = true."
        ),
        (
            "RecordDialog / Edit Mode Save Changes Flow",
            "Verify modifying fields in edit mode and confirming save updates record on API and logs diff. (Successful)",
            "1. In edit mode, change Contact Number and Address in Step 2.\n2. Advance to Step 3 and click 'Review & Save'.\n3. In ConfirmDialog, verify modified fields are highlighted in change diff.\n4. Click 'Confirm & Save'; verify toast confirms update and table reflects modified values.",
            "Edit Contact & Address -> Confirm & Save",
            "Functional Suitability",
            "Verified: SoloParentApiService.UpdateSoloParentAsync updates record and audit log records change diff."
        ),
        (
            "RecordDialog / Unsaved Changes Discard Warning on Step 3",
            "Verify attempting to close RecordDialog from Step 3 prompts Unsaved Changes warning. (Data Protection)",
            "1. From Step 3, click 'Cancel' button or press Alt+F4.\n2. Verify 'Unsaved Changes' warning prompt is displayed.\n3. Click 'No'; verify dialog remains active at Step 3.",
            "Click 'Cancel' from Step 3 -> Click 'No'",
            "Reliability",
            "Verified: Unsaved changes check prevents accidental loss of completed form."
        ),
        (
            "RecordDialog / Confirm Discard Unsaved Changes from Step 3",
            "Verify confirming discard closes RecordDialog cleanly without saving. (Data Protection)",
            "1. From Step 3, click 'Cancel'.\n2. In Unsaved Changes prompt, click 'Yes' (Discard).\n3. Verify RecordDialog closes and no changes are saved to database.",
            "Click 'Cancel' from Step 3 -> Click 'Yes'",
            "Reliability",
            "Verified: Closes dialog and returns to records list."
        ),
        (
            "RecordDialog / Dialog Shell Visibility Toggle During Save",
            "Verify RecordDialog shell is hidden while ConfirmDialog is active to prevent duplicate visual clutter. (Usability)",
            "1. Click 'Review & Save' in Step 3.\n2. Verify RecordDialog shell dims or hides cleanly behind ConfirmDialog.\n3. Click 'Cancel' in ConfirmDialog; verify RecordDialog restores focus smoothly.",
            "Inspect DialogShell during ConfirmDialog activation",
            "Usability",
            "Verified: DialogShell visibility management ensures clean modal stacking."
        ),
        (
            "RecordDialog / Audit Trail Generation on Record Creation",
            "Verify creating new solo parent creates CREATE_SOLO_PARENT audit entry. (Security)",
            "1. Complete registration and save new solo parent.\n2. Open ViewAllLogsOverlay.\n3. Verify top entry shows 'CREATE_SOLO_PARENT' with applicant name and author ID.",
            "Create new solo parent -> Inspect Audit Logs",
            "Security",
            "Verified: Server audit telemetry records record creation."
        ),
        (
            "RecordDialog / Audit Trail Generation on Record Edit",
            "Verify editing solo parent creates UPDATE_SOLO_PARENT audit entry with change details. (Security)",
            "1. Edit an existing solo parent's address.\n2. Save changes.\n3. Inspect Audit Logs Detail modal; verify changed field displays before/after values.",
            "Edit record -> Inspect Audit Logs Detail",
            "Security",
            "Verified: Server audit telemetry captures field-level change details."
        ),
        (
            "RecordDialog / Cache Invalidation Across Pages on Record Mutation",
            "Verify creating or editing a record invalidates cached Dashboard, Records, and Analytics pages. (Performance Efficiency)",
            "1. Create a new solo parent record.\n2. Navigate to Dashboard; verify KPI count increments immediately without showing stale cached data.\n3. Navigate to Analytics; verify totals reflect new record.",
            "Create record -> Navigate to Dashboard and Analytics",
            "Performance Efficiency",
            "Verified: ClearPageCache('Dashboard', 'Analytics', 'SoloParentRecords') forces fresh load."
        )
    ]
