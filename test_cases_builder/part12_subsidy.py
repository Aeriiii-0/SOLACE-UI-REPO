# -*- coding: utf-8 -*-

def get_subsidy_cases():
    return [
        (
            "Subsidy Recommendation / Role Access Enforcement",
            "Verify Administrator accesses Subsidy Recommendation and non-Admin roles are blocked. (Security)",
            "1. Sign in as Administrator; click 'Subsidy Recommendation' in sidebar; verify page loads.\n2. Sign in as Encoder; verify tab is collapsed.\n3. Sign in as Barangay Secretary; verify tab is collapsed.",
            "Admin session vs Encoder / Secretary session",
            "Security",
            "Verified: Enforces Administrator role check for ML Subsidy Recommendation module."
        ),
        (
            "Subsidy Recommendation / Cycle Header Metrics Card",
            "Verify header displays Allocated Slots, Remaining Slots, Grant Per Recipient, Total Budget, and Disqualified count. (Functional Suitability)",
            "1. On Subsidy Recommendation page, inspect top header cards.\n2. Verify 'ALLOCATED SLOTS' and 'REMAINING SLOTS' render with progress bar.\n3. Verify 'GRANT PER RECIPIENT' (e.g. ₱1,000.00) and calculated 'TOTAL BUDGET' display accurately.\n4. Verify 'DISQUALIFIED' candidate tally displays current count.",
            "Inspect SubsidyCycleControlBar header metrics",
            "Functional Suitability",
            "Verified: SubsidyCycleControlBar binds to active cycle properties from SubsidyRecommendationViewModel."
        ),
        (
            "Subsidy Recommendation / Edit Cycle Settings Dialog Trigger",
            "Verify clicking 'Edit Cycle Settings' in cycle menu opens EditCycleDialog. (Usability)",
            "1. In top-right cycle header card, click '...' cycle options menu.\n2. Click 'Edit Cycle Settings'.\n3. Verify EditCycleDialog opens modally showing Allocated Slots and Per Grantee Amount fields.",
            "Click Cycle Menu '...' -> Click 'Edit Cycle Settings'",
            "Usability",
            "Verified: EditCycleCommand opens EditCycleDialog."
        ),
        (
            "Subsidy Recommendation / Edit Cycle Settings Budget Auto-Calculation",
            "Verify updating Allocated Slots or Grant Amount recalculates Total Budget in real time. (Functional Suitability)",
            "1. In EditCycleDialog, change Allocated Slots to '80'.\n2. Change Grant Per Recipient to '1500'.\n3. Verify Total Budget auto-calculates to '₱120,000.00' (80 * 1500).",
            "Slots: 80, Grant Amount: 1500 in EditCycleDialog",
            "Functional Suitability",
            "Verified: Real-time multiplication calculates total cycle financial allocation."
        ),
        (
            "Subsidy Recommendation / Edit Cycle Settings Save Action",
            "Verify saving modified cycle settings updates header metrics and progress bar immediately. (Functional Suitability)",
            "1. In EditCycleDialog, click 'Save' button.\n2. Verify dialog closes and toast confirms 'Cycle Settings Updated'.\n3. Verify Allocated Slots, Remaining Slots, and Total Budget in header update immediately.",
            "Click 'Save' in EditCycleDialog",
            "Functional Suitability",
            "Verified: Persists updated quota and grant parameters via API."
        ),
        (
            "Subsidy Recommendation / View Cycle Details Modal (ModalCycleDetails)",
            "Verify clicking 'View Cycle Details' opens ModalCycleDetails with full metadata and dark overlay. (Functional Suitability)",
            "1. Click '...' cycle options menu -> 'View Cycle Details'.\n2. Verify ModalCycleDetails opens over dark backdrop.\n3. Verify Cycle Name, Status (ACTIVE/FINALIZED), Fiscal Year, Semester, Slots, Grant Amount, Confirmed Grantees, and Disqualified Count are rendered.\n4. Click 'Close' to dismiss.",
            "Click Cycle Menu '...' -> Click 'View Cycle Details'",
            "Functional Suitability",
            "Verified: ModalCycleDetails mounts in GlobalModalHost displaying comprehensive cycle dossier."
        ),
        (
            "Subsidy Recommendation / ML Triage Reassessment ('Reassess Records')",
            "Verify clicking 'Reassess Records' runs batch ML triage evaluation and ranks candidates. (Functional Suitability)",
            "1. On 'Recommendations Queue' tab, click 'Reassess Records' (RunMLTriageCommand).\n2. Verify loading indicator runs while ML model assesses solo parent registry.\n3. Verify toast confirms processing.\n4. Verify queue populates with candidates sorted descending by priority score.",
            "Click 'Reassess Records' button",
            "Functional Suitability",
            "Verified: RunMLTriageCommand evaluates vulnerability metrics and sorts by ML priority score."
        ),
        (
            "Subsidy Recommendation / Recommendations Queue Candidate Card Display",
            "Verify queue renders candidate name, SP ID, barangay, ML priority score, and priority badge. (Functional Suitability)",
            "1. Inspect candidate rows in Recommendations Queue.\n2. Verify Applicant Full Name, SP ID, and Barangay display.\n3. Verify ML score percentage (e.g. '88.5%') and Priority badge ('HIGH', 'MEDIUM', 'LOW') render with color coding.",
            "Inspect RecommendationQueueGrid rows",
            "Functional Suitability",
            "Verified: DataTemplate binds SubsidyItem attributes and priority tags."
        ),
        (
            "Subsidy Recommendation / Batch Select Mode Toggle",
            "Verify clicking 'Batch Select' reveals row checkboxes and batch action ribbon. (Usability)",
            "1. On Recommendations Queue tab, click 'Batch Select' button.\n2. Verify checkbox column appears on candidate rows.\n3. Verify batch action bar appears with selection counter and bulk action buttons.",
            "Click 'Batch Select' button (ToggleBatchModeCommand)",
            "Usability",
            "Verified: IsBatchMode toggles checkbox visibility and batch actions bar."
        ),
        (
            "Subsidy Recommendation / Select Top N Candidates",
            "Verify clicking 'Select Top N' selects top candidates matching remaining quota slots. (Functional Suitability)",
            "1. In Batch Select mode, click 'Select Top N' button.\n2. Verify the top candidates matching available remaining slots are checked automatically.\n3. Verify selection counter displays 'N Selected'.",
            "Click 'Select Top N' button (SelectTopNCommand)",
            "Functional Suitability",
            "Verified: SelectTopN() marks top N items in queue as IsSelected = true."
        ),
        (
            "Subsidy Recommendation / Shortlist Selected Candidates in Bulk",
            "Verify clicking 'Shortlist Selected' moves checked candidates to Approved Selection tab. (Functional Suitability)",
            "1. Select multiple candidate checkboxes in Recommendations Queue.\n2. Click 'Shortlist Selected' button.\n3. Verify selected candidates are moved to 'Approved Selection & Audit' (Tab 1).\n4. Verify toast notification confirms shortlisting and queue removes moved rows.",
            "Check multiple candidates -> Click 'Shortlist Selected'",
            "Functional Suitability",
            "Verified: ShortlistSelectedCommand moves items to Shortlist collection."
        ),
        (
            "Subsidy Recommendation / Quick Reject Candidate Row",
            "Verify clicking 'Reject' on a queue row opens ModalOutlierRejection dialog. (Usability)",
            "1. On a candidate row in Recommendations Queue, click red 'Reject' button.\n2. Verify ModalOutlierRejection opens showing candidate name and ID.\n3. Verify Disqualification Reason selector and Audit Notes box appear.",
            "Click 'Reject' button on candidate row",
            "Usability",
            "Verified: QuickRejectRowCommand opens ModalOutlierRejection."
        ),
        (
            "Subsidy Recommendation / Disqualify Candidate with Mandatory Reason",
            "Verify selecting reason and confirming in ModalOutlierRejection disqualifies candidate and logs audit trail. (Functional Suitability)",
            "1. In ModalOutlierRejection, select reason = 'Duplicate / Existing Government Grantee'.\n2. Enter audit remarks: 'Already beneficiary of DSWD 4Ps cash grant'.\n3. Click 'Confirm Disqualification'.\n4. Verify candidate is removed from queue, DISQUALIFIED count increments, and audit log records event.",
            "Select Reason & Remarks, Click 'Confirm Disqualification'",
            "Functional Suitability",
            "Verified: ConfirmOutlierRejectionAsync persists disqualification and increments disqualified count."
        ),
        (
            "Subsidy Recommendation / Cancel Outlier Rejection Modal",
            "Verify clicking Cancel in ModalOutlierRejection aborts disqualification without removing candidate. (Usability)",
            "1. In ModalOutlierRejection, click 'Cancel' (or close button).\n2. Verify modal dismisses and candidate remains in Recommendations Queue.",
            "Click 'Cancel' in ModalOutlierRejection",
            "Usability",
            "Verified: CloseOutlierModalCommand resets modal target and closes overlay."
        ),
        (
            "Subsidy Recommendation / Explainability Drawer Trigger on Double-Click",
            "Verify double-clicking a candidate row in queue or shortlist opens SubsidyExplainDrawer. (Usability)",
            "1. Double-click on any candidate row.\n2. Verify SubsidyExplainDrawer slides out from the right side of MainWindow.\n3. Verify candidate name, SP ID, barangay, and priority score display in drawer header.",
            "Double-click candidate row",
            "Usability",
            "Verified: OpenExplainDrawerCommand sets ActiveCandidate and opens SubsidyExplainDrawer."
        ),
        (
            "Subsidy Recommendation / Explainability Drawer Vulnerability Breakdown",
            "Verify drawer renders Economic Strain, Dependency Burden, Care Burden, and Needs & Problems breakdown. (Functional Suitability)",
            "1. In SubsidyExplainDrawer, inspect vulnerability feature cards.\n2. Verify Economic Strain bar and monthly income ratio display.\n3. Verify Dependency Burden shows number of minor children and ratio.\n4. Verify Care Burden displays special needs flags and infant presence.\n5. Verify Needs & Problems text from applicant case file displays.",
            "Inspect vulnerability breakdown in SubsidyExplainDrawer",
            "Functional Suitability",
            "Verified: SubsidyExplainDrawer renders ML feature importance and case attributes."
        ),
        (
            "Subsidy Recommendation / Explainability Drawer Live ML Re-assessment",
            "Verify clicking 'Re-assess' in Explainability Drawer runs live scoring and activates 30s cooldown timer. (Functional Suitability)",
            "1. In SubsidyExplainDrawer, click 'Re-assess' button.\n2. Verify live ML model re-calculates score.\n3. Verify candidate score updates in real time.\n4. Verify button activates a 30-second cooldown timer ('Re-assess (30s)') to prevent API rate-limit spam.",
            "Click 'Re-assess' button in SubsidyExplainDrawer",
            "Functional Suitability",
            "Verified: QuickPredictCandidateCommand triggers live model scoring and starts 30s cooldown DispatcherTimer."
        ),
        (
            "Subsidy Recommendation / Explainability Drawer View Full Record Action",
            "Verify clicking 'View Full Record' in drawer opens RecordViewDialog with complete applicant dossier. (Usability)",
            "1. In SubsidyExplainDrawer footer, click 'View Full Record' button.\n2. Verify system fetches complete profile and opens RecordViewDialog.\n3. Close RecordViewDialog; verify Subsidy page and drawer states are preserved.",
            "Click 'View Full Record' in SubsidyExplainDrawer",
            "Usability",
            "Verified: ViewFullRecordCommand opens RecordViewDialog cleanly."
        ),
        (
            "Subsidy Recommendation / Close Explainability Drawer",
            "Verify clicking '✕' in drawer header closes the slide-out panel smoothly. (Usability)",
            "1. In SubsidyExplainDrawer, click '✕' close button.\n2. Verify drawer slides closed (IsDrawerOpen = false) and table interaction restores.",
            "Click '✕' close button on SubsidyExplainDrawer",
            "Usability",
            "Verified: CloseExplainDrawerCommand sets IsDrawerOpen = false."
        ),
        (
            "Subsidy Recommendation / Approved Selection Tab Navigation",
            "Verify clicking 'Approved Selection & Audit' tab displays shortlisted candidates table. (Functional Suitability)",
            "1. Click Tab 1 ('Approved Selection & Audit').\n2. Verify shortlisted candidates table (ShortlistAuditGrid) loads.\n3. Verify search box, cross-check filters, and export buttons are available.",
            "Click Tab 1 'Approved Selection & Audit'",
            "Functional Suitability",
            "Verified: SwitchTabCommand switches to Tab 1 and loads ShortlistAuditGrid."
        ),
        (
            "Subsidy Recommendation / Search Shortlist Candidates",
            "Verify searching by applicant name in shortlist filters table dynamically. (Functional Suitability)",
            "1. In shortlist search box, type candidate name.\n2. Verify table filters in real time to matching shortlist candidates.\n3. Clear search box; verify full shortlist restores.",
            "SearchText: candidate name on Tab 1",
            "Functional Suitability",
            "Verified: Filters ShortlistRows collection by search keyword."
        ),
        (
            "Subsidy Recommendation / Cross-Check Filter - City Educ (17–24y)",
            "Verify filtering shortlist by 'City Educ Check (17–24y)' shows candidates with college-age dependents. (Functional Suitability)",
            "1. In Filter dropdown, select 'City Educ Check (17–24y)'.\n2. Verify table filters to show only candidates with dependents aged 17–24.\n3. Verify amber 'City Educ Check' notice badge displays on rows.",
            "Select Filter: 'City Educ Check (17–24y)'",
            "Functional Suitability",
            "Verified: Filters candidates with student dependents aged 17–24 for scholarship de-duplication."
        ),
        (
            "Subsidy Recommendation / Cross-Check Filter - Cleared (Standard)",
            "Verify filtering shortlist by 'Cleared (Standard)' displays applicants without student overlaps. (Functional Suitability)",
            "1. In Filter dropdown, select 'Cleared (Standard)'.\n2. Verify table filters to show applicants with no overlapping college scholarship recipients.",
            "Select Filter: 'Cleared (Standard)'",
            "Functional Suitability",
            "Verified: Filters cleared candidates ready for grant disbursement."
        ),
        (
            "Subsidy Recommendation / Confirm Single Recipient Action",
            "Verify clicking green 'Confirm' button on shortlist row confirms candidate as grantee. (Functional Suitability)",
            "1. On an unconfirmed candidate row in shortlist, click 'Confirm' button.\n2. Verify button toggles to 'Confirmed' (green border/text).\n3. Verify status pill changes to 'Confirmed' (green).\n4. Verify 'Confirmed Grantees' counter in top header increments by 1.",
            "Click 'Confirm' button on candidate row",
            "Functional Suitability",
            "Verified: ConfirmGranteeCommand sets IsFinalGrantee = true and increments confirmed counter."
        ),
        (
            "Subsidy Recommendation / Unconfirm Confirmed Recipient Action",
            "Verify clicking 'Confirmed' button on a confirmed row reverts candidate to unconfirmed shortlist status. (Functional Suitability)",
            "1. On a confirmed row, click 'Confirmed' button.\n2. Verify candidate reverts to unconfirmed status.\n3. Verify 'Confirmed Grantees' counter in header decrements by 1.",
            "Click 'Confirmed' button to unconfirm",
            "Functional Suitability",
            "Verified: Toggles IsFinalGrantee = false and updates counter."
        ),
        (
            "Subsidy Recommendation / Batch Confirm Shortlisted Candidates",
            "Verify selecting multiple checkboxes and clicking 'Confirm Selected' confirms candidates in bulk. (Functional Suitability)",
            "1. Check multiple candidate rows in shortlist.\n2. Click 'Confirm Selected' button.\n3. Verify all selected candidates toggle to Confirmed status.\n4. Verify Confirmed Grantees counter and progress bar update to reflect new total.",
            "Check multiple candidates -> Click 'Confirm Selected'",
            "Functional Suitability",
            "Verified: BatchConfirmShortlistCommand updates selected candidates via API."
        ),
        (
            "Subsidy Recommendation / Revoke Candidate from Shortlist Trigger",
            "Verify clicking 'Revoke' on shortlist row opens revocation modal with mandatory audit reason. (Reliability)",
            "1. On shortlist row, click 'Revoke' button (or select multiple and click 'Revoke Selected').\n2. Verify modal appears with title 'Revoke Candidate'.\n3. Verify Disqualification Reason dropdown and remarks box appear.",
            "Click 'Revoke' button on shortlist row",
            "Reliability",
            "Verified: RevokeShortlistCandidateCommand prompts revocation modal."
        ),
        (
            "Subsidy Recommendation / Confirm Candidate Revocation with Audit Justification",
            "Verify selecting reason and confirming revocation removes candidate, frees slot, and updates audit log. (Reliability)",
            "1. In revocation modal, select reason = 'City Educ Scholarship Overlap / Conflict'.\n2. Enter audit remarks: 'Awarded full city college scholarship grant'.\n3. Click 'Confirm Revocation'.\n4. Verify candidate is removed from shortlist, quota slot is freed, toast confirms revocation, and audit trail logs event.",
            "Select Reason: 'City Educ Scholarship Overlap', Enter remarks, Click 'Confirm Revocation'",
            "Reliability",
            "Verified: Revokes candidate, restores quota slot, and logs audit record."
        ),
        (
            "Subsidy Recommendation / Export City Educ Scholarship Roster CSV",
            "Verify exporting City Educ cross-check roster generates CSV with college-age dependents. (Functional Suitability)",
            "1. In Tab 1, click 'Export Rosters ▾' dropdown.\n2. Click 'City Educ Roster (17–24 yrs)'.\n3. In SaveFileDialog, specify destination and save.\n4. Verify exported CSV contains columns: No., Name of Solo Parent, Name of Dependent, Age, Education.",
            "Click 'Export Rosters' -> Select 'City Educ Roster (17–24 yrs)'",
            "Functional Suitability",
            "Verified: ExportCityEducCommand formats and writes cross-check roster CSV."
        ),
        (
            "Subsidy Recommendation / Export Pantawid 4Ps Beneficiaries CSV",
            "Verify exporting 4Ps Beneficiaries List generates CSV of overlapping 4Ps recipients. (Functional Suitability)",
            "1. In Tab 1, click 'Export Rosters ▾' -> '4Ps Beneficiaries List'.\n2. Specify file destination and save.\n3. Verify exported CSV contains 4Ps recipients with SP ID, Barangay, Income, and Status.",
            "Click 'Export Rosters' -> Select '4Ps Beneficiaries List'",
            "Functional Suitability",
            "Verified: ExportPantawid4PsCommand exports 4Ps overlap CSV."
        ),
        (
            "Subsidy Recommendation / Final Grantees Tab Navigation",
            "Verify clicking 'Final Grantees & Archive' tab displays confirmed grantee disbursement ledger. (Functional Suitability)",
            "1. Click Tab 2 ('Final Grantees & Archive').\n2. Verify final disbursement table (FinalGranteesArchiveGrid) loads showing all confirmed recipients.",
            "Click Tab 2 'Final Grantees & Archive'",
            "Functional Suitability",
            "Verified: SwitchTabCommand switches to Tab 2 and renders FinalGranteesArchiveGrid."
        ),
        (
            "Subsidy Recommendation / Finalize & Lock Roster Action",
            "Verify clicking 'Finalize & Lock' locks roster against further edits and updates status badge. (Security)",
            "1. On Tab 2, ensure confirmed grantees exist.\n2. Click 'Finalize & Lock' button.\n3. Verify ActionConfirmDialog asks for confirmation.\n4. Click 'Confirm'; verify Roster Status badge turns green ('Finalized & Locked').\n5. Verify action button toggles to 'Unlock Roster' and row revoke buttons disable.",
            "Click 'Finalize & Lock' -> Confirm in dialog",
            "Security",
            "Verified: ToggleRosterLockCommand sets IsRosterLocked = true and disables candidate mutation actions."
        ),
        (
            "Subsidy Recommendation / Unlock Roster Action (Admin)",
            "Verify Administrator can unlock a finalized roster to perform authorized adjustments. (Security)",
            "1. On a locked roster, click 'Unlock Roster' button.\n2. Confirm unlock in dialog.\n3. Verify Roster Status returns to 'Open for Review' and editing controls re-enable.",
            "Click 'Unlock Roster' -> Confirm",
            "Security",
            "Verified: Unlocks roster and restores modification permissions."
        ),
        (
            "Subsidy Recommendation / Historical Fiscal Year Selector Auto-Navigation",
            "Verify selecting past fiscal year auto-switches to Tab 2 and locks pipeline tabs. (Security & Usability)",
            "1. Open 'FISCAL YEAR' dropdown and select a previous fiscal year (e.g., FY 2025).\n2. Verify view automatically navigates to 'Final Grantees & Archive' (Tab 2).\n3. Verify 'Recommendations Queue' (Tab 0) and 'Approved Selection' (Tab 1) are dimmed (35% opacity) and disabled with explanatory tooltip.\n4. Verify batch and mutation buttons are hidden.",
            "Select historical Fiscal Year in FiscalYearSelector ComboBox",
            "Security",
            "Verified: CanAccessActivePipelineTabs = IsActiveCycle; auto-switches to Tab 2 and blocks historical mutation."
        ),
        (
            "Subsidy Recommendation / Historical Fiscal Year Pipeline Tab Click Restriction",
            "Verify clicking disabled pipeline tabs in historical fiscal year displays explanatory info toast. (Usability)",
            "1. While viewing historical fiscal year, attempt clicking on Tab 0 or Tab 1.\n2. Verify tab transition is blocked and toast displays: 'Candidate triage and audit pipeline are only accessible for the operational fiscal cycle.'",
            "Click Tab 0 while viewing historical fiscal year",
            "Usability",
            "Verified: SwitchTabCommand guards active tabs for historical cycles."
        ),
        (
            "Subsidy Recommendation / Launch New Fiscal Year Cycle (Start New Cycle)",
            "Verify clicking '+ Start New Cycle' opens NewCycleDialog and launches next fiscal cycle. (Lifecycle)",
            "1. On Tab 2, click '+ Start New Cycle' button.\n2. Verify NewCycleDialog opens suggesting next fiscal year (e.g. FY 2027).\n3. Enter Target Beneficiary Quota (120) and Monthly Allowance (₱1,000); verify total budget auto-calculates (₱120,000.00).\n4. Click 'Launch Cycle'.\n5. Verify current cycle archives, new cycle initializes, and status resets to 'Open for Review'.",
            "Click '+ Start New Cycle' -> Quota: 120, Allowance: 1000, Click 'Launch Cycle'",
            "Functional Suitability",
            "Verified: StartNewCycleAsync archives current cycle and boots fresh fiscal year."
        ),
        (
            "Subsidy Recommendation / Export Final Grantee Ledger CSV",
            "Verify clicking 'Export Final Ledger' generates disbursement ledger CSV. (Functional Suitability)",
            "1. On Tab 2, click 'Export Final Ledger' button.\n2. In SaveFileDialog, specify destination and save.\n3. Verify exported CSV contains: No., Full Name, Contact Number, Barangay, Address, Birthday, Status, and Grant Amount ('₱1,000.00').",
            "Click 'Export Final Ledger' on Tab 2",
            "Functional Suitability",
            "Verified: ExportFinalLedgerCommand formats and writes disbursement ledger CSV."
        ),
        (
            "Subsidy Recommendation / Open Disqualified Records Modal",
            "Verify clicking 'View Disqualified Records' in cycle menu opens ModalDisqualifiedCandidates. (Usability)",
            "1. Click '...' cycle menu -> 'View Disqualified Records'.\n2. Verify ModalDisqualifiedCandidates renders over a 100% full-window dark overlay covering TitleBar, TopBar, Sidebar, and ContentArea.\n3. Verify table lists disqualified candidates with reasons.",
            "Click Cycle Menu '...' -> Click 'View Disqualified Records'",
            "Usability",
            "Verified: ModalDisqualifiedCandidates mounts in MainWindow.GlobalModalHost (Panel.ZIndex=998)."
        ),
        (
            "Subsidy Recommendation / Disqualified Modal Filter by Reason",
            "Verify filtering disqualified candidates by specific disqualification reason. (Functional Suitability)",
            "1. In ModalDisqualifiedCandidates, click Reason filter dropdown.\n2. Select 'Duplicate / Existing Government Grantee'.\n3. Verify list filters to candidates disqualified for that specific reason.",
            "Filter by Reason in ModalDisqualifiedCandidates",
            "Functional Suitability",
            "Verified: ApplyDisqualifiedReasonFilterCommand filters disqualified collection."
        ),
        (
            "Subsidy Recommendation / Disqualified Modal Pagination",
            "Verify navigating through pages of disqualified candidates. (Functional Suitability)",
            "1. In ModalDisqualifiedCandidates, click 'Next' page button; verify Page 2 loads.\n2. Click 'Previous' button; verify Page 1 reloads.",
            "Click Next and Previous in ModalDisqualifiedCandidates",
            "Functional Suitability",
            "Verified: DisqualifiedNextPageCommand and DisqualifiedPrevPageCommand manage pagination."
        ),
        (
            "Subsidy Recommendation / Reinstate Disqualified Candidate",
            "Verify clicking 'Reinstate' on a disqualified candidate row restores candidate to Recommendations Queue. (Functional Suitability)",
            "1. In ModalDisqualifiedCandidates, locate a candidate row.\n2. Click 'Reinstate' action button.\n3. Verify candidate is removed from disqualified list and returns to active Recommendations Queue.\n4. Verify DISQUALIFIED counter in top header decrements by 1.",
            "Click 'Reinstate' in ModalDisqualifiedCandidates",
            "Functional Suitability",
            "Verified: ReinstateCandidateCommand clears disqualification and restores candidate to triage queue."
        ),
        (
            "Subsidy Recommendation / Export Disqualified Candidates CSV",
            "Verify clicking 'Export CSV' in disqualified modal exports list of disqualified applicants. (Functional Suitability)",
            "1. In ModalDisqualifiedCandidates, click 'Export CSV' button.\n2. Save file in SaveFileDialog.\n3. Verify exported CSV contains disqualified candidates with name, barangay, disqualification reason, and audit remarks.",
            "Click 'Export CSV' in ModalDisqualifiedCandidates",
            "Functional Suitability",
            "Verified: ExportDisqualifiedCsvCommand writes disqualified candidates CSV."
        ),
        (
            "Subsidy Recommendation / Close Disqualified Modal via '✕' or Backdrop",
            "Verify closing ModalDisqualifiedCandidates removes full-window overlay. (Usability)",
            "1. In ModalDisqualifiedCandidates, click 'Close' button or outer backdrop.\n2. Verify modal closes, full-window dark overlay lifts, and Subsidy page interaction restores.",
            "Click 'Close' in ModalDisqualifiedCandidates",
            "Usability",
            "Verified: CloseDisqualifiedModalCommand collapses modal and clears GlobalModalHost."
        ),
        (
            "Subsidy Recommendation / Network Error Handling on Batch Triage",
            "Verify friendly error notice when network disconnects during ML batch triage. (Reliability)",
            "1. Disconnect network or stop API.\n2. Click 'Reassess Records'.\n3. Verify error notification is displayed without UI crash and button re-enables.",
            "Simulate network failure during Reassess Records",
            "Reliability",
            "Verified: Catches exception, shows error alert, and resets IsLoading flag."
        ),
        (
            "Subsidy Recommendation / Audit Logging on All Cycle Modifications",
            "Verify cycle setting updates, candidate shortlisting, confirmations, and revocations generate audit logs. (Security)",
            "1. Execute subsidy workflow actions (edit settings, confirm grantees, revoke candidate).\n2. Inspect ViewAllLogsOverlay.\n3. Verify corresponding SUBSIDY audit records are logged with author and timestamp.",
            "Execute subsidy actions -> Inspect Audit Logs",
            "Security",
            "Verified: Server records audit telemetry on all subsidy cycle mutations."
        )
    ]
