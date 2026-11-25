# -*- coding: utf-8 -*-

def get_analytics_cases():
    return [
        (
            "Analytics / Per-Month Temporal Filtering",
            "Verify filtering analytics by specific month and year updates KPI metrics and charts. (Functional Suitability)",
            "1. Open Analytics page.\n2. In Filter popup, select Type = 'Per Month'.\n3. Select Year = '2026', Month = 'October'.\n4. Click 'Apply Filter'.\n5. Verify header pill badge displays 'October 2026' and metrics reflect that month.",
            "Filter Type: 'Per Month', Year: '2026', Month: 'October', Click 'Apply Filter'",
            "Functional Suitability",
            "Verified: Queries MonthlyAnalytics with month and year parameters."
        ),
        (
            "Analytics / Quarterly Temporal Filtering",
            "Verify filtering analytics by Quarter (Q1–Q4) aggregates three months of demographic data. (Functional Suitability)",
            "1. In Filter popup, select Type = 'Quarterly'.\n2. Select Year = '2026', Quarter = 'Q1 (Jan – Mar)'.\n3. Click 'Apply Filter'.\n4. Verify active period badge updates to 'Q1 2026' and quarterly totals load.",
            "Filter Type: 'Quarterly', Year: '2026', Quarter: '1', Click 'Apply Filter'",
            "Functional Suitability",
            "Verified: Aggregates Q1 analytics data."
        ),
        (
            "Analytics / Yearly Temporal Filtering",
            "Verify filtering analytics by full calendar year aggregates 12 months of registry data. (Functional Suitability)",
            "1. In Filter popup, select Type = 'Yearly', Year = '2025'.\n2. Click 'Apply Filter'.\n3. Verify period badge displays 'Full Year 2025' and annual figures render.",
            "Filter Type: 'Yearly', Year: '2025', Click 'Apply Filter'",
            "Functional Suitability",
            "Verified: Aggregates 12 months of annual analytics data."
        ),
        (
            "Analytics / Custom Date Range Filtering",
            "Verify filtering analytics by custom Start Date and End Date pickers. (Functional Suitability)",
            "1. In Filter popup, select Type = 'Custom Range'.\n2. Select Start Date = '2026-01-01', End Date = '2026-06-30'.\n3. Click 'Apply Filter'.\n4. Verify charts display aggregate data for the custom 6-month window.",
            "Filter Type: 'Custom Range', StartDate: '2026-01-01', EndDate: '2026-06-30'",
            "Functional Suitability",
            "Verified: Temporal scoping between StartDate and EndDate."
        ),
        (
            "Analytics / Geographic Scoping (Single Barangay)",
            "Verify filtering analytics from citywide to a single barangay (e.g., 'San Vicente'). (Functional Suitability)",
            "1. In Filter popup, select Barangay = 'San Vicente'.\n2. Click 'Apply Filter'.\n3. Verify KPI cards and charts reflect only residents of Barangay San Vicente.",
            "Select Barangay: 'San Vicente', Click 'Apply Filter'",
            "Functional Suitability",
            "Verified: Scopes analytics calculations to single barangay locality."
        ),
        (
            "Analytics / Reset Filters to Default Baseline",
            "Verify clicking 'Reset' restores filters to current month, current year, and 'All Barangays'. (Usability)",
            "1. Apply custom historical filter.\n2. Reopen Filter popup and click 'Reset'.\n3. Verify dropdowns restore to current month and 'All Barangays' and dashboard reloads baseline.",
            "Click 'Reset' in FilterPopup",
            "Usability",
            "Verified: Resets filter model to default temporal parameters."
        ),
        (
            "Analytics / Active Period Badge Label Display",
            "Verify header pill badge displays accurate textual description of active temporal filter. (Usability)",
            "1. Apply various filter combinations (Monthly, Quarterly, Custom).\n2. Inspect TxtActiveFilter badge in header.\n3. Verify label accurately summarizes active window.",
            "Inspect TxtActiveFilter under different filter states",
            "Usability",
            "Verified: TxtActiveFilter dynamically formats human-readable filter description."
        ),
        (
            "Analytics / KPI Total Records & Validity Rate",
            "Verify Total Records and Valid/Active Records display accurate counts and mathematical percentage share. (Functional Suitability)",
            "1. Load analytics with 100 total records (80 Active, 20 Inactive).\n2. Inspect TxtTotalRecords ('100'), TxtValidRecords ('80'), and TxtValidRate ('80.0%').\n3. Verify math checks.",
            "Verify TxtTotalRecords, TxtValidRecords, and TxtValidRate",
            "Functional Suitability",
            "Verified: Mathematical percentage calculation matches active/total ratio."
        ),
        (
            "Analytics / KPI Inactive Records Count & Non-Negative Boundary",
            "Verify Inactive Records count and percentage rate calculation, ensuring value never falls below zero. (Reliability)",
            "1. Load period data where Active equals Total.\n2. Inspect TxtInactiveRecords ('0') and TxtInactiveRate ('0.0%').\n3. Verify count is non-negative.",
            "Inspect TxtInactiveRecords and TxtInactiveRate",
            "Reliability",
            "Verified: Inactive metrics clamp safely at zero."
        ),
        (
            "Analytics / KPI New Registrations vs Renewals Tally",
            "Verify New Registrations and Renewals KPI cards display accurate counts for filtered period. (Functional Suitability)",
            "1. Check TxtNewSpic and TxtRenewedSpic cards on analytics dashboard.\n2. Cross-check against backend response TotalNewRegistrations and TotalRenewals.\n3. Verify counts align.",
            "Compare TxtNewSpic and TxtRenewedSpic against API DTO",
            "Functional Suitability",
            "Verified: Binds newly issued SPIC vs renewed SPIC metrics."
        ),
        (
            "Analytics / Dependents Total Count & Average Ratio",
            "Verify Total Children card calculates sum of dependents and displays average children per solo parent. (Functional Suitability)",
            "1. Ingest dataset with 150 total children across 100 solo parents.\n2. Inspect TxtTotalChildren ('150') and TxtAvgChildren ('1.5 per parent').\n3. Verify ratio accuracy.",
            "Verify TxtTotalChildren and TxtAvgChildren",
            "Functional Suitability",
            "Verified: Average dependents calculation divides total children by total parents."
        ),
        (
            "Analytics / Gender Donut Chart Geometry & Arcs",
            "Verify Gender Donut canvas renders proportional female (burgundy) and male (blush pink) arcs and center count. (Usability)",
            "1. Load dataset with 75 Females and 25 Males.\n2. Inspect GenderDonut canvas.\n3. Verify burgundy arc covers 75% of circumference and blush pink arc covers 25%.\n4. Verify center label displays '100'.",
            "Data: Female=75, Male=25",
            "Usability",
            "Verified: DonutChart PathGeometry draws proportional circular arcs."
        ),
        (
            "Analytics / Applicant Age Bracket Horizontal Bars",
            "Verify Age Distribution chart maps counts into 4 standard brackets (19 & below, 20–39, 40–59, 60 & above). (Functional Suitability)",
            "1. Inspect AgeChart in Demographics section.\n2. Verify all 4 age brackets render with horizontal proportional bars.\n3. Verify percentage labels sum to 100%.",
            "Inspect AgeChart rows",
            "Functional Suitability",
            "Verified: Buckets applicants into standard demographic age ranges."
        ),
        (
            "Analytics / Civil Status Consolidated Breakdown",
            "Verify Civil Status chart consolidates Separated and Annulled categories into 'Sep./Annul.' alongside Single, Married, and Widowed. (Functional Suitability)",
            "1. Ingest dataset with Separated=8 and Annulled=2.\n2. Inspect CivilStatusChart.\n3. Verify 'Sep./Annul.' row displays combined count of 10 alongside Single, Married, and Widowed.",
            "Check CivilStatusChart 'Sep./Annul.' row",
            "Functional Suitability",
            "Verified: Merges separated and annulled legal categories per statutory reporting guidelines."
        ),
        (
            "Analytics / Employment Status Distribution",
            "Verify Employment Status chart maps 'Employed', 'Self-Employed', and 'Not Employed' categories. (Functional Suitability)",
            "1. Inspect EmploymentChart in Socioeconomic card.\n2. Verify count and percentage labels for each employment category.",
            "Inspect EmploymentChart items",
            "Functional Suitability",
            "Verified: Renders employment status breakdown."
        ),
        (
            "Analytics / Monthly Income Bracket Proportions",
            "Verify Monthly Income chart maps into 3 tiers (Below Min. Wage, Min.+1 – ₱20,833, ₱20,834 and above). (Functional Suitability)",
            "1. Inspect IncomeChart rows.\n2. Verify tier labels and percentage distributions accurately reflect income classification.",
            "Inspect IncomeChart rows",
            "Functional Suitability",
            "Verified: Groups solo parents into statutory minimum wage threshold tiers."
        ),
        (
            "Analytics / Child Dependents by Statutory Age Tier Progress Bars",
            "Verify 'Child dependents by age' card renders proportional bars and tooltips for 0–6, 7–21, and 22+ years. (Functional Suitability)",
            "1. Inspect 'Child dependents by age' card (PbChild0, PbChild1, PbChild2).\n2. Verify counts and percentage labels render for 0–6 yrs, 7–21 yrs, and 22+ yrs.\n3. Hover over each bar; verify tooltip displays count, percentage, and service note.",
            "Inspect PbChild0, PbChild1, PbChild2 and hover tooltips",
            "Functional Suitability",
            "Verified: Statutory child age tier progress bars render accurately with hover tooltips."
        ),
        (
            "Analytics / Top Categories Horizontal Bar Canvas (RA 11861 Codes)",
            "Verify 'Top categories' canvas (CvTopCategories) renders ranked bars with #1 highlighted in maroon and hover tooltips. (Usability)",
            "1. Inspect 'Top categories' card (CvTopCategories).\n2. Verify categories are ranked descending with #1 category highlighted in maroon (#7D2245) and remaining bars in rose.\n3. Hover over category bar; verify tooltip displays category title, count, percentage, and statutory description.",
            "Inspect CvTopCategories and hover over category bars",
            "Usability",
            "Verified: Draws horizontal bar chart with highlighted leader and descriptive tooltips."
        ),
        (
            "Analytics / Solo Parents per Barangay Startup & Post-Logout Rendering",
            "Verify 'Solo parents per barangay' chart renders all 24 Biñan barangays on initial launch and after logout/login. (Functional Suitability)",
            "1. Log in and open Analytics without touching filters; verify all 24 barangays load.\n2. Log out, log back in, and return to Analytics; verify chart renders all barangays with no empty state.",
            "Fresh login -> Open Analytics -> Logout -> Login -> Open Analytics",
            "Functional Suitability",
            "Verified: Case-insensitive grouping and cumulative fallback ensure all 24 barangays render reliably."
        ),
        (
            "Analytics / Solo Parents per Barangay Pagination Controls",
            "Verify 'Solo parents per barangay' renders 10 bars per page across 3 pages with Previous/Next buttons. (Functional Suitability)",
            "1. On CvBarangay chart, verify Page 1 displays Top 10 barangays with 'Top barangay' pill over #1.\n2. Click 'Next' (NextBarangayButton); verify Page 2 renders ranks 11–20.\n3. Click 'Next' again; verify Page 3 renders remaining barangays.\n4. Click 'Previous' (PreviousBarangayButton); verify Page 2 restores.",
            "Click NextBarangayButton and PreviousBarangayButton",
            "Functional Suitability",
            "Verified: Paginates 24 barangays across 3 pages (10 per page)."
        ),
        (
            "Analytics / Barangay Vertical Bar Geometry (Rounded Top, Flat Bottom)",
            "Verify vertical bars in CvBarangay render with rounded top corners (r=8) and straight flat bottom (r=0) flush with axis. (Usability)",
            "1. Inspect vertical bars in CvBarangay.\n2. Verify top corners of each bar have smooth rounded ArcSegments (radius 8).\n3. Verify bottom corners sit completely flat and flush against baseline.",
            "Visual inspection of CvBarangay bar geometry",
            "Usability",
            "Verified: DrawBarangayBarChart uses PathGeometry with top ArcSegments and flat bottom LineSegments."
        ),
        (
            "Analytics / Barangay Bar Data-Point Hover Tooltips",
            "Verify hovering directly over a barangay bar shows rank, count, percentage, and block count without background hover noise. (Usability)",
            "1. Hover cursor directly over a barangay bar in CvBarangay.\n2. Verify tooltip displays: Barangay Name, '#N Ranked • X solo parents (Y% of citywide)', and block count (1 block = 5 solo parents).\n3. Move mouse to empty background space between bars; verify no tooltip appears.",
            "Mouse hover over bar host vs empty canvas space in CvBarangay",
            "Usability",
            "Verified: Tooltip is attached strictly to bar visual hosts, eliminating background clutter."
        ),
        (
            "Analytics / Concise Data-Point Hover Tooltips on Age and Income Bars",
            "Verify outer card containers have no cluttering tooltips while Age and Income bars show concise data tooltips. (Usability)",
            "1. Move cursor across empty card background; verify no large box tooltips appear.\n2. Hover over Age Group bars; verify concise count, percentage, and RA 11861 tier tooltip appears after 350ms.\n3. Hover over Monthly Income bars; verify concise tooltip appears after 350ms.",
            "Hover card whitespace (no tooltip), Hover Age & Income progress bars",
            "Usability",
            "Verified: Container-level tooltips stripped; data-point tooltips attached via UpdateAgeAndIncomeTooltips()."
        ),
        (
            "Analytics / Data Caching & Instant Return",
            "Verify subsequent requests for identical period load instantly from cache without network delay. (Performance Efficiency)",
            "1. Load October 2026 data (network fetch).\n2. Switch to November, then switch back to October.\n3. Verify October data renders instantly from cache without loading spinner.",
            "Switch between periods in Analytics",
            "Performance Efficiency",
            "Verified: AnalyticsViewModel in-memory cache returns cached period DTO instantly."
        ),
        (
            "Analytics / Refresh Button Cache Invalidation",
            "Verify clicking 'Refresh' button invalidates cache and queries fresh data from API. (Reliability)",
            "1. On Analytics page, click 'Refresh' button (BtnRefresh).\n2. Verify cached DTO is evicted, loading skeleton appears, and fresh network request executes.",
            "Click 'Refresh' button (BtnRefresh)",
            "Reliability",
            "Verified: BtnRefresh_Click invalidates cache and calls LoadAnalyticsAsync(forceRefresh: true)."
        ),
        (
            "Analytics / Export to Excel SaveFileDialog Trigger",
            "Verify clicking 'Export to Excel' opens Windows SaveFileDialog with default file name. (Functional Suitability)",
            "1. Click 'Export to Excel' button in header.\n2. Verify Windows SaveFileDialog opens with default filename 'DSWD_Solo_Parent_Report_Q4_2026.xlsx'.",
            "Click 'Export to Excel' button",
            "Functional Suitability",
            "Verified: ExportBtn_Click initializes SaveFileDialog with quarterly report naming convention."
        ),
        (
            "Analytics / Export Options Dialog (Password & Auto-Open)",
            "Verify ExportOptionsDialog provides password encryption and 'Open file after export' controls. (Security & Usability)",
            "1. Confirm SaveFileDialog destination.\n2. Verify ExportOptionsDialog opens.\n3. Enter workbook password 'Solace2026!' and check 'Open file after export'.\n4. Click 'Export'; verify workbook generates with password protection and opens in Excel.",
            "Set Password: 'Solace2026!', Check 'Open file after export', Click 'Export'",
            "Security",
            "Verified: AnalyticsExcelExportService applies password encryption and launches file process."
        ),
        (
            "Analytics / Export Cancel Handling",
            "Verify canceling SaveFileDialog or ExportOptionsDialog cleanly aborts export without error. (Usability)",
            "1. Click 'Export to Excel' and click 'Cancel' in SaveFileDialog.\n2. Repeat export and click 'Cancel' in ExportOptionsDialog.\n3. Verify both cancellations exit cleanly without throwing exceptions.",
            "Click 'Cancel' in SaveFileDialog, then in ExportOptionsDialog",
            "Usability",
            "Verified: Clean cancellation handling in export pipeline."
        ),
        (
            "Analytics / Export Locked Destination File Handling",
            "Verify friendly warning dialog when export targets an existing report currently open and locked in Excel. (Reliability)",
            "1. Open target Excel report in Microsoft Excel.\n2. In SOLACE Analytics, trigger export targeting the locked file name.\n3. Verify system catches IOException and displays ActionConfirmDialog warning: 'File in use. Please close the file and try again.'",
            "Export to open locked file 'report.xlsx'",
            "Reliability",
            "Verified: Catches IOException and displays ActionConfirmDialog warning."
        ),
        (
            "Analytics / Zero Records Historical Period Rendering",
            "Verify clean UI presentation and zero percentages when querying a period with zero records. (Reliability)",
            "1. Filter for a historical year with zero recorded solo parents (e.g. Year 2015).\n2. Apply filter.\n3. Verify all KPI cards display '0', rates display '0.0%', and charts render clean zero baseline without NaN errors.",
            "Filter for historical period with 0 records",
            "Reliability",
            "Verified: Handles division by zero safely and renders clean empty charts."
        )
    ]
