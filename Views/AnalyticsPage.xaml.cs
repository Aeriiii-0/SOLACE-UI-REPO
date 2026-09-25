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
            TxtMaleCircle.Text = male.ToString();

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
                if (used.Contains(kvp.Key)) continue;
                rows.Add(new ChartRow
                {
                    Label = kvp.Key + " (unmapped)",
                    Count = kvp.Value,
                    Pct = total > 0 ? Math.Round(kvp.Value * 100.0 / total, 0).ToString("0") + "%" : "—",
                    BarColor = brush
                });
            }

            return rows;
        }

        private void bindBucketedBars(ItemsControl chart, Dictionary<string, int> data, (string key, string label)[] knownOrder, string hexColor)
        {
            chart.ItemsSource = BuildBucketedRows(data, knownOrder, hexColor);
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

            double canvasH = BarangayCanvas.ActualHeight > 0 ? BarangayCanvas.ActualHeight : 160;
            double canvasW = BarangayCanvas.ActualWidth  > 0 ? BarangayCanvas.ActualWidth  : 400;
            int    maxCount = groups.Max(g => g.Count());
            int    n        = groups.Count;
            double totalBarW = canvasW * 0.65;
            double barW     = Math.Floor(totalBarW / n);
            double gap      = (canvasW - barW * n) / (n + 1);
            double chartH   = canvasH - 2;

            var gridBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xD8, 0xDE));
            for (int step = 1; step <= 4; step++)
            {
                double gy = chartH * (1 - step / 4.0);
                BarangayCanvas.Children.Add(new Line
                {
                    X1 = 0, X2 = canvasW, Y1 = gy, Y2 = gy,
                    Stroke = gridBrush, StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection(new double[] { 4, 3 })
                });
            }

            var fill = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
            var hov  = new SolidColorBrush(Color.FromRgb(0x9B, 0x30, 0x60));

            for (int i = 0; i < n; i++)
            {
                double barH = Math.Max(4, groups[i].Count() * chartH / maxCount);
                double x    = gap + i * (barW + gap);
                double y    = chartH - barH;
                var rect = new Rectangle
                {
                    Width = barW, Height = barH, Fill = fill,
                    RadiusX = 4, RadiusY = 4,
                    Tag     = groups[i].Key, Cursor = Cursors.Hand,
                    ToolTip = groups[i].Key + "  ·  " + groups[i].Count() + " record(s)"
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                rect.MouseEnter += (s, e) => ((Rectangle)s).Fill = hov;
                rect.MouseLeave += (s, e) => ((Rectangle)s).Fill = fill;
                BarangayCanvas.Children.Add(rect);

                var lbl = new TextBlock
                {
                    Text = groups[i].Count().ToString(),
                    FontSize = 9, FontWeight = FontWeights.SemiBold,
                    Foreground = fill, FontFamily = new FontFamily("Segoe UI")
                };
                Canvas.SetLeft(lbl, x + barW / 2 - 4);
                Canvas.SetTop(lbl, Math.Max(0, y - 14));
                BarangayCanvas.Children.Add(lbl);
            }

            BarangayLabels.ItemsSource = groups.Select(g => new ChartRow { Label = g.Key }).ToList();
        }
        */

        
        private void RecentRecord_Click(object sender, MouseButtonEventArgs e)
        {
            ToastNotification.Show("Unavailable", "Recent records aren't available yet.", ToastType.Info);
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _cache.Remove((_filterYear, _filterMonth, _filterBarangay ?? "ALL"));
            await LoadAndBuildAnalyticsAsync();
        }

        
        private async void ExportXlsx_Click(object sender, RoutedEventArgs e)
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
            public string Label { get; set; }
            public int Count { get; set; }
            public string Pct { get; set; }
            public SolidColorBrush BarColor { get; set; }
        }

        
    }
}