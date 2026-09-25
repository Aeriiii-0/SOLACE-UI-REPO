using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;

namespace SOLUM_UI
{
    public partial class AnalyticsPage : Page
    {
        private int _filterYear;
        private int _filterMonth;
        private string _filterBarangay = "ALL";
        private string _currentPeriod = "";
        private MonthlyAnalyticsDto _current;

        private readonly Dictionary<(int year, int month, string barangay), MonthlyAnalyticsDto> _cache = new Dictionary<(int, int, string), MonthlyAnalyticsDto>();


        private static readonly (string key, string label)[] AgeBracketOrder =
        {
            ("19_AND_BELOW", "19 & below"),
            ("20_39",        "20 – 39"),
            ("40_59",        "40 – 59"),
            ("60_AND_ABOVE", "60 and above"),
        };

        private static readonly (string key, string label)[] IncomeBracketOrder =
        {
            ("BELOW_MINIMUM_WAGE",          "Below Min. Wage"),
            ("MINIMUM_WAGE_PLUS1_TO_20833", "Min.+1 – ₱20,833"),
            ("20834_AND_ABOVE",             "₱20,834 and above"),
        };

        private static readonly (string key, string label)[] DependentsAgeBracketOrder =
        {
            ("6_AND_BELOW",  "6 & below"),
            ("7_22",         "7 – 22"),
            ("22_AND_ABOVE", "22 and above"),
        };

        private static readonly (string key, string label)[] EmploymentStatusOrder =
        {
            ("employed",      "Employed"),
            ("self_employed", "Self-Employed"),
            ("not_employed",  "Not Employed"),
        };

        private static readonly (string key, string label)[] CivilStatusOrder =
        {
            ("Single",    "Single"),
            ("Married",   "Married"),
            ("Widowed",   "Widowed"),
            ("Separated", "Sep./Annul."),
            ("Annulled",  "Sep./Annul."),
        };

        private static readonly (string key, string label)[] CategoryOrder =
        {
            ("A1", "a1. Consequence of rape"),
            ("A2", "a2. Widow/widower"),
            ("A3", "a3. Spouse of PDL"),
            ("A4", "a4. Spouse of PWD"),
            ("A5", "a5. Separated/de facto"),
            ("A6", "a6. Annulled"),
            ("A7", "a7. Abandoned"),
            ("B",  "b. Spouse/Relative of OFW"),
            ("C",  "c. Unmarried person"),
            ("D",  "d. Guardian/Adoptive/Foster"),
            ("E",  "e. Relative"),
            ("F",  "f. Pregnant woman"),
        };

        public AnalyticsPage()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                _filterYear = DateTime.Today.Year;
                _filterMonth = DateTime.Today.Month;
                PopulateFilterDropdowns();
                await LoadAndBuildAnalyticsAsync();

              
            };
        }

        private async Task<BaseResponse<MonthlyAnalyticsDto>> GetMonthlyAnalyticsCachedAsync(int year, int month, string barangay)
        {
            var key = (year, month, barangay ?? "ALL");

            if (_cache.TryGetValue(key, out var cached))
            {
                return new BaseResponse<MonthlyAnalyticsDto> { Succeeded = true, Data = cached };
            }

            var response = await AnalyticsApiService.Instance.GetMonthlyAnalyticsAsync(year, month, barangay);

            if (response.Succeeded && response.Data != null)
            {
                _cache[key] = response.Data;
            }

            return response;
        }

        private static readonly string[] Barangays =
            {
                "Biñan", "Bungahan", "Canlalay", "Casile", "De La Paz", "Ganado",
                "Langkiwa", "Loma", "Malaban", "Malamig", "Mamplasan", "Platero",
                "Poblacion", "San Antonio", "San Francisco", "San Jose", "San Vicente",
                "Santo Domingo", "Santo Niño", "Santo Tomas", "Soro-Soro", "Timbao",
                "Tubigan", "Zapote"
            };

        private void PopulateFilterDropdowns()
        {
            CmbFilterYear.Items.Clear();
            int thisYear = DateTime.Today.Year;
            for (int y = thisYear; y >= thisYear - 4; y--)
                CmbFilterYear.Items.Add(new ComboBoxItem { Content = y.ToString(), Tag = y });
            CmbFilterYear.SelectedItem = CmbFilterYear.Items
                .Cast<ComboBoxItem>().FirstOrDefault(i => (int)i.Tag == _filterYear);

            CmbFilterMonth.Items.Clear();
            for (int m = 1; m <= 12; m++)
                CmbFilterMonth.Items.Add(new ComboBoxItem { Content = new DateTime(2000, m, 1).ToString("MMMM"), Tag = m });
            CmbFilterMonth.SelectedItem = CmbFilterMonth.Items
                .Cast<ComboBoxItem>().FirstOrDefault(i => (int)i.Tag == _filterMonth);

            CmbFilterBarangay.Items.Clear();
            CmbFilterBarangay.Items.Add(new ComboBoxItem { Content = "All Barangays", Tag = "ALL" });
            foreach (var b in Barangays)
                CmbFilterBarangay.Items.Add(new ComboBoxItem { Content = b, Tag = b });
            CmbFilterBarangay.SelectedItem = CmbFilterBarangay.Items
                .Cast<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == _filterBarangay);
        }

        private void FilterDrop_Click(object sender, RoutedEventArgs e) => FilterPopup.IsOpen = true;

        private async void FilterApply_Click(object sender, RoutedEventArgs e)
        {
            _filterYear = (int)((CmbFilterYear.SelectedItem as ComboBoxItem)?.Tag ?? DateTime.Today.Year);
            _filterMonth = (int)((CmbFilterMonth.SelectedItem as ComboBoxItem)?.Tag ?? DateTime.Today.Month);
            _filterBarangay = (string)((CmbFilterBarangay.SelectedItem as ComboBoxItem)?.Tag ?? "ALL"); // NEW
            FilterPopup.IsOpen = false;
            UpdateFilterBadge();
            await LoadAndBuildAnalyticsAsync();
        }

        private async void FilterReset_Click(object sender, RoutedEventArgs e)
        {
            _filterYear = DateTime.Today.Year;
            _filterMonth = DateTime.Today.Month;
            _filterBarangay = "ALL";
            PopulateFilterDropdowns();
            FilterPopup.IsOpen = false;
            UpdateFilterBadge();
            await LoadAndBuildAnalyticsAsync();
        }

        private void UpdateFilterBadge()
        {
            string period = new DateTime(2000, _filterMonth, 1).ToString("MMMM") + " " + _filterYear;
            if (_filterBarangay != "ALL") period += " · " + _filterBarangay;
            TxtActiveFilter.Text = period;
            ActiveFilterBadge.Visibility = Visibility.Visible;
        }


        private async Task LoadAndBuildAnalyticsAsync()
        {
            var response = await GetMonthlyAnalyticsCachedAsync(_filterYear, _filterMonth, _filterBarangay); 

            if (!response.Succeeded || response.Data == null)
            {
                string err = response.Errors != null && response.Errors.Count > 0
                    ? string.Join("\n", response.Errors)
                    : "Unable to load analytics for this period.";
                ToastNotification.Show("Load Failed", err, ToastType.Warning);
                return;
            }

            _current = response.Data;
            BuildAnalytics(_current);
        }

        private void BuildAnalytics(MonthlyAnalyticsDto d)
        {
            string period = new DateTime(2000, d.Month, 1).ToString("MMMM") + " " + d.Year;
            TxtTotalRecords.Text = d.TotalSoloParents.ToString();
            TxtPeriodLabel.Text = period;
            _currentPeriod = period;
            TxtLastRefresh.Text = "Updated " + DateTime.Now.ToString("MMM d, h:mm tt");

            int female = d.Sex.TryGetValue("Female", out var f) ? f : 0;
            int male = d.Sex.TryGetValue("Male", out var m) ? m : 0;
            TxtFemaleCircle.Text = female.ToString();
            TxtMaleCircle.Text   = male.ToString();
            DrawGenderDonut(female, male);

            int totKids = d.DependentsAgeBrackets.Values.Sum();
            TxtTotalChildren.Text = totKids.ToString();
            TxtAvgChildren.Text = d.TotalSoloParents > 0
                ? Math.Round((double)totKids / d.TotalSoloParents, 1).ToString("0.0") + " avg."
                : "—";

            bindBucketedBars(AgeChart, d.AgeBrackets, AgeBracketOrder, "#702943");
            bindBucketedBars(CivilStatusChart, d.CivilStatus, CivilStatusOrder, "#9B3060");
            bindBucketedBars(EmploymentChart, d.EmploymentStatus, EmploymentStatusOrder, "#C4788E");
            bindBucketedBars(IncomeChart, d.MonthlyIncomeBrackets, IncomeBracketOrder, "#702943");
            bindBucketedBars(DependantsAgeChart, d.DependentsAgeBrackets, DependentsAgeBracketOrder, "#F57F17");

            var catRows = BuildBucketedRows(d.Categories, CategoryOrder, "#702943");
            int half = (catRows.Count + 1) / 2;
            CategoryChartLeft.ItemsSource = catRows.Take(half).ToList();
            CategoryChartRight.ItemsSource = catRows.Skip(half).ToList();

            // Barangay breakdown chart and recent-records list are left empty —
            // see the commented-out methods below for the original logic and
            // what each would need to work again.
            BarangayCanvas.Children.Clear();
            BarangayLabels.ItemsSource = null;
            RecentList.ItemsSource = null;
        }

        private List<ChartRow> BuildBucketedRows(Dictionary<string, int> data, (string key, string label)[] knownOrder, string hexColor)
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            var brush = new SolidColorBrush(color);
            int total = data.Values.Sum();
            var used = new HashSet<string>(knownOrder.Select(k => k.key), StringComparer.OrdinalIgnoreCase);

            var rows = knownOrder.Select(k =>
            {
                int count = data.TryGetValue(k.key, out var c) ? c : 0;
                return new ChartRow
                {
                    Label = k.label,
                    Count = count,
                    Pct = total > 0 ? Math.Round(count * 100.0 / total, 0).ToString("0") + "%" : "—",
                    BarColor = brush
                };
            }).ToList();

            foreach (var kvp in data)
            {
                ("a1. Consequence of rape",               d.Count(r => r.CircumstanceA1)),
                ("a2. Widow/widower",                     d.Count(r => r.CircumstanceA2)),
                ("a3. Spouse of PDL",                     d.Count(r => r.CircumstanceA3)),
                ("a4. Spouse of PWD",                     d.Count(r => r.CircumstanceA4)),
                ("a5. Separated/de facto",                d.Count(r => r.CircumstanceA5)),
                ("a6. Annulled",                          d.Count(r => r.CircumstanceA6)),
                ("a7. Abandoned",                         d.Count(r => r.CircumstanceA7)),
                ("b. Spouse/Relative of OFW",             d.Count(r => r.CircumstanceB)),
                ("c. Unmarried person",                   d.Count(r => r.CircumstanceC)),
                ("d. Guardian/Adoptive/Foster",           d.Count(r => r.CircumstanceD)),
                ("e. Relative",                           d.Count(r => r.CircumstanceE)),
                ("f. Pregnant woman",                     d.Count(r => r.CircumstanceF)),
            };
            int catTotal = catRows.Sum(x => x.Item2);
            int catMax   = catRows.Length > 0 ? catRows.Max(x => x.Item2) : 1;
            CategoryChartLeft.ItemsSource  = catRows.Take(6).Select(r => new ChartRow
            {
                Label      = r.Item1,
                Count      = r.Item2,
                Pct        = catTotal > 0 ? Math.Round(r.Item2 * 100.0 / catTotal, 0).ToString("0") + "%" : "—",
                BarColor   = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43)),
                BarWidthPx = catMax > 0 ? Math.Max(4, r.Item2 * 180.0 / catMax) : 0,
                GradStart  = "#702943",
                GradEnd    = "#A8415E",
            }).ToList();
            CategoryChartRight.ItemsSource = catRows.Skip(6).Select(r => new ChartRow
            {
                Label      = r.Item1,
                Count      = r.Item2,
                Pct        = catTotal > 0 ? Math.Round(r.Item2 * 100.0 / catTotal, 0).ToString("0") + "%" : "—",
                BarColor   = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43)),
                BarWidthPx = catMax > 0 ? Math.Max(4, r.Item2 * 180.0 / catMax) : 0,
                GradStart  = "#702943",
                GradEnd    = "#A8415E",
            }).ToList();

            DrawBarangayChart(d);

            var recent = d.OrderByDescending(r => r.LastUpdated).Take(12).ToList();
            RecentList.ItemsSource = recent.Select(r =>
            {
                string initials = "";
                var parts = (r.Name ?? "").Split(new char[]{' ', ','}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 1) initials += parts[0][0];
                if (parts.Length >= 2) initials += parts[1][0];
                return new RecentRow
                {
                    SpId      = r.Id,
                    Name      = r.Name,
                    Barangay  = r.Barangay,
                    DateLabel = r.LastUpdated.ToString("MMM d, yyyy"),
                    Status    = r.Status,
                    Initials  = initials.ToUpperInvariant(),
                    StatusBg  = r.Status == "Valid"
                        ? new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9))
                        : new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2)),
                    StatusFg  = r.Status == "Valid"
                        ? new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60))
                        : new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99))
                };
            }).ToList();
        }

        private void bindBucketedBars(ItemsControl chart, Dictionary<string, int> data, (string key, string label)[] knownOrder, string hexColor)
        {
            int    total    = rows.Sum(r => r.count);
            int    maxCount = rows.Length > 0 ? rows.Max(r => r.count) : 1;
            var    c        = (Color)ColorConverter.ConvertFromString(hexColor);
            string gradEnd  = hexColor;
            // lighter tint for gradient end
            var lighter = Color.FromRgb(
                (byte)Math.Min(255, c.R + 60),
                (byte)Math.Min(255, c.G + 40),
                (byte)Math.Min(255, c.B + 50));

            chart.ItemsSource = rows.Select(r => new ChartRow
            {
                Label      = r.label,
                Count      = r.count,
                Pct        = total > 0 ? Math.Round(r.count * 100.0 / total, 0).ToString("0") + "%" : "—",
                BarColor   = new SolidColorBrush(c),
                BarWidthPx = maxCount > 0 ? Math.Max(4, r.count * 180.0 / maxCount) : 0,
                GradStart  = hexColor,
                GradEnd    = "#" + lighter.R.ToString("X2") + lighter.G.ToString("X2") + lighter.B.ToString("X2"),
            }).ToList();
        }

        /* OLD: per-barangay breakdown chart. Needs a data source that returns counts
           PER barangay for the period — the current endpoint only returns totals for
           ONE barangay (or "ALL") at a time, so this can't be fed directly anymore.
           Either: (a) call GetMonthlyAnalyticsAsync once per known barangay and
           assemble the bars from d.TotalSoloParents of each response, or (b) get a
           dedicated "grouped by barangay" endpoint from the backend.

        private void DrawBarangayChart(List<SoloParentRecord> data)
        {
            BarangayCanvas.Children.Clear();
            BarangayLabels.ItemsSource = null;

            var groups = data.GroupBy(r => r.Barangay ?? "Unknown")
                             .OrderByDescending(g => g.Count())
                             .Take(10).ToList();
            if (!groups.Any()) return;

            double canvasH  = BarangayCanvas.ActualHeight > 0 ? BarangayCanvas.ActualHeight : 160;
            double canvasW  = BarangayCanvas.ActualWidth  > 0 ? BarangayCanvas.ActualWidth  : 400;
            int    maxCount = groups.Max(g => g.Count());
            int    n        = groups.Count;
            double barW     = Math.Floor((canvasW * 0.72) / n);
            double gap      = (canvasW - barW * n) / (n + 1);
            double chartH   = canvasH - 4;

            // grid lines
            var gridBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xD8, 0xDE));
            for (int step = 1; step <= 4; step++)
            {
                double gy = chartH * (1.0 - step / 4.0);
                BarangayCanvas.Children.Add(new Line
                {
                    X1 = 0, X2 = canvasW, Y1 = gy, Y2 = gy,
                    Stroke = gridBrush, StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection(new double[] { 4, 3 })
                });
            }

            // color palette cycling through the brand palette
            Color[] palette =
            {
                Color.FromRgb(0x70, 0x29, 0x43),
                Color.FromRgb(0x9B, 0x30, 0x60),
                Color.FromRgb(0xC4, 0x78, 0x8E),
                Color.FromRgb(0xA8, 0x41, 0x5E),
                Color.FromRgb(0x80, 0x35, 0x50),
                Color.FromRgb(0xB5, 0x5A, 0x78),
                Color.FromRgb(0xD4, 0xA0, 0xB0),
                Color.FromRgb(0x87, 0x2D, 0x4A),
                Color.FromRgb(0x70, 0x29, 0x43),
                Color.FromRgb(0x9B, 0x30, 0x60),
            };

            for (int i = 0; i < n; i++)
            {
                double barH   = Math.Max(6, groups[i].Count() * chartH / maxCount);
                double x      = gap + i * (barW + gap);
                double y      = chartH - barH;
                var    fillC  = palette[i % palette.Length];
                var    lightC = Color.FromRgb(
                    (byte)Math.Min(255, fillC.R + 55),
                    (byte)Math.Min(255, fillC.G + 35),
                    (byte)Math.Min(255, fillC.B + 45));

                var rect = new Rectangle
                {
                    Width   = barW,
                    Height  = barH,
                    RadiusX = 5,
                    RadiusY = 5,
                    Tag     = groups[i].Key,
                    Cursor  = Cursors.Hand,
                    ToolTip = groups[i].Key + "  ·  " + groups[i].Count() + " record(s)",
                    Fill    = new LinearGradientBrush(
                        fillC, lightC,
                        new Point(0, 1), new Point(0, 0))
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);

                var captureFill  = new SolidColorBrush(fillC);
                var captureHover = new SolidColorBrush(lightC);
                rect.MouseEnter += (s, e) => ((Rectangle)s).Fill = captureHover;
                rect.MouseLeave += (s, e) => ((Rectangle)s).Fill = new LinearGradientBrush(
                    fillC, lightC, new Point(0, 1), new Point(0, 0));
                BarangayCanvas.Children.Add(rect);

                // count label above bar
                var lbl = new TextBlock
                {
                    Text       = groups[i].Count().ToString(),
                    FontSize   = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(fillC),
                    FontFamily = new FontFamily("Segoe UI")
                };
                Canvas.SetLeft(lbl, x + barW / 2 - 5);
                Canvas.SetTop(lbl, Math.Max(0, y - 16));
                BarangayCanvas.Children.Add(lbl);
            }

            BarangayLabels.ItemsSource = groups.Select(g => new ChartRow { Label = g.Key }).ToList();
        }
        */

        
        private void RecentRecord_Click(object sender, MouseButtonEventArgs e)
        {
            ToastNotification.Show("Unavailable", "Recent records aren't available yet.", ToastType.Info);
        }

        private void DrawGenderDonut(int female, int male)
        {
            GenderDonut.Children.Clear();
            double cx = 40, cy = 40, r = 34, innerR = 22;
            int total = female + male;
            if (total == 0) return;

            double femalePct = female / (double)total;
            double sweep     = femalePct * 360.0;
            if (sweep < 1)  sweep = 1;
            if (sweep > 359) sweep = 359;

            // helper: point on circle
            System.Func<double, double, System.Windows.Point> pt = (angle, rad2) =>
            {
                double radians = (angle - 90) * Math.PI / 180.0;
                return new System.Windows.Point(cx + rad2 * Math.Cos(radians), cy + rad2 * Math.Sin(radians));
            };

            // female arc (brand primary)
            var femArc = BuildArcDonut(cx, cy, r, innerR, 0, sweep,
                Color.FromRgb(0x70, 0x29, 0x43), Color.FromRgb(0xA8, 0x41, 0x5E));
            GenderDonut.Children.Add(femArc);

            // male arc
            var maleArc = BuildArcDonut(cx, cy, r, innerR, sweep, 360.0 - sweep,
                Color.FromRgb(0xD4, 0xA0, 0xB0), Color.FromRgb(0xE8, 0xC8, 0xD4));
            GenderDonut.Children.Add(maleArc);

            // center label
            var centerLbl = new TextBlock
            {
                Text              = total.ToString(),
                FontFamily        = new FontFamily("Segoe UI"),
                FontSize          = 13,
                FontWeight        = FontWeights.Bold,
                Foreground        = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            };
            Canvas.SetLeft(centerLbl, cx - 12);
            Canvas.SetTop(centerLbl,  cy - 9);
            GenderDonut.Children.Add(centerLbl);
        }

        private static UIElement BuildArcDonut(double cx, double cy, double r, double innerR,
            double startDeg, double sweepDeg, Color c1, Color c2)
        {
            if (sweepDeg <= 0) return new Path();

            bool isLarge = sweepDeg > 180;

            double r1 = (startDeg - 90) * Math.PI / 180.0;
            double r2 = (startDeg + sweepDeg - 90) * Math.PI / 180.0;

            var outerStart = new System.Windows.Point(cx + r * Math.Cos(r1), cy + r * Math.Sin(r1));
            var outerEnd   = new System.Windows.Point(cx + r * Math.Cos(r2), cy + r * Math.Sin(r2));
            var innerEnd   = new System.Windows.Point(cx + innerR * Math.Cos(r2), cy + innerR * Math.Sin(r2));
            var innerStart = new System.Windows.Point(cx + innerR * Math.Cos(r1), cy + innerR * Math.Sin(r1));

            var figure = new PathFigure { StartPoint = outerStart, IsClosed = true };
            figure.Segments.Add(new ArcSegment(outerEnd, new System.Windows.Size(r, r), 0, isLarge,
                SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(innerEnd, true));
            figure.Segments.Add(new ArcSegment(innerStart, new System.Windows.Size(innerR, innerR), 0, isLarge,
                SweepDirection.Counterclockwise, true));

            var geo = new PathGeometry();
            geo.Figures.Add(figure);

            return new Path
            {
                Data = geo,
                Fill = new LinearGradientBrush(c1, c2,
                    new System.Windows.Point(0, 0), new System.Windows.Point(1, 1))
            };
        }

        private void ExportXlsx_Click(object sender, RoutedEventArgs e)
        {
            var saveDlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "SoloParents_Quarterly_" + DateTime.Now.ToString("yyyyMMdd"),
                DefaultExt = ".xlsx",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx"
            };
            if (saveDlg.ShowDialog() != true) return;

            // Derive the quarter from the currently selected month
            int q = (_filterMonth - 1) / 3 + 1;
            int startMonth = (q - 1) * 3 + 1;
            var months = new[] { startMonth, startMonth + 1, startMonth + 2 };

            var results = new List<MonthlyAnalyticsDto>();
            foreach (var mo in months)
            {
                var resp = await GetMonthlyAnalyticsCachedAsync(_filterYear, mo, _filterBarangay);
                if (!resp.Succeeded || resp.Data == null)
                {
                    string err = resp.Errors != null && resp.Errors.Count > 0
                        ? string.Join("\n", resp.Errors)
                        : $"Unable to load data for {new DateTime(2000, mo, 1):MMMM} {_filterYear}.";
                    ToastNotification.Show("Export Failed", err, ToastType.Warning);
                    return;
                }
                results.Add(resp.Data);
            }

            string periodLabel = new DateTime(2000, months[2], 1).ToString("MMMM").ToUpper() + " " + _filterYear;

            var mw = Window.GetWindow(this) as MainWindow;
            if (mw != null) mw.MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 };

            double w = mw?.Width ?? 1280, h = mw?.Height ?? 800, l = mw?.Left ?? 0, t = mw?.Top ?? 0;
            var optsDlg = new ExportOptionsDialog(System.IO.Path.GetFileName(saveDlg.FileName), w, h, l, t)
            { Owner = mw ?? Window.GetWindow(this) };
            optsDlg.ShowDialog();

            if (mw != null) mw.MainContent.Effect = null;
            if (!optsDlg.Confirmed) return;

            try
            {
                AnalyticsExportService.ExportQuarterlySummary(
                    saveDlg.FileName,
                    results[0], results[1], results[2],
                    periodLabel,
                    MainWindow.CurrentUserName,   // NEW — real logged-in user, not a hardcoded name
                    password: optsDlg.Password);

                ToastNotification.Show("Exported", "Saved to " + System.IO.Path.GetFileName(saveDlg.FileName), ToastType.Success);
                if (optsDlg.OpenAfter)
                    System.Diagnostics.Process.Start(saveDlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private class ChartRow
        {
            public string          Label      { get; set; }
            public int             Count      { get; set; }
            public string          Pct        { get; set; }
            public SolidColorBrush BarColor   { get; set; }
            public double          BarWidthPx { get; set; }
            public string          GradStart  { get; set; }
            public string          GradEnd    { get; set; }
        }

        private class RecentRow
        {
            public string          SpId      { get; set; }
            public string          Name      { get; set; }
            public string          Barangay  { get; set; }
            public string          DateLabel { get; set; }
            public string          Status    { get; set; }
            public string          Initials  { get; set; }
            public SolidColorBrush StatusBg  { get; set; }
            public SolidColorBrush StatusFg  { get; set; }
        }
    }
}

