# -*- coding: utf-8 -*-

def get_audit_cases():
    return [
        (
            "Audit Logs / Initial Retrieval (Administrator)",
            "Verify Administrator retrieves system-wide audit logs with correct header and metadata. (Functional Suitability)",
            "1. Log in as an Administrator and navigate to Dashboard.\n2. Click 'View all logs' button in Recent Activity panel.\n3. Verify overlay title displays 'All Audit Logs'.\n4. Verify logs from all staff members across all barangays load in chronological order.",
            "Administrator session, Click 'View all logs'",
            "Functional Suitability",
            "Verified: ViewAllLogsOverlay sets HeaderTitle = 'All Audit Logs' and loads system-wide audit stream."
        ),
        (
            "Audit Logs / Role Scoping (Encoder)",
            "Verify Encoder audit logs view is strictly scoped to the user's own activity. (Security)",
            "1. Log in as an Encoder and open 'View all logs'.\n2. Verify overlay title displays 'My Audit Logs'.\n3. Verify every listed log entry is attributed to the logged-in encoder's user ID.\n4. Verify no activity from other encoders or administrators is displayed.",
            "Encoder session, Open ViewAllLogsOverlay",
            "Security",
            "Verified: ViewAllLogsOverlay sets HeaderTitle = 'My Audit Logs' and scopes query by logged-in UserId."
        ),
        (
            "Audit Logs / Action Filter (CREATE_SOLO_PARENT)",
            "Verify filtering audit logs by CREATE_SOLO_PARENT action type via dropdown. (Functional Suitability)",
            "1. In ViewAllLogsOverlay, click Action filter dropdown.\n2. Select 'CREATE_SOLO_PARENT'.\n3. Click 'Search' button.\n4. Verify only Solo Parent creation log events appear in the list.",
            "Action filter: 'CREATE_SOLO_PARENT', Click 'Search'",
            "Functional Suitability",
            "Verified: Filters log collection to action == 'CREATE_SOLO_PARENT'."
        ),
        (
            "Audit Logs / Action Filter (UPDATE_SOLO_PARENT)",
            "Verify filtering audit logs by UPDATE_SOLO_PARENT action type. (Functional Suitability)",
            "1. In ViewAllLogsOverlay, select Action = 'UPDATE_SOLO_PARENT'.\n2. Click 'Search'.\n3. Verify only record update events appear in the list.",
            "Action filter: 'UPDATE_SOLO_PARENT', Click 'Search'",
            "Functional Suitability",
            "Verified: Filters log stream to update events."
        ),
        (
            "Audit Logs / Action Filter (DELETE_SOLO_PARENT)",
            "Verify filtering audit logs by DELETE_SOLO_PARENT action type. (Functional Suitability)",
            "1. In ViewAllLogsOverlay, select Action = 'DELETE_SOLO_PARENT'.\n2. Click 'Search'.\n3. Verify only record deletion events appear in the list.",
            "Action filter: 'DELETE_SOLO_PARENT', Click 'Search'",
            "Functional Suitability",
            "Verified: Displays deletion log events with red action dots."
        ),
        (
            "Audit Logs / Action Filter (AUTH_LOGIN)",
            "Verify filtering audit logs by LOGIN action events. (Functional Suitability)",
            "1. In ViewAllLogsOverlay, select Action = 'AUTH_LOGIN'.\n2. Click 'Search'.\n3. Verify only user sign-in events appear in the list.",
            "Action filter: 'AUTH_LOGIN', Click 'Search'",
            "Functional Suitability",
            "Verified: Displays authentication login events."
        ),
        (
            "Audit Logs / Date Range Filter",
            "Verify filtering audit logs within a specific 'From' and 'To' date range. (Functional Suitability)",
            "1. In the Date Range section, pick a Start Date (e.g. '2026-10-01') in 'From' DatePicker.\n2. Pick an End Date (e.g. '2026-10-07') in 'To' DatePicker.\n3. Click 'Search'.\n4. Verify all displayed log timestamps fall within the selected date window.",
            "StartDate: 2026-10-01, EndDate: 2026-10-07, Click 'Search'",
            "Functional Suitability",
            "Verified: Filters logs where Timestamp >= StartDate and Timestamp <= EndDate."
        ),
        (
            "Audit Logs / User Search (Administrator)",
            "Verify Administrator searching logs by User ID via search box and Enter key. (Functional Suitability)",
            "1. As Administrator, enter a target staff GUID in the 'Search by user ID…' text box.\n2. Press the keyboard [Enter] key or click 'Search'.\n3. Verify the log list filters to records performed by or targeting that specific user ID.",
            "SearchText: 'b3f51a22-3c12-4d56-8910-123456789abc', Key: [Enter]",
            "Functional Suitability",
            "Verified: Administrator query filters logs by PerformedBy or TargetId matching the search GUID."
        ),
        (
            "Audit Logs / Search Restriction (Encoder)",
            "Verify Search text box is disabled for Encoders to prevent unauthorized user queries. (Security)",
            "1. Log in as an Encoder and open 'View all logs'.\n2. Inspect the search input field.\n3. Verify Search box is disabled (IsEnabled = false).\n4. Verify placeholder displays: 'Viewing your activity logs (read-only)'.",
            "Encoder session, Inspect Search input box on ViewAllLogsOverlay",
            "Security",
            "Verified: Enforces IsSearchEnabled = false and shows read-only placeholder for non-Admin users."
        ),
        (
            "Audit Logs / Clear Filters Action",
            "Verify 'Clear' button resets search text, action dropdown, date pickers, and reloads page 1. (Usability)",
            "1. Apply custom filters (Search text, specific action, date range).\n2. Click the 'Clear' button.\n3. Verify Search text is cleared, Action filter resets to 'All', and dates reset to null.\n4. Verify log list resets to Page 1 of the default unfiltered audit stream.",
            "Click 'Clear' button (ClearFiltersCommand)",
            "Usability",
            "Verified: ClearFiltersCommand resets all filter properties and queries baseline logs."
        ),
        (
            "Audit Logs / Refresh Logs Stream",
            "Verify '↻' Refresh button updates log records and invalidates cache while retaining active filters. (Reliability)",
            "1. Apply a filter on the audit logs view.\n2. Click the '↻' Refresh icon button in the header.\n3. Verify cache is invalidated and fresh log entries are retrieved from the API.\n4. Verify active search and action filters remain selected.",
            "Click '↻' Refresh button (RefreshCommand)",
            "Reliability",
            "Verified: RefreshCommand clears page cache and reloads current filter state."
        ),
        (
            "Audit Logs / Cursor-Based Pagination (Next Page)",
            "Verify navigating forward to Next page of audit logs. (Functional Suitability)",
            "1. On Page 1 with more than 8 logs, inspect pagination controls.\n2. Click 'Next →' button.\n3. Verify Next page of 8 items loads and PageInfo updates (e.g. 'Page 2').\n4. Verify '← Previous' button becomes enabled.",
            "Click 'Next →' button",
            "Functional Suitability",
            "Verified: Fetches next page chunk using cursor token and updates collection."
        ),
        (
            "Audit Logs / Cursor-Based Pagination (Previous Page)",
            "Verify navigating backward to Previous page of audit logs. (Functional Suitability)",
            "1. From Page 2, click '← Previous' button.\n2. Verify Page 1 reloads with previous 8 items.\n3. Verify '← Previous' button is disabled on Page 1.",
            "Click '← Previous' button",
            "Functional Suitability",
            "Verified: Previous button navigates back in cursor history and disables at page 1."
        ),
        (
            "Audit Logs / In-Memory Page Cache",
            "Verify previously fetched log pages render instantly from memory cache during pagination. (Performance Efficiency)",
            "1. Navigate from Page 1 to Page 2, then Page 3.\n2. Click '← Previous' to return to Page 2.\n3. Verify Page 2 items display instantly without displaying loading spinner or issuing network request.",
            "Navigate forward then backward via pagination buttons",
            "Performance Efficiency",
            "Verified: AuditLogViewModel memory cache serves visited pages instantly."
        ),
        (
            "Audit Logs / Empty State Display",
            "Verify clear empty state notification when no audit logs match search criteria. (Usability)",
            "1. Set a date range filter in the distant past or future where no records exist.\n2. Click 'Search'.\n3. Verify empty state placeholder is shown with message: 'No audit logs found. Try adjusting your filters or date range.'",
            "Filter with zero matching logs, Click 'Search'",
            "Usability",
            "Verified: EmptyStatePanel becomes visible when PagedLogs collection is empty."
        ),
        (
            "Audit Logs / Error Handling and Retry",
            "Verify error panel and 'Retry' button functionality when server connection fails. (Reliability)",
            "1. Disconnect network or stop API service while loading logs.\n2. Observe error state panel: '⚠ Could not load logs' with error details.\n3. Restore connection and click 'Retry' button.\n4. Verify logs load successfully.",
            "Simulated network failure, Click 'Retry' button",
            "Reliability",
            "Verified: Catches network error, shows ErrorPanel, and RetryCommand successfully recovers."
        ),
        (
            "Audit Logs / Card Visual Encoding",
            "Verify color-coded action dots and metadata tags on audit log cards. (Usability)",
            "1. Inspect audit log entries in the list.\n2. Verify CREATE events display green dot, UPDATE events display blue dot, DELETE events display red dot, and AUTH events display purple dot.\n3. Verify role badge pill displays user role.\n4. Verify relative time ('2 hours ago') and exact timestamp are visible.",
            "Inspect PagedLogs ItemTemplate rendering",
            "Usability",
            "Verified: ActionDotConverter maps action categories to respective solid color brushes."
        ),
        (
            "Audit Logs / Open Detail Modal",
            "Verify double-clicking an audit log card opens the comprehensive Detail overlay dialog. (Usability)",
            "1. Double-click on any audit log card in the list.\n2. Verify dim background backdrop appears.\n3. Verify 'Audit Log Detail' modal dialog is presented centered on the screen.",
            "Mouse double-click on audit log card",
            "Usability",
            "Verified: Row double-click sets SelectedLog and opens Detail modal."
        ),
        (
            "Audit Logs / Detail Metadata Verification",
            "Verify Detail modal displays complete audit record metadata (Target, IP, Status, Actor). (Functional Suitability)",
            "1. Open Detail modal for a log entry.\n2. Verify 'Performed by' displays author name and role badge.\n3. Verify 'Target' displays record ID and target type badge.\n4. Verify 'Status' displays SUCCESS (green) or FAILED (red).\n5. Verify client IP Address and exact UTC/local timestamp are displayed.",
            "Inspect SelectedLog metadata in Detail modal",
            "Functional Suitability",
            "Verified: Detail modal binds to actor name, role, IP address, status badge, and target identifier."
        ),
        (
            "Audit Logs / Field Diff Inspection (What Changed)",
            "Verify 'WHAT CHANGED' diff section displays Old Value and New Value for UPDATE actions. (Functional Suitability)",
            "1. Open Detail modal for an UPDATE_SOLO_PARENT or UPDATE_USER log entry.\n2. Scroll to 'WHAT CHANGED' section.\n3. Verify field name is indicated.\n4. Verify original value is displayed in red box ('Before') and updated value is displayed in green box ('After').",
            "Open Detail modal of update log with HasChangeDetail = true",
            "Functional Suitability",
            "Verified: ChangeDetailPanel renders before/after diff for modified attributes."
        ),
        (
            "Audit Logs / Close Detail Modal via '✕' Button",
            "Verify closing Detail modal via '✕' button returns to log list without losing scroll position. (Usability)",
            "1. With Detail modal open, click '✕' close button in top right.\n2. Verify Detail modal disappears and background dimming lifts.\n3. Verify underlying audit log list retains current page and selection.",
            "Click '✕' close button on Detail modal",
            "Usability",
            "Verified: Dismisses Detail modal and restores view focus."
        ),
        (
            "Audit Logs / Close Detail Modal via Backdrop Click",
            "Verify clicking outside the Detail modal on the dim backdrop dismisses the dialog. (Usability)",
            "1. Open Detail modal for an audit log.\n2. Click on the darkened outer backdrop.\n3. Verify Detail modal closes cleanly.",
            "Click dim backdrop outside Detail modal",
            "Usability",
            "Verified: Backdrop mouse click dismisses detail overlay."
        ),
        (
            "Audit Logs / Close ViewAllLogsOverlay to Dashboard",
            "Verify closing ViewAllLogsOverlay smoothly returns user to Dashboard view. (Usability)",
            "1. In ViewAllLogsOverlay header, click the '✕' close button.\n2. Verify overlay raises CloseRequested event.\n3. Verify overlay hides and HomePage dashboard is restored with full interaction.",
            "Click 'CloseBtn' on ViewAllLogsOverlay",
            "Usability",
            "Verified: CloseRequested event triggers overlay collapse in MainWindow."
        ),
        (
            "Audit Logs / Automated Telemetry - Solo Parent Creation",
            "Verify system automatically creates audit log entry upon creating a Solo Parent record. (Functional Suitability)",
            "1. Create a new Solo Parent record in Solo Parent Records page.\n2. Open ViewAllLogsOverlay and click '↻' Refresh.\n3. Verify new audit log row is immediately present at top of Page 1 with action CREATE_SOLO_PARENT.",
            "Create Solo Parent record -> Check Audit Logs",
            "Functional Suitability",
            "Verified: SoloParentApiService create endpoint triggers server-side audit logging."
        ),
        (
            "Audit Logs / Automated Telemetry - Solo Parent Update",
            "Verify updating a Solo Parent record generates UPDATE_SOLO_PARENT audit entry with diff. (Functional Suitability)",
            "1. Edit an existing solo parent's contact number or address.\n2. Save changes.\n3. Open Audit Logs; verify UPDATE_SOLO_PARENT log entry is present.\n4. Inspect Detail modal; verify updated fields display before/after values.",
            "Edit record -> Inspect Audit Logs Detail modal",
            "Functional Suitability",
            "Verified: Server records ChangeDetail records for updated columns."
        ),
        (
            "Audit Logs / Automated Telemetry - Solo Parent Soft-Deletion",
            "Verify soft-deleting a record creates DELETE_SOLO_PARENT audit entry with deletion reason. (Functional Suitability)",
            "1. Soft-delete a record via DeleteRecordDialog with reason 'Relocated Outside Municipality'.\n2. Open Audit Logs; verify DELETE_SOLO_PARENT event is logged.\n3. Verify deletion reason is captured in audit details.",
            "Soft-delete record -> Inspect Audit Logs",
            "Functional Suitability",
            "Verified: AuditLog captures deletion reason and soft-delete event."
        ),
        (
            "Audit Logs / Automated Telemetry - Record Reinstatement",
            "Verify reinstating a record from Recycle Bin generates REINSTATE_SOLO_PARENT audit event. (Functional Suitability)",
            "1. Reinstate a record from the 30-day Recycle Bin.\n2. Open Audit Logs; verify top entry shows REINSTATE_SOLO_PARENT action with citizen name.",
            "Reinstate record -> Check Audit Logs",
            "Functional Suitability",
            "Verified: ArchiveApiService reinstatement triggers audit log telemetry."
        ),
        (
            "Audit Logs / Automated Telemetry - User Account Registration",
            "Verify creating a staff user generates REGISTER_USER audit record. (Security)",
            "1. Register a new user in User Administration.\n2. Open Audit Logs; verify REGISTER_USER entry records created staff email and creator ID.",
            "Create user account -> Check Audit Logs",
            "Security",
            "Verified: UserApiService create endpoint logs user registration audit event."
        ),
        (
            "Audit Logs / Automated Telemetry - User Status Update",
            "Verify disabling or re-enabling a staff user logs USER_STATUS_CHANGE audit event. (Security)",
            "1. Disable a user in User Administration.\n2. Open Audit Logs; verify status change event is logged with old and new status.",
            "Disable user -> Check Audit Logs",
            "Security",
            "Verified: Logs user status modification with actor name and target email."
        ),
        (
            "Audit Logs / Automated Telemetry - Staff Password Reset",
            "Verify resetting a user's password records PASSWORD_RESET audit event. (Security)",
            "1. Reset an employee's password in User Administration.\n2. Open Audit Logs; verify PASSWORD_RESET event is logged.",
            "Reset employee password -> Check Audit Logs",
            "Security",
            "Verified: Logs temporary password reset event."
        ),
        (
            "Audit Logs / Automated Telemetry - Auth Login Event",
            "Verify successful user login generates AUTH_LOGIN audit event with IP address. (Security)",
            "1. Perform user sign-in.\n2. Open Audit Logs; verify AUTH_LOGIN event is recorded with user email, timestamp, and client IP.",
            "Sign in -> Inspect Audit Logs",
            "Security",
            "Verified: AuthApiService login flow records login event in database."
        ),
        (
            "Audit Logs / Automated Telemetry - Auth Logout Event",
            "Verify user logout generates AUTH_LOGOUT audit event. (Security)",
            "1. Sign out of the application.\n2. Sign in as Admin and inspect Audit Logs.\n3. Verify AUTH_LOGOUT event is recorded with session conclusion timestamp.",
            "Sign out -> Sign in as Admin -> Inspect Audit Logs",
            "Security",
            "Verified: AuthApiService logout flow records session termination event."
        ),
        (
            "Audit Logs / Export / Print Prevention for Encoders",
            "Verify Encoders cannot export or print raw system audit logs. (Security)",
            "1. Sign in as an Encoder and open 'View all logs'.\n2. Verify export controls are hidden and Encoder is restricted to personal viewing only.",
            "Encoder session on ViewAllLogsOverlay",
            "Security",
            "Verified: Enforces read-only scoping for Encoder accounts."
        ),
        (
            "Audit Logs / Date Picker Format Enforcement",
            "Verify Start Date cannot be set later than End Date in Date Range filter. (Functional Suitability)",
            "1. In Date Range filter, pick Start Date = '2026-10-15'.\n2. Attempt to pick End Date = '2026-10-01' (preceding Start Date).\n3. Click 'Search'; verify system adjusts or validates date boundary gracefully.",
            "StartDate later than EndDate, Click 'Search'",
            "Functional Suitability",
            "Verified: Date validation prevents inverted date filters."
        ),
        (
            "Audit Logs / Immutability Enforcement",
            "Verify audit log records cannot be edited or deleted via the UI. (Security)",
            "1. Inspect audit log list and Detail modal.\n2. Verify no edit, delete, or modify actions exist on audit logs.\n3. Verify all displayed audit data is strictly read-only.",
            "Inspect ViewAllLogsOverlay controls",
            "Security",
            "Verified: Audit log records are strictly append-only and immutable."
        )
    ]
