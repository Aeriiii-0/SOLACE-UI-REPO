# -*- coding: utf-8 -*-

def get_records_grid_cases():
    return [
        (
            "Solo Parent Records / Table Display & Column Layout",
            "Verify records table renders SP ID, Citizen Name, Barangay, PhilSys No, Expiration Date, Status, and Actions. (Functional Suitability)",
            "1. Navigate to Solo Parent Records page.\n2. Observe records table grid.\n3. Verify columns: Case No/SP ID, Citizen Name, Barangay, PhilSys Card Number, Expiration Date, Status badge, and Action buttons.",
            "Navigate to Solo Parent Records",
            "Functional Suitability",
            "Verified: SoloParentRecordsPage renders complete grid columns with data bindings."
        ),
        (
            "Solo Parent Records / Status Badge Visual Encoding",
            "Verify records table renders color-coded badges for Active (green), Expiring Soon (amber), and Inactive (grey). (Visual Verification)",
            "1. Inspect record status column across loaded rows.\n2. Verify Active records display green badge.\n3. Verify records expiring within 30 days display amber 'Expiring Soon' badge.\n4. Verify expired records display grey 'Inactive' badge.",
            "Inspect Status badges in records table",
            "Usability",
            "Verified: StatusToBrushConverter binds status text to appropriate color palette."
        ),
        (
            "Solo Parent Records / Global Search by Name",
            "Verify searching by applicant first or last name filters records table dynamically. (Functional Suitability)",
            "1. In Search box, type 'Santos'.\n2. Press Enter or click Search button.\n3. Verify table displays only solo parents with 'Santos' in first or last name.\n4. Verify total count updates.",
            "SearchText: 'Santos', Key: [Enter]",
            "Functional Suitability",
            "Verified: Executes search query against backend SoloParentApiService."
        ),
        (
            "Solo Parent Records / Global Search by PhilSys Number",
            "Verify searching by PhilSys Card Number filters to specific registered citizen. (Functional Suitability)",
            "1. In Search box, type 16-digit PhilSys number '1234-5678-9012-3456'.\n2. Click Search.\n3. Verify table filters to the single record matching the PhilSys ID.",
            "SearchText: '1234-5678-9012-3456'",
            "Functional Suitability",
            "Verified: PhilSys exact match filters record collection."
        ),
        (
            "Solo Parent Records / Search Zero Results Empty State",
            "Verify clear empty state notification when search query matches zero records. (Usability)",
            "1. In Search box, enter non-existent name 'NonExistentCitizen999'.\n2. Click Search.\n3. Verify empty state panel displays: 'No records found matching your search criteria.'",
            "SearchText: 'NonExistentCitizen999'",
            "Usability",
            "Verified: EmptyStatePanel becomes visible when record count is 0."
        ),
        (
            "Solo Parent Records / Special Character Sanitization in Search",
            "Verify SQL injection or script syntax in search input is sanitized safely without database error. (Security)",
            "1. In Search box, enter: '\"; DROP TABLE SoloParents; -- <script>alert(1)</script>.\n2. Click Search.\n3. Verify application does not crash, input is sanitized, and zero results or safe query executes.",
            "SearchText: \"'; DROP TABLE SoloParents; -- <script>alert(1)</script>\"",
            "Security",
            "Verified: Parameterized API query sanitizes search text and guards against injection."
        ),
        (
            "Solo Parent Records / Filter by Status (Active, Inactive, Expiring)",
            "Verify filtering records by specific status via status dropdown. (Functional Suitability)",
            "1. In Status filter dropdown, select 'Active'; verify only active records display.\n2. Select 'Expiring Soon'; verify only records expiring within 30 days display.\n3. Select 'Inactive'; verify only expired records display.",
            "Toggle Status filter between 'Active', 'Expiring Soon', and 'Inactive'",
            "Functional Suitability",
            "Verified: Filters records collection by Status property."
        ),
        (
            "Solo Parent Records / Filter by Barangay (Administrator)",
            "Verify filtering records by specific barangay via Barangay dropdown. (Functional Suitability)",
            "1. As Administrator, click Barangay filter dropdown.\n2. Select 'San Vicente'.\n3. Verify table filters to records belonging only to Barangay San Vicente.",
            "Select Barangay: 'San Vicente' (Admin session)",
            "Functional Suitability",
            "Verified: Queries records filtered by barangay parameter."
        ),
        (
            "Solo Parent Records / Barangay Scoping Enforcement (Barangay Secretary)",
            "Verify Barangay Secretary view is strictly locked to their assigned barangay. (Security)",
            "1. Sign in as Barangay Secretary assigned to 'Malaban'.\n2. Open Solo Parent Records page.\n3. Verify Barangay filter is locked to 'Malaban' (disabled) and all displayed records belong to Malaban.\n4. Verify secretary cannot access records from other barangays.",
            "Barangay Secretary login (Malaban)",
            "Security",
            "Verified: Server and client enforce barangay scoping for Barangay Secretary role."
        ),
        (
            "Solo Parent Records / Clear Filters Action",
            "Verify clicking 'Clear Search' / 'Reset Filters' restores default unfiltered list. (Usability)",
            "1. Apply custom search keyword, status filter, and barangay filter.\n2. Click 'Clear Search' button.\n3. Verify search box clears, dropdowns reset to 'All', and baseline record stream restores.",
            "Click 'Clear Search' button",
            "Usability",
            "Verified: Resets filter parameters and reloads baseline page 1."
        ),
        (
            "Solo Parent Records / Encoder Search Toolbar",
            "Verify Encoder role displays dedicated Encoder Search panel with ID, First Name, Last Name, and Barangay. (Functional Suitability)",
            "1. Sign in as an Encoder.\n2. Navigate to Solo Parent Records.\n3. Verify dedicated Encoder Search panel is rendered with distinct query inputs.",
            "Encoder session, inspect search panel",
            "Functional Suitability",
            "Verified: Dedicated Encoder search UI layout binds to search filters."
        ),
        (
            "Solo Parent Records / Double-Click Row to View Record Dossier",
            "Verify double-clicking a record row opens RecordViewDialog with full citizen dossier. (Usability)",
            "1. Double-click on any record row in the table.\n2. Verify RecordViewDialog opens centered on the window.\n3. Verify complete personal details, contact info, address, dependent children list, and circumstance classification are displayed.",
            "Double-click record row",
            "Usability",
            "Verified: Row double-click event launches RecordViewDialog with citizen dossier."
        ),
        (
            "Solo Parent Records / RecordViewDialog Read-Only Enforcement",
            "Verify all citizen data in RecordViewDialog is strictly read-only. (Functional Suitability)",
            "1. In RecordViewDialog, inspect text labels and dependent tables.\n2. Verify controls cannot be directly edited.\n3. Verify close button dismisses dialog cleanly.",
            "Inspect RecordViewDialog fields",
            "Functional Suitability",
            "Verified: RecordViewDialog is designed strictly for inspection and printing."
        ),
        (
            "Solo Parent Records / RecordViewDialog Dependents Table",
            "Verify RecordViewDialog displays full table of dependent children with ages and educational attainment. (Functional Suitability)",
            "1. Open RecordViewDialog for a solo parent with 3 children.\n2. Inspect Family Composition section.\n3. Verify all 3 children appear with Name, Relationship, Birthdate, Age, and Education.",
            "Inspect Family Composition in RecordViewDialog",
            "Functional Suitability",
            "Verified: RecordViewDialog binds to dependents collection."
        ),
        (
            "Solo Parent Records / RecordViewDialog Print / Export Action",
            "Verify clicking 'Print' / 'Export Dossier' generates readable applicant document. (Usability)",
            "1. In RecordViewDialog, click 'Export / Print' button.\n2. Verify system prepares formatted document export for caseworker printing.",
            "Click 'Export / Print' in RecordViewDialog",
            "Usability",
            "Verified: Exports citizen case profile for municipal filing."
        ),
        (
            "Solo Parent Records / Open Record in Edit Mode",
            "Verify clicking 'Edit' action button on a record row opens RecordDialog pre-populated with data. (Functional Suitability)",
            "1. Locate eligible record row.\n2. Click 'Edit' pencil action button.\n3. Verify RecordDialog opens in edit mode with existing data populated in Step 1, Step 2, and Step 3.",
            "Click 'Edit' button on record row",
            "Functional Suitability",
            "Verified: Opens RecordDialog in edit mode with existing applicant details."
        ),
        (
            "Solo Parent Records / Edit Ownership Boundary Check (Encoder)",
            "Verify Encoders can only edit records they personally created and are blocked from editing records by others. (Security)",
            "1. As Encoder, locate a record where CreatedBy does NOT match the logged-in user.\n2. Click 'Edit' button on that record.\n3. Verify system blocks action with warning: 'Access Denied: You can only edit records you created.'",
            "Click 'Edit' on record where CreatedBy != CurrentUserName (Encoder session)",
            "Security",
            "Verified: Edit_Click enforces ownership check for non-Admin users."
        ),
        (
            "Solo Parent Records / Administrator Edit Override on Any Record",
            "Verify Administrator can edit any solo parent record regardless of original creator. (Security)",
            "1. Sign in as Administrator.\n2. Locate a record created by an Encoder or Barangay Secretary.\n3. Click 'Edit' button.\n4. Verify RecordDialog opens in edit mode allowing Administrator to modify and update record.",
            "Click 'Edit' on any record (Admin session)",
            "Security",
            "Verified: Administrator role bypasses creator ownership constraint."
        ),
        (
            "Solo Parent Records / Edit Record Contact and Address Update",
            "Verify modifying contact and address in edit mode saves changes and logs audit diff. (Successful)",
            "1. In RecordDialog edit mode, change Address to 'Block 5 Lot 12, Phase 2'.\n2. Change Contact Number to '0918-999-8877'.\n3. Click 'Confirm & Save'.\n4. Verify toast confirms 'Record Updated' and table row reflects new values immediately.",
            "Edit Address & Contact -> Confirm & Save",
            "Functional Suitability",
            "Verified: Updates record via API and logs before/after diff in audit log."
        ),
        (
            "Solo Parent Records / Renewal Flow - Method Selection Dialog Trigger",
            "Verify clicking 'Renew' action button on an active record opens RenewalMethodDialog. (Usability)",
            "1. Locate an active record row.\n2. Click 'Renew' action button (or Kebab menu -> 'Renew Record').\n3. Verify RenewalMethodDialog opens presenting 'Quick Renew' and 'Reassess & Renew' choices.",
            "Click 'Renew' action button on record row",
            "Usability",
            "Verified: Launches RenewalMethodDialog to choose renewal path."
        ),
        (
            "Solo Parent Records / Quick Renew Action (1-Year Validity Extension)",
            "Verify selecting 'Quick Renew' extends record validity by 12 months immediately. (Functional Suitability)",
            "1. In RenewalMethodDialog, click 'Quick Renew'.\n2. Verify system calls renewal API.\n3. Verify success toast confirms renewal.\n4. Verify Expiration Date in table advances by exactly 1 year from previous expiration.",
            "Click 'Quick Renew' in RenewalMethodDialog",
            "Functional Suitability",
            "Verified: Extends expiration by 12 months and updates status badge to Active."
        ),
        (
            "Solo Parent Records / Reassess & Renew Action (Open RenewalReassessmentDialog)",
            "Verify selecting 'Reassess & Renew' opens RenewalReassessmentDialog for updating circumstances and dependents. (Functional Suitability)",
            "1. In RenewalMethodDialog, click 'Reassess & Renew'.\n2. Verify RenewalMethodDialog closes and RenewalReassessmentDialog opens.\n3. Verify citizen name, current dependents list, employment, and circumstances are populated.",
            "Click 'Reassess & Renew' in RenewalMethodDialog",
            "Functional Suitability",
            "Verified: Opens RenewalReassessmentDialog to re-evaluate solo parent eligibility."
        ),
        (
            "RenewalReassessmentDialog / Add New Dependent Child",
            "Verify adding a new dependent child during annual renewal reassessment. (Functional Suitability)",
            "1. In RenewalReassessmentDialog, click '+ Add Dependent'.\n2. Enter new child name, birthdate, and relationship.\n3. Verify new child is tagged with 'New' status tag.",
            "Click '+ Add Dependent' in RenewalReassessmentDialog",
            "Functional Suitability",
            "Verified: DependentItemViewModel adds new dependent flagged with IsNew = true."
        ),
        (
            "RenewalReassessmentDialog / Mark Existing Dependent as Ineligible / Removed",
            "Verify marking an existing dependent child as removed/ineligible when they reach adulthood. (Functional Suitability)",
            "1. In RenewalReassessmentDialog, locate a child who turned 22 without disability.\n2. Click 'Remove / Ineligible' button on that dependent card.\n3. Verify card background dims and status tag changes to 'Removed' with strike-through or badge.\n4. Verify undo button is available.",
            "Click 'Remove / Ineligible' on dependent card in RenewalReassessmentDialog",
            "Functional Suitability",
            "Verified: Sets IsRemoved = true and updates visual styling to tag removed state."
        ),
        (
            "RenewalReassessmentDialog / Update Employment and Income",
            "Verify updating employment status and monthly income during renewal reassessment. (Functional Suitability)",
            "1. In RenewalReassessmentDialog, change Employment Status from 'Unemployed' to 'Employed'.\n2. Enter updated Monthly Income '₱18,000.00'.\n3. Verify socioeconomic profile updates.",
            "Update Employment & Monthly Income in RenewalReassessmentDialog",
            "Functional Suitability",
            "Verified: Captures updated economic parameters for ongoing subsidy eligibility."
        ),
        (
            "RenewalReassessmentDialog / Confirm Reassessment & Renew",
            "Verify submitting reassessment saves updated dependents and extends validity by 12 months. (Successful)",
            "1. Complete updates in RenewalReassessmentDialog.\n2. Click 'Save & Renew Record'.\n3. Verify API receives reassessment payload, expiration extends by 12 months, toast confirms success, and audit log records REASSESS_RENEW event.",
            "Click 'Save & Renew Record' in RenewalReassessmentDialog",
            "Functional Suitability",
            "Verified: SoloParentApiService.ReassessAndRenewAsync persists updates and renews card validity."
        ),
        (
            "RenewalReassessmentDialog / Cancel Reassessment",
            "Verify clicking Cancel in RenewalReassessmentDialog aborts reassessment without altering record. (Usability)",
            "1. Make changes in RenewalReassessmentDialog.\n2. Click 'Cancel' button.\n3. Verify dialog closes and existing record remains unchanged.",
            "Click 'Cancel' in RenewalReassessmentDialog",
            "Usability",
            "Verified: Aborts reassessment without saving."
        ),
        (
            "Solo Parent Records / Masterlist Export to CSV Action",
            "Verify clicking 'Export' opens Windows SaveFileDialog and prompts for file path. (Functional Suitability)",
            "1. On Solo Parent Records page, click 'Export Masterlist' (or 'Export CSV') button.\n2. Verify Windows SaveFileDialog opens prompting user to choose destination folder and filename (e.g., 'SoloParents_Masterlist_2026.csv').",
            "Click 'Export Masterlist' in toolbar",
            "Functional Suitability",
            "Verified: SoloParentExportService initializes SaveFileDialog with default filename."
        ),
        (
            "Solo Parent Records / Masterlist Export Cancel Handling",
            "Verify canceling SaveFileDialog cleanly aborts export without throwing exceptions. (Usability)",
            "1. Click 'Export Masterlist'.\n2. In Windows SaveFileDialog, click 'Cancel'.\n3. Verify dialog closes, export cancels, and user returns to records page without error.",
            "Click 'Cancel' in SaveFileDialog",
            "Usability",
            "Verified: Checks saveDialog.ShowDialog() != true and exits cleanly."
        ),
        (
            "Solo Parent Records / Masterlist Export File Generation & Column Verification",
            "Verify exported Masterlist file contains all required statutory columns and citizen records. (Functional Suitability)",
            "1. Confirm SaveFileDialog destination.\n2. Verify export completes and success toast displays.\n3. Open generated CSV in Excel/editor; verify columns: SP ID, Full Name, Barangay, PhilSys No, Sex, Civil Status, Contact, Dependents Count, Status, and Expiration Date.",
            "Confirm SaveFileDialog to save CSV",
            "Functional Suitability",
            "Verified: SoloParentMasterlistExportService formats and writes CSV data accurately."
        ),
        (
            "Solo Parent Records / Masterlist Export Locked File Error Handling",
            "Verify friendly error notice when export targets a file currently open and locked by Excel. (Reliability)",
            "1. Open target export file in Microsoft Excel to lock file handle.\n2. Trigger export targeting the locked file name.\n3. Verify application catches IOException and ActionConfirmDialog displays friendly 'File in Use' warning rather than crashing.",
            "Export to locked open file 'masterlist.xlsx'",
            "Reliability",
            "Verified: Catches IOException and displays ActionConfirmDialog warning."
        ),
        (
            "Solo Parent Records / Pagination First and Last Page Controls",
            "Verify clicking First and Last buttons navigates to the beginning and end of the records registry. (Usability)",
            "1. On Page 1, click 'Last' page button.\n2. Verify table jumps to final page and 'Next' and 'Last' buttons are disabled.\n3. Click 'First' page button.\n4. Verify table returns to Page 1 and 'Previous' and 'First' buttons are disabled.",
            "Click 'Last' then 'First' pagination buttons",
            "Usability",
            "Verified: Paging logic manages First and Last navigation commands."
        ),
        (
            "Solo Parent Records / Pagination Previous and Next Controls",
            "Verify clicking Next and Previous navigates page by page through the registry. (Usability)",
            "1. From Page 1, click 'Next' button; verify Page 2 loads.\n2. Click 'Previous' button; verify Page 1 reloads.\n3. Verify page summary label reflects 'Showing X–Y of Z records'.",
            "Click 'Next' then 'Previous' pagination buttons",
            "Usability",
            "Verified: Updates currentPage and queries paged DTO chunk."
        ),
        (
            "Solo Parent Records / Dynamic Page Sizing on Window Resize",
            "Verify records table recalculates visible row capacity when window is resized. (Usability)",
            "1. Maximize MainWindow.\n2. Verify table recalculates page size to fill available vertical space without unnecessary outer scrollbars.\n3. Restore window to normal size; verify page size adjusts proportionally.",
            "Resize MainWindow height",
            "Usability",
            "Verified: Dynamic page sizing algorithm optimizes row count per screen height."
        ),
        (
            "Solo Parent Records / Sort by Citizen Name (A–Z and Z–A)",
            "Verify clicking Citizen Name column header toggles ascending and descending alphabetical sorting. (Functional Suitability)",
            "1. Click 'Citizen Name' column header.\n2. Verify rows sort alphabetically (A–Z) with ascending indicator arrow.\n3. Click header again; verify rows sort descending (Z–A).",
            "Click 'Citizen Name' column header twice",
            "Functional Suitability",
            "Verified: RecordSortingHelper sorts collection by FullName."
        ),
        (
            "Solo Parent Records / Sort by Expiration Date",
            "Verify clicking Expiration Date column header sorts records by validity date. (Functional Suitability)",
            "1. Click 'Expiration Date' header.\n2. Verify rows sort by soonest expiring records first.\n3. Click again; verify furthest expiration records display first.",
            "Click 'Expiration Date' column header",
            "Functional Suitability",
            "Verified: Sorts by ExpirationDate timestamp."
        ),
        (
            "Solo Parent Records / Refresh Records Table",
            "Verify clicking Refresh button re-fetches latest records from server and updates grid. (Reliability)",
            "1. In toolbar, click '↻' Refresh button.\n2. Verify loading indicator displays briefly and records table reloads fresh data from API.",
            "Click '↻' Refresh button in toolbar",
            "Reliability",
            "Verified: Invalidates cache and calls LoadRecordsAsync()."
        ),
        (
            "Solo Parent Records / Kebab Options Menu Toggle",
            "Verify clicking '...' Kebab menu on toolbar toggles dropdown with 'Deleted Records (Recycle Bin)' and 'Municipal Archives'. (Usability)",
            "1. Click '...' Kebab menu button in toolbar.\n2. Verify dropdown menu opens showing: 'Deleted Records (Recycle Bin)' and 'Municipal Archives'.\n3. Click outside; verify menu closes.",
            "Click toolbar Kebab menu button '...'",
            "Usability",
            "Verified: Toggles contextual dropdown menu for archival features."
        ),
        (
            "Solo Parent Records / Server Failure Fault Tolerance",
            "Verify friendly error panel and retry option when records server is unreachable. (Reliability)",
            "1. Disconnect network or stop API.\n2. Navigate to Solo Parent Records.\n3. Verify error message panel appears: 'Could not connect to server' with Retry button.",
            "API offline, Open Solo Parent Records",
            "Reliability",
            "Verified: Catches connection failure and offers clean retry mechanism."
        ),
        (
            "Solo Parent Records / Non-Admin Access Restriction to Kebab Deletion Actions",
            "Verify non-Admin users cannot trigger administrative archival actions from Kebab menu. (Security)",
            "1. Sign in as an Encoder.\n2. Inspect Kebab menu options.\n3. Verify administrative actions are hidden or restricted.",
            "Encoder session, inspect Kebab menu",
            "Security",
            "Verified: Restricts archival controls to Administrator role."
        )
    ]
