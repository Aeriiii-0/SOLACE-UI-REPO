# -*- coding: utf-8 -*-

def get_archiving_cases():
    return [
        (
            "Record Deletion / Admin Access Check",
            "Verify Delete action button on record row is visible to Administrator and hidden for Encoder. (Security)",
            "1. Log in as an Administrator; verify 'Delete' (trash icon) button is visible on record rows.\n2. Log in as an Encoder; verify 'Delete' button is completely hidden from record rows.",
            "Admin session vs Encoder session in records table",
            "Security",
            "Verified: RecordActionToolbar collapses Delete button for non-Admin roles."
        ),
        (
            "Record Deletion / DeleteRecordDialog Presentation",
            "Verify clicking Delete opens DeleteRecordDialog displaying citizen name and deletion reason selector. (Usability)",
            "1. As Administrator, click 'Delete' on an active record row.\n2. Verify DeleteRecordDialog opens centered on MainWindow.\n3. Verify citizen name and 'Solo Parent Case File' subtitle are displayed.\n4. Verify deletion reason dropdown and remarks input are rendered.",
            "Click 'Delete' on record row (Admin session)",
            "Usability",
            "Verified: DeleteRecordDialog constructor binds citizen name and cleans raw GUID details."
        ),
        (
            "DeleteRecordDialog / Mandatory Reason Selection Validation",
            "Verify system blocks deletion and displays error when no reason is selected. (Negative / Validation)",
            "1. In DeleteRecordDialog, leave dropdown at default 'Select a reason for deletion...'.\n2. Click 'Delete Record'.\n3. Verify PnlError displays: 'Please select a reason for deletion.' and deletion is blocked.",
            "Deletion Reason: unselected, Click 'Delete Record'",
            "Functional Suitability",
            "Verified: BtnDelete_Click checks CmbReason.SelectedIndex <= 0 and calls ShowError."
        ),
        (
            "DeleteRecordDialog / Reason - Duplicate Record Entry",
            "Verify selecting 'Duplicate Record Entry' captures statutory reason and permits soft-deletion. (Functional Suitability)",
            "1. In DeleteRecordDialog, select reason = 'Duplicate Record Entry'.\n2. Click 'Delete Record'.\n3. Verify dialog confirms and passes 'Duplicate Record Entry' reason to API.",
            "Select Reason: 'Duplicate Record Entry', Click 'Delete Record'",
            "Functional Suitability",
            "Verified: Captures selected reason and sets Confirmed = true."
        ),
        (
            "DeleteRecordDialog / Reason - Loss of Solo Parent Eligibility",
            "Verify selecting 'Loss of Solo Parent Eligibility' captures reason for status forfeiture. (Functional Suitability)",
            "1. In DeleteRecordDialog, select reason = 'Loss of Solo Parent Eligibility'.\n2. Click 'Delete Record'.\n3. Verify reason is recorded for case history.",
            "Select Reason: 'Loss of Solo Parent Eligibility'",
            "Functional Suitability",
            "Verified: Maps to loss of eligibility statutory clause."
        ),
        (
            "DeleteRecordDialog / Reason - Relocated Outside Municipality",
            "Verify selecting 'Relocated Outside Municipality' captures transfer of residence reason. (Functional Suitability)",
            "1. Select reason = 'Relocated Outside Municipality'.\n2. Click 'Delete Record'.\n3. Verify reason is passed to deletion payload.",
            "Select Reason: 'Relocated Outside Municipality'",
            "Functional Suitability",
            "Verified: Captures relocation reason for municipal residency audit."
        ),
        (
            "DeleteRecordDialog / Reason - Requested by Citizen / Applicant",
            "Verify selecting 'Requested by Citizen / Applicant' captures voluntary withdrawal. (Functional Suitability)",
            "1. Select reason = 'Requested by Citizen / Applicant'.\n2. Click 'Delete Record'.\n3. Verify voluntary withdrawal reason is stored.",
            "Select Reason: 'Requested by Citizen / Applicant'",
            "Functional Suitability",
            "Verified: Captures citizen voluntary cancellation request."
        ),
        (
            "DeleteRecordDialog / Reason - Dependent Reached Legal Age (18+)",
            "Verify selecting 'Dependent Reached Legal Age (18+)' captures age maturity reason. (Functional Suitability)",
            "1. Select reason = 'Dependent Reached Legal Age (18+)'.\n2. Click 'Delete Record'.\n3. Verify reason binds to soft-delete payload.",
            "Select Reason: 'Dependent Reached Legal Age (18+)'",
            "Functional Suitability",
            "Verified: Captures dependent aging out justification."
        ),
        (
            "DeleteRecordDialog / Reason - Other Reason Remarks Validation",
            "Verify selecting 'Other Reason' requires specific details in remarks and blocks empty remarks. (Negative / Validation)",
            "1. Select reason = 'Other Reason (Specify Below)'.\n2. Leave remarks text box empty.\n3. Click 'Delete Record'.\n4. Verify PnlError displays: 'Please provide specific details in remarks for \'Other Reason\'.' and deletion is blocked.",
            "Select Reason: 'Other Reason', Remarks: \"\" (Empty), Click 'Delete Record'",
            "Functional Suitability",
            "Verified: Enforces mandatory remarks when 'Other Reason' is selected."
        ),
        (
            "DeleteRecordDialog / Reason - Other Reason with Valid Remarks",
            "Verify entering custom details for 'Other Reason' permits deletion and combines remarks. (Functional Suitability)",
            "1. Select 'Other Reason (Specify Below)'.\n2. In remarks box, enter 'Citizen transferred to national government agency program'.\n3. Click 'Delete Record'.\n4. Verify deletion proceeds with custom reason recorded.",
            "Select 'Other Reason', Remarks: 'Citizen transferred to national program', Click 'Delete Record'",
            "Functional Suitability",
            "Verified: Combines 'Other Reason' with custom remarks string."
        ),
        (
            "DeleteRecordDialog / Real-Time Error Clearance on Reason Selection",
            "Verify selecting a valid reason from the dropdown clears active error panel. (Usability)",
            "1. Trigger validation error in DeleteRecordDialog.\n2. Select any valid reason from CmbReason.\n3. Verify PnlError collapses immediately.",
            "Trigger error -> Select valid reason in CmbReason",
            "Usability",
            "Verified: CmbReason_SelectionChanged collapses PnlError and clears TxtError."
        ),
        (
            "DeleteRecordDialog / Cancel Action",
            "Verify clicking Cancel in DeleteRecordDialog aborts deletion and preserves record in active table. (Data Protection)",
            "1. In DeleteRecordDialog, click 'Cancel' button (or close '✕').\n2. Verify dialog closes, Confirmed remains false, no API call is made, and record remains in active table.",
            "Click 'Cancel' in DeleteRecordDialog",
            "Usability",
            "Verified: BtnCancel_Click sets Confirmed = false, DialogResult = false, and closes dialog."
        ),
        (
            "Record Deletion / Successful Soft-Delete to 30-Day Recycle Bin",
            "Verify confirming deletion removes record from active table, moves it to Recycle Bin, and logs audit event. (Functional Suitability)",
            "1. In DeleteRecordDialog, select reason 'Relocated Outside Municipality' and confirm.\n2. Verify API executes soft-delete.\n3. Verify toast notification confirms 'Record moved to 30-day Recycle Bin'.\n4. Verify record is removed from active table, Recycle Bin counter increments, and audit log records deletion.",
            "Confirm delete in DeleteRecordDialog",
            "Functional Suitability",
            "Verified: Moves record to 30-day grace period soft-delete recycle bin."
        ),
        (
            "Recycle Bin / Kebab Menu Navigation",
            "Verify clicking 'Deleted Records (Recycle Bin)' in toolbar Kebab menu opens RecycleBinDialog. (Usability)",
            "1. On Solo Parent Records page, click toolbar '...' Kebab menu.\n2. Click 'Deleted Records (Recycle Bin)'.\n3. Verify RecycleBinDialog opens showing soft-deleted records table.",
            "Click '...' Kebab menu -> Click 'Deleted Records (Recycle Bin)'",
            "Usability",
            "Verified: Launches RecycleBinDialog and loads soft-deleted records from ArchiveApiService."
        ),
        (
            "Recycle Bin / Soft-Deleted Record Item Display",
            "Verify Recycle Bin renders citizen name, barangay, PhilSys ID, deletion timestamp, and days remaining. (Functional Suitability)",
            "1. Inspect record cards in RecycleBinDialog.\n2. Verify Full Name, Barangay, and PhilSys No display accurately.\n3. Verify Deletion Requested timestamp and Reinstatement Deadline are rendered.\n4. Verify amber Days Remaining badge displays 'X days remaining'.",
            "Inspect RecycleList ItemTemplate in RecycleBinDialog",
            "Functional Suitability",
            "Verified: RecycleBinItemViewModel binds metadata and calculates days remaining in grace period."
        ),
        (
            "Recycle Bin / Deletion Reason Badge Display",
            "Verify Recycle Bin displays deletion reason badge for each soft-deleted record. (Visual Verification)",
            "1. In RecycleBinDialog, inspect reason badge on record card.\n2. Verify badge displays specific reason provided at deletion (e.g., 'Relocated Outside Municipality').",
            "Inspect DeletionReason badge in RecycleBinDialog",
            "Usability",
            "Verified: Displays DeletionReason pill tag."
        ),
        (
            "Recycle Bin / Search Filter by Citizen Name",
            "Verify searching by citizen name in RecycleBinDialog filters soft-deleted items. (Functional Suitability)",
            "1. In Recycle Bin search box, type a citizen name.\n2. Verify display list filters dynamically to matching names in real time.\n3. Clear search box; verify full list of soft-deleted items restores.",
            "Type name in RecycleBinDialog search box",
            "Functional Suitability",
            "Verified: ApplyFilter executes client-side search across _allItems."
        ),
        (
            "Recycle Bin / Search Filter by Barangay",
            "Verify filtering soft-deleted records by barangay in RecycleBinDialog. (Functional Suitability)",
            "1. Select specific barangay in RecycleBinDialog barangay filter.\n2. Verify only soft-deleted records from that barangay are displayed.",
            "Filter by Barangay in RecycleBinDialog",
            "Functional Suitability",
            "Verified: Filters recycle bin items by Barangay."
        ),
        (
            "Recycle Bin / Empty State Notification",
            "Verify clear empty state display when Recycle Bin contains zero deleted records. (Usability)",
            "1. Open Recycle Bin when no records are in 30-day grace period.\n2. Verify EmptyStatePanel displays: 'Recycle bin is empty. No deleted records within the 30-day grace period.'",
            "Open RecycleBinDialog with 0 deleted records",
            "Usability",
            "Verified: EmptyStatePanel.Visibility = Visible when _displayItems.Count == 0."
        ),
        (
            "Recycle Bin / Reinstate Confirmation Dialog Trigger",
            "Verify clicking 'Reinstate' action button on a soft-deleted row opens confirmation dialog. (Usability)",
            "1. In RecycleBinDialog, locate a soft-deleted record.\n2. Click the green 'Reinstate' action button.\n3. Verify ActionConfirmDialog opens asking: 'Reinstate Citizen? This will restore the solo parent case file to active standing.'",
            "Click 'Reinstate' button on RecycleBinDialog item",
            "Usability",
            "Verified: Prompts user confirmation before executing API reinstatement."
        ),
        (
            "Recycle Bin / Reinstate Cancel Action",
            "Verify clicking Cancel in Reinstate confirmation dialog preserves record in Recycle Bin. (Reliability)",
            "1. In Reinstate confirmation dialog, click 'Cancel'.\n2. Verify dialog closes, no API call is made, and record remains in Recycle Bin.",
            "Click 'Cancel' in Reinstate confirmation dialog",
            "Reliability",
            "Verified: Aborting confirmation keeps record in recycle bin."
        ),
        (
            "Recycle Bin / Successful Record Reinstatement",
            "Verify confirming reinstatement restores record to active registry, clears caches, and updates table. (Successful)",
            "1. In Reinstate confirmation dialog, click 'Reinstate Citizen'.\n2. Verify system calls ArchiveApiService.ReinstateFromRecycleBinAsync.\n3. Verify toast notification confirms 'Record Reinstated'.\n4. Verify record is removed from Recycle Bin list.\n5. Close dialog; verify record immediately re-appears in active Solo Parent Records table.",
            "Confirm 'Reinstate Citizen' in ActionConfirmDialog",
            "Functional Suitability",
            "Verified: Restores record, sets WasRecordReinstated = true, clears page caches, and refreshes records table."
        ),
        (
            "Recycle Bin / Cache Invalidation Across Pages on Reinstatement",
            "Verify reinstating a record invalidates Dashboard, Records, and Analytics cached instances. (Performance Efficiency)",
            "1. Reinstate a record from Recycle Bin.\n2. Return to Dashboard; verify KPI count increments immediately.\n3. Navigate to Analytics; verify totals reflect reinstated citizen.",
            "Reinstate record -> Verify Dashboard and Analytics KPI counts",
            "Performance Efficiency",
            "Verified: MainWindow.ClearPageCache clears 'SoloParentRecords', 'Dashboard', and 'Analytics'."
        ),
        (
            "Recycle Bin / Audit Trail Generation on Reinstatement",
            "Verify reinstating a record creates REINSTATE_SOLO_PARENT audit entry. (Security)",
            "1. Complete record reinstatement from Recycle Bin.\n2. Open ViewAllLogsOverlay.\n3. Verify audit log entry records action 'REINSTATE_SOLO_PARENT' with citizen name and staff author.",
            "Reinstate record -> Inspect Audit Logs",
            "Security",
            "Verified: Server records audit telemetry on reinstatement event."
        ),
        (
            "Recycle Bin / Server Failure Fault Tolerance",
            "Verify friendly error notice when loading Recycle Bin fails due to network outage. (Reliability)",
            "1. Disconnect network or stop API.\n2. Open Recycle Bin dialog.\n3. Verify error dialog displays friendly failure notice and application remains stable.",
            "Simulate API offline when opening RecycleBinDialog",
            "Reliability",
            "Verified: Catches exception and displays ActionConfirmDialog.Error."
        ),
        (
            "Municipal Archives / Kebab Menu Navigation",
            "Verify clicking 'Municipal Archives' in toolbar Kebab menu navigates to MunicipalArchivesPage. (Functional Suitability)",
            "1. In Solo Parent Records toolbar, click '...' Kebab menu.\n2. Click 'Municipal Archives'.\n3. Verify application navigates to MunicipalArchivesPage and TopBar title updates to 'Municipal Archives'.",
            "Click '...' Kebab menu -> Click 'Municipal Archives'",
            "Functional Suitability",
            "Verified: MainWindow navigates to MunicipalArchivesPage."
        ),
        (
            "Municipal Archives / Archived Records Table Display",
            "Verify archives table renders Name, Barangay, PhilSys No, Date Archived, Archive Reason, and Actions. (Functional Suitability)",
            "1. On MunicipalArchivesPage, observe archives table.\n2. Verify GridView columns: Citizen Name, Barangay, PhilSys Card Number, Date Archived, Archive Reason, and Actions.",
            "Inspect ArchivesList columns on MunicipalArchivesPage",
            "Functional Suitability",
            "Verified: ArchivesList GridView binds to ArchivedSoloParentRowViewModel collection."
        ),
        (
            "Municipal Archives / Search by Citizen Name",
            "Verify typing citizen name in archives search box filters archived records dynamically. (Functional Suitability)",
            "1. In Municipal Archives search box, type 'Cruz'.\n2. Verify table filters to archived records matching 'Cruz' in first or last name.",
            "SearchText: 'Cruz' on MunicipalArchivesPage",
            "Functional Suitability",
            "Verified: SearchArchivesAsync queries archives matching search term."
        ),
        (
            "Municipal Archives / Search by PhilSys Number",
            "Verify typing PhilSys number in archives search box filters to specific archived case file. (Functional Suitability)",
            "1. In search box, type PhilSys number '9876-5432-1098-7654'.\n2. Verify table filters to matching archived record.",
            "SearchText: '9876-5432-1098-7654'",
            "Functional Suitability",
            "Verified: Exact PhilSys search in archives."
        ),
        (
            "Municipal Archives / Filter by Archive Reason 'All'",
            "Verify selecting 'All' in Archive Reason dropdown displays all archived records. (Functional Suitability)",
            "1. Set Archive Reason dropdown (CmbReason) to 'All Reasons'.\n2. Verify table displays records archived due to both 5-year inactivity and expired deletion.",
            "Archive Reason: 'All Reasons'",
            "Functional Suitability",
            "Verified: Queries archives without reason filter."
        ),
        (
            "Municipal Archives / Filter by Archive Reason 'Inactive 5+ Years'",
            "Verify filtering archives by 'Inactive 5+ Years' displays only automatically aged-out records. (Functional Suitability)",
            "1. In Archive Reason dropdown, select 'Inactive 5+ Years'.\n2. Verify table filters to records with ArchiveReason == 'Inactive5Years'.",
            "Archive Reason: 'Inactive 5+ Years'",
            "Functional Suitability",
            "Verified: Filters archives where ArchiveReason == 'Inactive5Years'."
        ),
        (
            "Municipal Archives / Filter by Archive Reason 'Deletion Expired'",
            "Verify filtering archives by 'Deletion Expired' displays records whose 30-day deletion grace period passed. (Functional Suitability)",
            "1. In Archive Reason dropdown, select 'Deletion Expired'.\n2. Verify table displays records archived following 30-day deletion expiration.",
            "Archive Reason: 'Deletion Expired'",
            "Functional Suitability",
            "Verified: Filters archives where ArchiveReason == 'DeletionExpired'."
        ),
        (
            "Municipal Archives / Filter by Barangay (Administrator)",
            "Verify Administrator can filter archives across all 24 Biñan barangays. (Functional Suitability)",
            "1. As Administrator, click Barangay dropdown on MunicipalArchivesPage.\n2. Select 'San Antonio'.\n3. Verify table filters to archived records belonging only to Barangay San Antonio.",
            "Select Barangay: 'San Antonio' (Admin session)",
            "Functional Suitability",
            "Verified: Administrator queries archives across any barangay."
        ),
        (
            "Municipal Archives / Barangay Scoping Enforcement (Barangay Secretary)",
            "Verify Barangay Secretary is locked strictly to their assigned barangay in Municipal Archives. (Security)",
            "1. Sign in as Barangay Secretary assigned to 'Platero'.\n2. Navigate to Municipal Archives.\n3. Verify CmbBarangay is locked (IsEnabled = false) and selected to 'Platero'.\n4. Verify secretary cannot access archives of other barangays.",
            "Barangay Secretary session (Platero), inspect CmbBarangay",
            "Security",
            "Verified: ApplyRolePermissions locks CmbBarangay to CurrentUserBarangay."
        ),
        (
            "Municipal Archives / Scheduled Shred Date Calculation (10 Years)",
            "Verify Scheduled Shred Date is calculated accurately as Date Archived + 10 years per RA 10173. (Functional Suitability)",
            "1. Inspect an archived record with DateArchived = '2026-05-10'.\n2. Verify Scheduled Shred Date displays '2036-05-10' (exactly 10 years later).\n3. Verify compliance with National Archives and Data Privacy Act data retention disposition.",
            "Inspect ScheduledShredDate on archived record",
            "Functional Suitability",
            "Verified: ScheduledShredDate property returns DateArchived.AddYears(10)."
        ),
        (
            "Municipal Archives / Responsive Column Resizing with Scrollbar Buffer",
            "Verify table columns resize proportionally when vertical scrollbar appears or window resizes. (Usability)",
            "1. Resize window width and observe table columns.\n2. Verify fixed widths are maintained (Philsys: 160px, Date: 140px, Actions: 220px).\n3. Verify flex columns distribute remaining space (Barangay: 25%, Reason: 32%, Name: remainder) with 22px scrollbar buffer.",
            "Resize MunicipalArchivesPage window width",
            "Usability",
            "Verified: ResizeColumns() algorithm allocates proportional flex widths and reserves scrollbar margin."
        ),
        (
            "Municipal Archives / Table Rounded Corner Clipping",
            "Verify archives table grid clips smoothly to 15px rounded corner geometry. (Visual Verification)",
            "1. Inspect archives table corners on resize.\n2. Verify table edges clip neatly to RectangleGeometry with 15px radius.",
            "Inspect ArchivesTableGrid_SizeChanged clipping",
            "Usability",
            "Verified: Applies RectangleGeometry with 15px radius on SizeChanged."
        ),
        (
            "Municipal Archives / Pagination Controls",
            "Verify navigating across archive pages using Next and Previous buttons. (Functional Suitability)",
            "1. Ensure total archives exceed 20 items.\n2. Click 'Next' button; verify Page 2 loads and summary updates ('Page 2 of N').\n3. Click 'Previous' button; verify Page 1 reloads.",
            "Click 'Next' then 'Previous' pagination buttons on MunicipalArchivesPage",
            "Functional Suitability",
            "Verified: UpdatePaginationUI manages page indexing and button enabled states."
        ),
        (
            "Municipal Archives / Empty State Notification",
            "Verify clear empty state display when no archives match filter criteria. (Usability)",
            "1. Filter archives by non-existent term.\n2. Verify empty state panel is displayed: 'No archived records found matching your filters.'",
            "Filter with zero matching archives",
            "Usability",
            "Verified: Empty state UI activates when archive collection count is 0."
        ),
        (
            "Municipal Archives / Run Archival Pipeline Button Visibility (Admin)",
            "Verify 'Run Archival Pipeline' button is visible only to Administrator and hidden for other roles. (Security)",
            "1. Sign in as Administrator; verify 'Run Archival Pipeline' button is visible in header.\n2. Sign in as Encoder or Barangay Secretary; verify button is collapsed.",
            "Admin session vs non-Admin session on MunicipalArchivesPage",
            "Security",
            "Verified: ApplyRolePermissions sets BtnRunPipeline.Visibility = isAdmin ? Visible : Collapsed."
        ),
        (
            "Municipal Archives / Run Archival Pipeline Execution",
            "Verify clicking 'Run Archival Pipeline' triggers automated archiving and displays summary toast. (Functional Suitability)",
            "1. As Administrator, click 'Run Archival Pipeline' button.\n2. Verify loading indicator displays while pipeline executes.\n3. Verify system calls ArchiveApiService.RunLifecyclePipelineAsync.\n4. Verify summary toast displays: 'Archival Pipeline Completed: X records processed'.\n5. Verify archives table reloads.",
            "Click 'Run Archival Pipeline' button (Admin session)",
            "Functional Suitability",
            "Verified: Executes automated lifecycle pipeline for 30-day expiries and 5-year inactivity."
        ),
        (
            "Archived Dossier / Open Archived Dossier Dialog",
            "Verify clicking 'View Dossier' action button on an archived row opens ArchivedDossierDialog. (Usability)",
            "1. On an archived record row, click 'View Dossier' button.\n2. Verify ArchivedDossierDialog opens showing complete archived case dossier.\n3. Verify loading overlay displays during data fetch and collapses when loaded.",
            "Click 'View Dossier' button on archived row",
            "Usability",
            "Verified: OpenArchivedDossierDialog opens ArchivedDossierDialog and fetches details."
        ),
        (
            "Archived Dossier / Personal & Contact Information Display",
            "Verify ArchivedDossierDialog renders personal details, address, and contact accurately. (Functional Suitability)",
            "1. In ArchivedDossierDialog, inspect Personal Info, Contact, and Address cards.\n2. Verify Full Name, Birthdate, PhilSys No, Address, Barangay, and Mobile Number are displayed.",
            "Inspect Personal Info cards in ArchivedDossierDialog",
            "Functional Suitability",
            "Verified: PopulateUI binds personal and contact details from ArchivedSoloParentDetailDto."
        ),
        (
            "Archived Dossier / Family Dependents Table Display",
            "Verify ArchivedDossierDialog renders archived family composition table. (Functional Suitability)",
            "1. In ArchivedDossierDialog, inspect Family Dependents section.\n2. Verify dependent children are listed with Full Name, Relationship, and Age at time of archiving.",
            "Inspect Family Dependents in ArchivedDossierDialog",
            "Functional Suitability",
            "Verified: Binds archived dependents list."
        ),
        (
            "Archived Dossier / Archive Reason and Dates Display",
            "Verify ArchivedDossierDialog renders Date Archived, Archive Reason, and Scheduled Shred Date. (Functional Suitability)",
            "1. In ArchivedDossierDialog, inspect Archival Details card.\n2. Verify Date Archived, statutory Archive Reason, and 10-year Scheduled Shred Date are prominently displayed.",
            "Inspect Archival Details in ArchivedDossierDialog",
            "Functional Suitability",
            "Verified: Displays archival timeline and retention compliance data."
        ),
        (
            "Archived Dossier / Reactivate Button Visibility on Active Archive",
            "Verify 'Reactivate Citizen' button is visible for standard archived records. (Functional Suitability)",
            "1. Open ArchivedDossierDialog for a standard archived record.\n2. Verify green 'Reactivate Citizen' button is visible in dialog footer.",
            "Inspect footer buttons on standard archived dossier",
            "Functional Suitability",
            "Verified: BtnReactivateRecord is visible when record is not shredded or already reactivated."
        ),
        (
            "Archived Dossier / Reactivate Button Hidden on Shredded Record",
            "Verify 'Reactivate Citizen' button is hidden/collapsed if record has been shredded. (Security)",
            "1. Open dossier for a record where IsShredded = true.\n2. Verify 'Reactivate Citizen' button is collapsed to prevent illegal reactivation of destroyed records.",
            "Open dossier for shredded record",
            "Security",
            "Verified: Hides Reactivate button when IsShredded is true."
        ),
        (
            "Archived Dossier / Reactivate Button Hidden on Already Reactivated Record",
            "Verify 'Reactivate Citizen' button is hidden if record was already reactivated. (Security)",
            "1. Open dossier for record where IsReactivated = true.\n2. Verify 'Reactivate Citizen' button is collapsed.",
            "Open dossier for already reactivated record",
            "Security",
            "Verified: Hides Reactivate button when IsReactivated is true."
        ),
        (
            "Archived Dossier / Close Dossier Dialog via '✕' or Close Button",
            "Verify closing ArchivedDossierDialog returns cleanly to archives table. (Usability)",
            "1. In ArchivedDossierDialog, click 'Close' button or '✕'.\n2. Verify dialog closes and archives table remains displayed with focus intact.",
            "Click 'Close' on ArchivedDossierDialog",
            "Usability",
            "Verified: Closes dossier dialog."
        ),
        (
            "Citizen Reactivation / Open ReactivateCitizenDialog from Table",
            "Verify clicking 'Reactivate' on archived table row opens ReactivateCitizenDialog. (Usability)",
            "1. On an archived record row, click 'Reactivate' action button.\n2. Verify ReactivateCitizenDialog opens with citizen name pre-filled in header.\n3. Verify Employment Status, Occupation, Monthly Income, Needs, and Justification Notes inputs are displayed.",
            "Click 'Reactivate' button on archived table row",
            "Usability",
            "Verified: OpenReactivationDialog launches ReactivateCitizenDialog."
        ),
        (
            "Citizen Reactivation / Open ReactivateCitizenDialog from Dossier",
            "Verify clicking 'Reactivate Citizen' inside ArchivedDossierDialog opens ReactivateCitizenDialog. (Usability)",
            "1. In ArchivedDossierDialog, click 'Reactivate Citizen' button.\n2. Verify ReactivateCitizenDialog opens modally over dossier dialog.",
            "Click 'Reactivate Citizen' inside ArchivedDossierDialog",
            "Usability",
            "Verified: Launches ReactivateCitizenDialog with parent dossier window owner."
        ),
        (
            "ReactivateCitizenDialog / Occupation Mandatory Validation",
            "Verify inline error when Occupation / Livelihood is left blank. (Negative / Validation)",
            "1. In ReactivateCitizenDialog, leave Occupation text box empty.\n2. Fill justification notes and click 'Reactivate Citizen'.\n3. Verify error alert displays: 'Please provide the citizen\'s current occupation or source of livelihood.' and reactivation is blocked.",
            "Occupation: \"\" (Empty), Click 'Reactivate Citizen'",
            "Functional Suitability",
            "Verified: Validates string.IsNullOrWhiteSpace(occupation) and shows ActionConfirmDialog.Error."
        ),
        (
            "ReactivateCitizenDialog / Justification Notes Mandatory Validation",
            "Verify inline error when Reactivation Justification Notes is left blank. (Negative / Validation)",
            "1. In ReactivateCitizenDialog, enter Occupation 'Dressmaker'.\n2. Leave Justification Notes empty.\n3. Click 'Reactivate Citizen'.\n4. Verify error alert displays: 'Please provide a justification note for reactivating this citizen.'",
            "Justification Notes: \"\" (Empty), Click 'Reactivate Citizen'",
            "Functional Suitability",
            "Verified: Validates string.IsNullOrWhiteSpace(notes) and blocks submission."
        ),
        (
            "ReactivateCitizenDialog / Monthly Income Numeric Parsing",
            "Verify Monthly Income accepts numeric amount and parses decimal values. (Functional Suitability)",
            "1. In Monthly Income field, enter '12500'.\n2. Verify decimal parsing accepts value and binds to UpdatedEmployment.MonthlyIncome.",
            "Monthly Income: '12500'",
            "Functional Suitability",
            "Verified: decimal.TryParse converts income value."
        ),
        (
            "ReactivateCitizenDialog / Employment Status Selection",
            "Verify selecting updated employment status from dropdown (Employed, Self-Employed, Unemployed). (Functional Suitability)",
            "1. Click CmbEmploymentStatus dropdown in ReactivateCitizenDialog.\n2. Select 'Self-Employed'.\n3. Verify selection binds to reactivation request.",
            "Select 'Self-Employed' in ReactivateCitizenDialog",
            "Functional Suitability",
            "Verified: Binds selected employment status to ReactivateArchivedRecordRequest."
        ),
        (
            "ReactivateCitizenDialog / Needs and Other Income Input",
            "Verify entering updated Needs & Problems and Other Sources of Income. (Functional Suitability)",
            "1. In Needs & Problems box, enter 'Financial assistance for children schooling'.\n2. In Other Income box, enter 'Sari-sari store earnings'.\n3. Verify text inputs bind cleanly to reactivation payload.",
            "Enter Needs & Other Income in ReactivateCitizenDialog",
            "Functional Suitability",
            "Verified: Binds updated social circumstances to request DTO."
        ),
        (
            "ReactivateCitizenDialog / Cancel Action",
            "Verify clicking Cancel in ReactivateCitizenDialog aborts reactivation without modifying archive. (Usability)",
            "1. Enter partial data in ReactivateCitizenDialog.\n2. Click 'Cancel' button.\n3. Verify dialog closes and citizen remains archived.",
            "Click 'Cancel' in ReactivateCitizenDialog",
            "Usability",
            "Verified: Aborts reactivation without saving."
        ),
        (
            "ReactivateCitizenDialog / Successful Citizen Reactivation",
            "Verify submitting valid reactivation restores citizen to active registry, removes from archives, and clears caches. (Successful)",
            "1. In ReactivateCitizenDialog, enter Occupation 'VENDOR', Notes 'CITIZEN RETURNED TO MUNICIPALITY AND RE-APPLIED WITH MINOR CHILDREN', and Income '8000'.\n2. Click 'Reactivate Citizen'.\n3. Verify API executes reactivation.\n4. Verify toast confirms 'Citizen Reactivated Successfully'.\n5. Verify citizen is removed from Municipal Archives table and restored to active Solo Parent Records table.",
            "Valid reactivation inputs, Click 'Reactivate Citizen'",
            "Functional Suitability",
            "Verified: ArchiveApiService.ReactivateCitizenAsync restores record to active registry and clears page caches."
        ),
        (
            "ReactivateCitizenDialog / Audit Trail Generation on Citizen Reactivation",
            "Verify citizen reactivation generates REACTIVATE_CITIZEN audit log entry. (Security)",
            "1. Complete citizen reactivation.\n2. Open ViewAllLogsOverlay.\n3. Verify audit log records 'REACTIVATE_CITIZEN' with citizen name, staff author, and justification notes.",
            "Reactivate citizen -> Inspect Audit Logs",
            "Security",
            "Verified: Server records audit log telemetry for citizen reactivation."
        ),
        (
            "Municipal Archives / Server Failure Fault Tolerance",
            "Verify friendly error notice when archives service is offline during search or page navigation. (Reliability)",
            "1. Disconnect network or stop API.\n2. Apply filter or search on Municipal Archives.\n3. Verify friendly error panel is displayed without application crash.",
            "Simulate API offline during archives search",
            "Reliability",
            "Verified: Catches exception and renders friendly connection error notice."
        )
    ]
