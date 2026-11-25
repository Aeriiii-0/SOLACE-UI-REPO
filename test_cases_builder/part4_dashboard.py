# -*- coding: utf-8 -*-

def get_dashboard_cases():
    return [
        (
            "Dashboard / Default Landing Page",
            "Verify application automatically lands on HomePage (Dashboard) with active button highlight upon successful login. (Successful)",
            "1. Log into the application with valid credentials.\n2. Observe the initial landing page in the main window frame.\n3. Verify the Dashboard tab button is visually active (maroon highlight #702943).\n4. Verify TopBar displays 'Dashboard' title and KPI cards are visible.",
            "Valid login credentials, Application startup",
            "Functional Suitability",
            "Verified: MainWindow.xaml.cs navigates to HomePage by default and highlights Dashboard button."
        ),
        (
            "Sidebar Navigation / Solo Parent Records Tab",
            "Verify navigation from HomePage to Solo Parent Records page and top bar title update. (Successful)",
            "1. From HomePage, click the 'Solo Parent Records' tab icon in the sidebar.\n2. Verify the TopBar title updates to 'Solo Parent Records'.\n3. Verify MainFrame loads the Solo Parent Records table.\n4. Verify user context (UserName, Role, Barangay) is passed to the page.",
            "Click 'SoloParentRecords' NavButton in sidebar",
            "Functional Suitability",
            "Verified: Navigates to SoloParentRecordsPage and updates TopBar title."
        ),
        (
            "Sidebar Navigation / User Administration Tab (Admin)",
            "Verify administrator navigation to User Administration tab and access to user management table. (Successful)",
            "1. Sign in using an Administrator account.\n2. Click the 'User Administration' tab icon in the sidebar.\n3. Verify page title updates to 'User Administration'.\n4. Verify user management list and staff registration controls are rendered.",
            "Click 'UserAdministration' NavButton (Admin session)",
            "Functional Suitability",
            "Verified: Renders UserAdministrationPage for Administrator."
        ),
        (
            "Sidebar Navigation / Role-Based Access Enforcement (Encoder)",
            "Verify User Administration and Subsidy Recommendation tabs are collapsed/hidden for Encoder role. (Security)",
            "1. Sign in using an Encoder account.\n2. Inspect the sidebar navigation bar.\n3. Verify 'User Administration' and 'Subsidy Recommendation' icons are completely collapsed/hidden.\n4. Verify Encoder cannot view or click administrative links.",
            "Encoder account login, inspect sidebar",
            "Security",
            "Verified: MainWindow.xaml.cs collapses administrative NavButtons based on CurrentUserRole."
        ),
        (
            "Sidebar Navigation / Role-Based Access Enforcement (Barangay Secretary)",
            "Verify User Administration and Subsidy Recommendation tabs are hidden for Barangay Secretary. (Security)",
            "1. Sign in as a Barangay Secretary.\n2. Inspect the sidebar navigation bar.\n3. Verify 'User Administration' and 'Subsidy Recommendation' tabs are collapsed.",
            "Barangay Secretary login, inspect sidebar",
            "Security",
            "Verified: Administrative tabs are restricted to Admin roles only."
        ),
        (
            "Sidebar Navigation / Subsidy Recommendation Tab (Admin)",
            "Verify navigation to Subsidy Recommendation tab and display of ML priority rankings. (Successful)",
            "1. Sign in as Administrator.\n2. Click the 'Subsidy Recommendation' tab icon in sidebar.\n3. Verify TopBar title changes to 'Subsidy Recommendation'.\n4. Verify AI-generated priority recommendations list is rendered.",
            "Click 'SubsidyRecommendation' NavButton",
            "Functional Suitability",
            "Verified: Opens SubsidyRecommendationPage for authorized users."
        ),
        (
            "Sidebar Navigation / Analytics Tab",
            "Verify tab switching from HomePage to Analytics page and loading of demographic visualizations. (Successful)",
            "1. From HomePage, click the 'Analytics' tab icon in the sidebar.\n2. Verify TopBar title updates to 'Analytics'.\n3. Verify demographic breakdown charts, barangay breakdowns, and export options load.",
            "Click 'Analytics' NavButton in sidebar",
            "Functional Suitability",
            "Verified: Navigates to AnalyticsPage and initializes charts."
        ),
        (
            "Sidebar Navigation / Visual Selection Highlight Styling",
            "Verify visual selection highlight changes on sidebar buttons during tab transitions. (Usability)",
            "1. Note current active tab with maroon circle background.\n2. Click a different sidebar tab.\n3. Verify previous tab reverts to transparent background.\n4. Verify newly selected tab gains maroon fill (#702943) and white foreground icon.",
            "Sequential tab clicks across sidebar buttons",
            "Usability",
            "Verified: ActivateButton method manages button background brushes and icon fills."
        ),
        (
            "Sidebar Navigation / Page Caching & State Preservation",
            "Verify cached page instances retain scroll and filter states when switching between tabs. (Performance Efficiency)",
            "1. Navigate to Solo Parent Records and enter a search filter or scroll table.\n2. Click 'Dashboard' to return to HomePage.\n3. Click 'Solo Parent Records' tab again.\n4. Verify previous search filter and scroll position are preserved without reloading.",
            "Filter records, navigate to Dashboard, return to Records",
            "Performance Efficiency",
            "Verified: MainWindow page caching dictionary preserves page instances."
        ),
        (
            "Sidebar Navigation / Browser Back/Forward Journal Disabled",
            "Verify browser-style back/forward navigation history is cleared to prevent unsynchronized state. (Reliability)",
            "1. Switch across 3 different tabs in sequence.\n2. Press mouse back button or keyboard Alt+Left Arrow.\n3. Verify MainFrame does not execute back navigation.\n4. Verify sidebar active highlight remains in sync with the visible page.",
            "Tab switching, followed by Alt+Left Arrow / mouse back",
            "Reliability",
            "Verified: NavigationCommands.BrowseBack and NavigationService journal are cleared."
        ),
        (
            "Sidebar Navigation / Sidebar Hover Tooltips",
            "Verify hovering over collapsed sidebar icons displays descriptive floating tooltips. (Usability)",
            "1. Hover mouse pointer over each icon in the sidebar.\n2. Verify floating maroon tooltip appears to the right with correct label (Dashboard, Solo Parent Records, etc.).\n3. Move pointer away and verify tooltip dismisses.",
            "Mouse hover over sidebar NavButtons",
            "Usability",
            "Verified: ToolTipService properties display clean descriptive tooltips on hover."
        ),
        (
            "TopBar / User Identity & Profile Pill",
            "Verify TopBar displays correct user initials, full name, and role badge. (Functional Suitability)",
            "1. Inspect top right corner of the application window.\n2. Verify initial avatar displays first letter of user's name.\n3. Verify user name text matches logged-in user.\n4. Verify role badge displays 'Administrator', 'Encoder', or 'Barangay Secretary'.",
            "Application loaded, inspect ProfilePill",
            "Functional Suitability",
            "Verified: TopBar.xaml binds to MainWindow user identity properties."
        ),
        (
            "TopBar / Profile Dropdown Menu Toggle",
            "Verify clicking Profile Pill toggles dropdown menu with 'View Profile', 'Change Password', and 'Logout'. (Usability)",
            "1. Click on the Profile Pill in TopBar.\n2. Verify ProfileMenu popup opens smoothly.\n3. Verify menu displays user name, role, and options: 'View Profile', 'Change Password', and 'Logout'.\n4. Click outside to dismiss popup.",
            "Mouse left click on ProfilePill",
            "Usability",
            "Verified: TopBar ProfilePill_Click toggles ProfileMenu.IsOpen."
        ),
        (
            "TopBar / Help & FAQ Modal",
            "Verify opening, scrolling, and closing the Help & FAQ guide popup from TopBar. (Usability)",
            "1. In TopBar, click the '?' Help & FAQ button.\n2. Verify FaqPopup opens showing FAQs about SOLACE, user roles, adding records, and audit logs.\n3. Scroll through FAQ entries.\n4. Click '✕' close button.\n5. Verify popup closes cleanly.",
            "Click 'BtnFaq', scroll contents, click FaqClose button",
            "Usability",
            "Verified: FaqPopup renders guide information and dismisses on close button click."
        ),
        (
            "Dashboard / KPI Summary Cards Display",
            "Verify dashboard displays Registered, Active, and Renewals Due count cards with correct values. (Functional Suitability)",
            "1. On HomePage dashboard, inspect the 3 primary metric cards.\n2. Verify 'REGISTERED Solo Parents' card shows total registered count.\n3. Verify 'ACTIVE Valid Records' card shows count in green with percentage rate.\n4. Verify 'RENEWALS Due this month' card shows renewals due count in amber.",
            "HomePage loaded, API analytics response",
            "Functional Suitability",
            "Verified: DashboardViewModel loads KPI metrics from AnalyticsApiService."
        ),
        (
            "Dashboard / Inactive Records Tooltip on Delta Pill",
            "Verify hovering over the Registered delta badge reveals tooltip showing inactive records count. (Usability)",
            "1. On the Registered Solo Parents KPI card, hover mouse over the delta pill badge.\n2. Verify tooltip displays: '<Count> inactive records'.\n3. Move pointer away to dismiss tooltip.",
            "Mouse hover over Registered delta pill badge",
            "Usability",
            "Verified: Tooltip binds to InactiveRecords count on Dashboard KPI card."
        ),
        (
            "Dashboard / Cumulative Registrations Line Chart Rendering",
            "Verify dynamic rendering of the Registrations line chart curve and gradient fill. (Functional Suitability)",
            "1. On HomePage dashboard, inspect the Registrations line chart section.\n2. Verify the SVG PathGeometry renders smooth cubic bezier curve for cumulative registration counts.\n3. Verify maroon gradient area fill renders flush with the horizontal baseline.",
            "Inspect Registrations chart on DashboardPage",
            "Functional Suitability",
            "Verified: ChartCanvas renders dynamic line path and area gradient."
        ),
        (
            "Dashboard / Cumulative Line Chart Quarterly / Monthly Toggle",
            "Verify toggling between Quarterly and Monthly radio pills re-renders the registration line chart. (Functional Suitability)",
            "1. On Registrations chart, toggle from 'Quarterly' (RbQuarterly) to 'Monthly' (RbMonthly).\n2. Verify line path, area gradient, and data points re-render to reflect monthly data.\n3. Toggle back to 'Quarterly'; verify quarterly aggregation re-renders.",
            "Toggle 'Quarterly' (RbQuarterly) and 'Monthly' (RbMonthly) pills",
            "Functional Suitability",
            "Verified: RadioButton checked event re-executes DrawChart with selected period granularity."
        ),
        (
            "Dashboard / Cumulative Line Chart Edge-Clamped Hover Tooltips",
            "Verify hovering data points on the cumulative line chart displays tooltips clamped within canvas bounds. (Usability)",
            "1. Hover mouse over leftmost, middle, and rightmost data point nodes on the chart.\n2. Verify ChartTip displays period range, total count, and delta pill.\n3. Verify tooltip does not clip off the left, right, or top boundaries on 1024x640 resolutions.",
            "Mouse hover over ChartCanvas data points on 1024x640 window",
            "Usability",
            "Verified: ShowChartTip clamps coordinates and flips vertically when near top edge."
        ),
        (
            "Dashboard / AI Recommendation Summary Banner",
            "Verify AI Recommendation banner renders candidate count and priority urgency level. (Functional Suitability)",
            "1. On HomePage, inspect the ML Model recommendation summary banner.\n2. Verify text indicates number of high-priority solo parent candidates awaiting review.\n3. Verify badge color reflects priority level.",
            "Inspect ML summary banner on DashboardPage",
            "Functional Suitability",
            "Verified: DashboardViewModel loads recommendation summary from SubsidyService."
        ),
        (
            "Dashboard / AI Recommendation Banner Action",
            "Verify clicking 'Review ↗' on the ML subsidy recommendation banner navigates to Subsidy Recommendation page. (Functional Suitability)",
            "1. On HomePage, locate the ML Model recommendation summary banner.\n2. Click the 'Review ↗' button.\n3. Verify application routes to Subsidy Recommendation page.\n4. Verify sidebar active tab switches to Subsidy Recommendation.",
            "Click 'Review ↗' button on ML summary banner",
            "Functional Suitability",
            "Verified: BtnReview_Click navigates to SubsidyRecommendation and activates sidebar button."
        ),
        (
            "Dashboard / Recent Activity Feed Chronological Display",
            "Verify recent audit logs load chronologically with color-coded action dots and relative timestamps. (Functional Suitability)",
            "1. On HomePage, inspect the Recent Activity feed in the right sidebar.\n2. Verify recent audit entries display action dots (Green=Create, Blue=Edit, Red=Delete, Purple=Auth).\n3. Verify description, author name, and relative timestamp ('2 hours ago') are displayed.",
            "HomePage loaded, AuditLogApiService response",
            "Functional Suitability",
            "Verified: ItemsControl binds to RecentAuditLogs collection."
        ),
        (
            "Dashboard / Role-Based Activity Feed Scoping",
            "Verify Encoders only see their own activities while Administrators see system-wide logs in feed. (Security)",
            "1. Log in as an Encoder and check Recent Activity feed.\n2. Verify all listed logs are performed by the logged-in encoder.\n3. Log out and log in as Administrator.\n4. Verify Recent Activity displays logs from all staff members across barangays.",
            "Encoder session vs Administrator session in Recent Activity feed",
            "Security",
            "Verified: API scopes audit log query by user ID when authenticated as non-Admin."
        ),
        (
            "Dashboard / Audit Log Detail Overlay",
            "Verify clicking an audit log card in feed opens modal card with full record metadata. (Usability)",
            "1. In Recent Activity feed, click on any audit log card.\n2. Verify dim backdrop appears and Log Detail modal card opens.\n3. Verify action badge, record name, author, IP, and timestamp are displayed.\n4. Click the '✕' close button or click the dim backdrop to dismiss.",
            "Click audit log item, Click '✕' button or backdrop",
            "Usability",
            "Verified: Log item click opens LogDetailModal and dismisses cleanly."
        ),
        (
            "Dashboard / View All Logs Button Action",
            "Verify clicking 'View all logs' button opens full-screen audit log viewer modal. (Functional Suitability)",
            "1. In Recent Activity panel, click the 'View all logs' button.\n2. Verify full overlay (ViewAllLogsOverlay) appears with complete searchable audit log history.\n3. Click close button on overlay; verify overlay closes and returns to dashboard.",
            "Click 'View all logs' button, Click close on LogsOverlay",
            "Functional Suitability",
            "Verified: BtnViewAllLogs_Click opens ViewAllLogsOverlay."
        ),
        (
            "Dashboard / ReviewAlertPopoutDialog Display on Pending Subsidies",
            "Verify popout alert dialog is displayed on dashboard when unreviewed subsidy recommendations exist. (Functional Suitability)",
            "1. Log in as Administrator when unreviewed subsidy recommendations exist.\n2. Verify ReviewAlertPopoutDialog appears centered on MainWindow showing pending candidate count.\n3. Click 'Review Subsidy'; verify dialog closes and routes directly to Subsidy Recommendation page.",
            "ReviewAlertPopoutDialog.ShowIfReviewsNeeded(MainWindow, pendingCount)",
            "Functional Suitability",
            "Verified: ReviewAlertPopoutDialog launches when pendingCount > 0 and routes on button click."
        ),
        (
            "Dashboard / ReviewAlertPopoutDialog Dismissal",
            "Verify clicking 'Dismiss' on ReviewAlertPopoutDialog closes the modal cleanly. (Usability)",
            "1. When ReviewAlertPopoutDialog appears, click 'Dismiss' (or '✕').\n2. Verify dialog closes and dashboard interaction is fully available.",
            "Click 'Dismiss' on ReviewAlertPopoutDialog",
            "Usability",
            "Verified: Dismiss_Click closes dialog without navigation."
        ),
        (
            "Shell / Logout Confirmation Modal",
            "Verify clicking Logout in TopBar Profile dropdown opens confirmation dialog with Cancel and Logout actions. (Usability)",
            "1. Click the Profile Pill in the top-right TopBar to open the dropdown menu.\n2. Click 'Logout'.\n3. Verify the 'Confirm Logout' dialog appears asking 'Are you sure you want to sign out?'.\n4. Verify 'Cancel' and 'Logout' buttons are present.",
            "Click TopBar ProfilePill -> Click 'Logout'",
            "Usability",
            "Verified: TopBar logout menu invokes ActionConfirmDialog for logout confirmation."
        ),
        (
            "Shell / Logout Cancel Action",
            "Verify clicking Cancel on the Confirm Logout dialog aborts sign-out and preserves active session. (Reliability)",
            "1. Open TopBar Profile dropdown and click 'Logout'.\n2. When 'Confirm Logout' dialog appears, click 'Cancel'.\n3. Verify dialog closes immediately and user session and active tab remain intact.",
            "Click ProfilePill -> 'Logout' -> Click 'Cancel'",
            "Reliability",
            "Verified: ActionConfirmDialog returns false; session remains active."
        ),
        (
            "Shell / Frameless Window Drag Move",
            "Verify clicking and dragging custom TitleBar moves the window smoothly across monitors. (Usability)",
            "1. Click mouse left button on empty area of TitleBar.\n2. Drag mouse across screen.\n3. Verify application window follows mouse drag seamlessly.",
            "Mouse drag on MainWindow TitleBar",
            "Usability",
            "Verified: TitleBar_MouseLeftButtonDown invokes DragMove()."
        ),
        (
            "Shell / Frameless Window Minimize and Restore",
            "Verify clicking Minimize button minimizes application to taskbar and clicking taskbar icon restores it. (Usability)",
            "1. Click Minimize button '-' on TitleBar.\n2. Verify window minimizes to Windows taskbar.\n3. Click taskbar icon; verify window restores to previous dimensions and active page.",
            "Click 'BtnMinimize' on TitleBar",
            "Usability",
            "Verified: BtnMinimize_Click sets WindowState = WindowState.Minimized."
        ),
        (
            "Shell / Frameless Window Maximize and Restore",
            "Verify clicking Maximize button toggles between Maximized and Normal window states. (Usability)",
            "1. Click Maximize button (square icon) on TitleBar.\n2. Verify window maximizes to fill screen without covering Windows taskbar.\n3. Click button again; verify window restores to standard size.",
            "Click 'BtnMaximize' on TitleBar",
            "Usability",
            "Verified: BtnMaximize_Click toggles WindowState between Maximized and Normal."
        ),
        (
            "Shell / Humanized Offline Connection Notice",
            "Verify abrupt network disconnection displays humanized 'No Internet Connection' notice instead of technical error. (Usability)",
            "1. Log in and navigate to Dashboard, Records, or Analytics.\n2. Disconnect internet connection abruptly and trigger data refresh.\n3. Verify alert title reads 'No Internet Connection' with user-friendly guidance.",
            "Disconnect Wi-Fi/Network during active session, Click Refresh",
            "Usability",
            "Verified: ApiClient.IsNetworkOrConnectionError and HumanizeErrorMessage display clear offline guidance."
        ),
        (
            "Shell / Window Minimum Size Constraints",
            "Verify MainWindow enforces minimum width and height constraints to prevent UI layout collapse. (Usability)",
            "1. Restore MainWindow to normal window state.\n2. Drag window borders to attempt shrinking below 1024x640.\n3. Verify window resizing halts at MinWidth=1024 and MinHeight=640.",
            "Attempt to resize MainWindow below 1024x640",
            "Usability",
            "Verified: MainWindow.xaml sets MinWidth='1024' and MinHeight='640'."
        ),
        (
            "Dashboard / Refresh Dashboard Data",
            "Verify refreshing dashboard updates KPI numbers, line chart, and recent activity feed. (Reliability)",
            "1. After new records are added, return to Dashboard.\n2. Trigger refresh or observe auto-refresh.\n3. Verify KPI cards, registration chart, and activity feed update to reflect latest data.",
            "Refresh Dashboard data",
            "Reliability",
            "Verified: DashboardViewModel reloads analytics and recent activity feed."
        )
    ]
