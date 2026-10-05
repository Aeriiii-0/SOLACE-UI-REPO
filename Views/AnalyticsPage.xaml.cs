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
        private string _filterType = "Monthly"; // "Yearly", "Quarterly", "Monthly", "Custom"
        private int _filterYear;
        private int _filterMonth;
        private int _filterQuarter = 1;
        private DateTime? _startDate = null;
        private DateTime? _endDate = null;
        private string _filterBarangay = "ALL";
        private string _currentPeriod = "";
        private MonthlyAnalyticsDto _current;
        private BarangayAnalyticsResponseDto _currentBreakdown;

        private readonly Dictionary<(int year, int month, string barangay), MonthlyAnalyticsDto> _cache = new Dictionary<(int, int, string), MonthlyAnalyticsDto>();

        private static readonly (string key, string label)[] AgeBracketOrder =
        {
            ("19_AND_BELOW", "19 & below"),
            ("20_39",        "20 – 39"),
            ("40_59",        "40 – 59"),
            ("60_AND_ABOVE", "60 and above"),
        };

        // CHANGED: API returns MIN_WAGE_PLUS1_TO_20833
        private static readonly (string key, string label)[] IncomeBracketOrder =
        {
            ("BELOW_MINIMUM_WAGE",      "Below Min. Wage"),     // verify against backend
            ("MIN_WAGE_PLUS1_TO_20833", "Min.+1 – ₱20,833"),
            ("20834_AND_ABOVE",         "₱20,834 and above"),   // verify against backend
        };

        // CHANGED: API returns 7_TO_22
        private static readonly (string key, string label)[] DependentsAgeBracketOrder =
        {
            ("6_AND_BELOW",  "6 & below"),      // verify against backend
            ("7_TO_22",      "7 – 22"),
            ("22_AND_ABOVE", "22 and above"),   // verify against backend
        };

        private static readonly (string key, string label)[] EmploymentStatusOrder =
        {
            ("employed",      "Employed"),
            ("self_employed", "Self-Employed"),
            ("not_employed",  "Not Employed"),
        };

        // CHANGED: Separated + Annulled are merged into SEP_ANNUL in BuildAnalytics
        private static readonly (string key, string label)[] CivilStatusOrder =
        {
            ("Single",    "Single"),
            ("Married",   "Married"),
            ("Widowed",   "Widowed"),
            ("SEP_ANNUL", "Sep./Annul."),
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

        // NEW: sector breakdowns
        private static readonly (string key, string label)[] LgbtOrder =
        {
            ("LGBT",     "LGBT"),
            ("Non-LGBT", "Non-LGBT"),
        };

        private static readonly (string key, string label)[] PantawidOrder =
        {
            ("Beneficiary",     "Pantawid Beneficiary"),
            ("Non-Beneficiary", "Non-Beneficiary"),
        };

        private static readonly (string key, string label)[] IndigenousOrder =
        {
            ("Indigenous",     "Indigenous"),
            ("Non-Indigenous", "Non-Indigenous"),
        };

        private static readonly string[] Barangays =
        {
            "Biñan", "Bungahan", "Canlalay", "Casile", "De La Paz", "Ganado",
            "Langkiwa", "Loma", "Malaban", "Malamig", "Mamplasan", "Platero",
            "Poblacion", "San Antonio", "San Francisco", "San Jose", "San Vicente",
            "Santo Domingo", "Santo Niño", "Santo Tomas", "Soro-Soro", "Timbao",
            "Tubigan", "Zapote"
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

        private void PopulateFilterDropdowns()
        {
            // Populate Filter Type dropdown
            CmbFilterType.Items.Clear();
            CmbFilterType.Items.Add(new ComboBoxItem { Content = "Per Month", Tag = "Monthly" });
            CmbFilterType.Items.Add(new ComboBoxItem { Content = "Quarterly", Tag = "Quarterly" });
            CmbFilterType.Items.Add(new ComboBoxItem { Content = "Yearly", Tag = "Yearly" });
            CmbFilterType.Items.Add(new ComboBoxItem { Content = "Custom Range", Tag = "Custom" });
            CmbFilterType.SelectedItem = CmbFilterType.Items
                .Cast<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == _filterType) 
                ?? CmbFilterType.Items[0];

            // Populate Year dropdown
            CmbFilterYear.Items.Clear();
            int thisYear = DateTime.Today.Year;
            for (int y = thisYear; y >= thisYear - 4; y--)
                CmbFilterYear.Items.Add(new ComboBoxItem { Content = y.ToString(), Tag = y });
            CmbFilterYear.SelectedItem = CmbFilterYear.Items
                .Cast<ComboBoxItem>().FirstOrDefault(i => (int)i.Tag == _filterYear)
                ?? CmbFilterYear.Items[0];

            // Populate Month dropdown
            CmbFilterMonth.Items.Clear();
            for (int m = 1; m <= 12; m++)
                CmbFilterMonth.Items.Add(new ComboBoxItem { Content = new DateTime(2000, m, 1).ToString("MMMM"), Tag = m });
            CmbFilterMonth.SelectedItem = CmbFilterMonth.Items
                .Cast<ComboBoxItem>().FirstOrDefault(i => (int)i.Tag == _filterMonth)
                ?? CmbFilterMonth.Items[0];

            // Populate Quarter dropdown
            CmbFilterQuarter.Items.Clear();
            CmbFilterQuarter.Items.Add(new ComboBoxItem { Content = "Q1 (Jan – Mar)", Tag = 1 });
            CmbFilterQuarter.Items.Add(new ComboBoxItem { Content = "Q2 (Apr – Jun)", Tag = 2 });
            CmbFilterQuarter.Items.Add(new ComboBoxItem { Content = "Q3 (Jul – Sep)", Tag = 3 });
            CmbFilterQuarter.Items.Add(new ComboBoxItem { Content = "Q4 (Oct – Dec)", Tag = 4 });
            CmbFilterQuarter.SelectedItem = CmbFilterQuarter.Items
                .Cast<ComboBoxItem>().FirstOrDefault(i => (int)i.Tag == _filterQuarter) 
                ?? CmbFilterQuarter.Items[0];

            // Custom Range default dates
            if (DpFilterStartDate != null)
                DpFilterStartDate.SelectedDate = _startDate ?? new DateTime(thisYear, 1, 1);
            if (DpFilterEndDate != null)
                DpFilterEndDate.SelectedDate = _endDate ?? DateTime.Today;

            // Populate Barangay dropdown
            CmbFilterBarangay.Items.Clear();
            CmbFilterBarangay.Items.Add(new ComboBoxItem { Content = "All Barangays", Tag = "ALL" });
            foreach (var b in Barangays)
                CmbFilterBarangay.Items.Add(new ComboBoxItem { Content = b, Tag = b });
            CmbFilterBarangay.SelectedItem = CmbFilterBarangay.Items
                .Cast<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == _filterBarangay);

            ApplyFilterVisibility(_filterType);
        }

        private void CmbFilterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFilterType?.SelectedItem is ComboBoxItem item && item.Tag is string selectedType)
            {
                ApplyFilterVisibility(selectedType);
            }
        }

        private void ApplyFilterVisibility(string filterType)
        {
            if (PnlFilterYear == null || PnlFilterQuarter == null || PnlFilterMonth == null || PnlFilterCustomRange == null)
                return;

            switch (filterType)
            {
                case "Yearly":
                    PnlFilterYear.Visibility = Visibility.Visible;
                    PnlFilterQuarter.Visibility = Visibility.Collapsed;
                    PnlFilterMonth.Visibility = Visibility.Collapsed;
                    PnlFilterCustomRange.Visibility = Visibility.Collapsed;
                    break;
                case "Quarterly":
                    PnlFilterYear.Visibility = Visibility.Visible;
                    PnlFilterQuarter.Visibility = Visibility.Visible;
                    PnlFilterMonth.Visibility = Visibility.Collapsed;
                    PnlFilterCustomRange.Visibility = Visibility.Collapsed;
                    break;
                case "Monthly":
                    PnlFilterYear.Visibility = Visibility.Visible;
                    PnlFilterQuarter.Visibility = Visibility.Collapsed;
                    PnlFilterMonth.Visibility = Visibility.Visible;
                    PnlFilterCustomRange.Visibility = Visibility.Collapsed;
                    break;
                case "Custom":
                    PnlFilterYear.Visibility = Visibility.Collapsed;
                    PnlFilterQuarter.Visibility = Visibility.Collapsed;
                    PnlFilterMonth.Visibility = Visibility.Collapsed;
                    PnlFilterCustomRange.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void FilterDrop_Click(object sender, RoutedEventArgs e) => FilterPopup.IsOpen = true;

        private async void FilterApply_Click(object sender, RoutedEventArgs e)
        {
            _filterType = (string)((CmbFilterType.SelectedItem as ComboBoxItem)?.Tag ?? "Monthly");
            _filterYear = (int)((CmbFilterYear.SelectedItem as ComboBoxItem)?.Tag ?? DateTime.Today.Year);
            _filterMonth = (int)((CmbFilterMonth.SelectedItem as ComboBoxItem)?.Tag ?? DateTime.Today.Month);
            _filterQuarter = (int)((CmbFilterQuarter.SelectedItem as ComboBoxItem)?.Tag ?? 1);
            _startDate = DpFilterStartDate?.SelectedDate;
            _endDate = DpFilterEndDate?.SelectedDate;
            _filterBarangay = (string)((CmbFilterBarangay.SelectedItem as ComboBoxItem)?.Tag ?? "ALL");

            FilterPopup.IsOpen = false;
            UpdateFilterBadge();
            await LoadAndBuildAnalyticsAsync();
        }

        private async void FilterReset_Click(object sender, RoutedEventArgs e)
        {
            _filterType = "Monthly";
            _filterYear = DateTime.Today.Year;
            _filterMonth = DateTime.Today.Month;
            _filterQuarter = (_filterMonth - 1) / 3 + 1;
            _startDate = null;
            _endDate = null;
            _filterBarangay = "ALL";
            PopulateFilterDropdowns();
            FilterPopup.IsOpen = false;
            UpdateFilterBadge();
            await LoadAndBuildAnalyticsAsync();
        }

        private void UpdateFilterBadge()
        {
            string period = "";
            switch (_filterType)
            {
                case "Yearly":
                    period = $"Year {_filterYear}";
                    break;
                case "Quarterly":
                    period = $"Q{_filterQuarter} {_filterYear}";
                    break;
                case "Monthly":
                    period = new DateTime(2000, _filterMonth, 1).ToString("MMMM") + " " + _filterYear;
                    break;
                case "Custom":
                    string startStr = _startDate?.ToString("MMM d, yyyy") ?? "Start";
                    string endStr = _endDate?.ToString("MMM d, yyyy") ?? "End";
                    period = $"{startStr} – {endStr}";
                    break;
            }

            if (_filterBarangay != "ALL") period += " · " + _filterBarangay;
            TxtActiveFilter.Text = period;
            ActiveFilterBadge.Visibility = Visibility.Visible;
        }

        private async Task LoadAndBuildAnalyticsAsync()
        {
            // Monthly analytics call (for demographic breakdowns)
            int queryMonth = _filterType == "Quarterly" ? ((_filterQuarter - 1) * 3 + 1) : _filterMonth;
            var monthlyTask = GetMonthlyAnalyticsCachedAsync(_filterYear, queryMonth, _filterBarangay);

            // Barangay breakdown call using revised API parameters
            Task<BaseResponse<BarangayAnalyticsResponseDto>> breakdownTask;
            if (_filterType == "Quarterly")
            {
                breakdownTask = AnalyticsApiService.Instance.GetBarangayBreakdownAsync(
                    quarter: _filterQuarter,
                    year: _filterYear,
                    barangay: _filterBarangay,
                    type: "Quarterly");
            }
            else if (_filterType == "Yearly")
            {
                breakdownTask = AnalyticsApiService.Instance.GetBarangayBreakdownAsync(
                    year: _filterYear,
                    barangay: _filterBarangay,
                    type: "Yearly");
            }
            else if (_filterType == "Custom")
            {
                breakdownTask = AnalyticsApiService.Instance.GetBarangayBreakdownAsync(
                    barangay: _filterBarangay,
                    type: "Custom",
                    startDate: _startDate,
                    endDate: _endDate);
            }
            else
            {
                breakdownTask = AnalyticsApiService.Instance.GetBarangayBreakdownAsync(
                    quarter: (_filterMonth - 1) / 3 + 1,
                    year: _filterYear,
                    barangay: _filterBarangay,
                    type: "Monthly");
            }

            await Task.WhenAll(monthlyTask, breakdownTask);

            var response = monthlyTask.Result;
            var breakdownResponse = breakdownTask.Result;

            if (!response.Succeeded || response.Data == null)
            {
                string err = response.Errors != null && response.Errors.Count > 0
                    ? string.Join("\n", response.Errors)
                    : "Unable to load analytics for this period.";
                ToastNotification.Show("Load Failed", err, ToastType.Warning);
                return;
            }

            _current = response.Data;
            _currentBreakdown = breakdownResponse?.Succeeded == true ? breakdownResponse.Data : null;
            BuildAnalytics(_current, _currentBreakdown);
        }

        private void BuildAnalytics(MonthlyAnalyticsDto d, BarangayAnalyticsResponseDto breakdown = null)
        {
            string period = "";
            if (breakdown != null && !string.IsNullOrWhiteSpace(breakdown.PeriodLabel))
            {
                period = breakdown.PeriodLabel;
            }
            else
            {
                switch (_filterType)
                {
                    case "Yearly":
                        period = $"Year {_filterYear}";
                        break;
                    case "Quarterly":
                        period = $"Q{_filterQuarter} {_filterYear}";
                        break;
                    case "Custom":
                        period = $"{_startDate:MMM d, yyyy} – {_endDate:MMM d, yyyy}";
                        break;
                    default:
                        period = new DateTime(2000, d.Month, 1).ToString("MMMM") + " " + d.Year;
                        break;
                }
            }

            int totalSP = (breakdown != null && breakdown.GrandTotalSoloParents > 0)
                ? breakdown.GrandTotalSoloParents
                : d.TotalSoloParents;
            TxtTotalRecords.Text = totalSP.ToString();

            // Active / Inactive counts from Breakdown or MonthlyAnalyticsDto
            int activeCount = (breakdown != null && breakdown.TotalActive > 0)
                ? breakdown.TotalActive
                : (d.ActiveSoloParents > 0 ? d.ActiveSoloParents : 0);

            int inactiveCount = (breakdown != null && breakdown.TotalInactive > 0)
                ? breakdown.TotalInactive
                : (d.InactiveSoloParents > 0 ? d.InactiveSoloParents : (totalSP - activeCount));
            if (inactiveCount < 0) inactiveCount = 0;

            TxtValidRecords.Text = activeCount.ToString();
            TxtValidRate.Text = totalSP > 0 ? Math.Round(activeCount * 100.0 / totalSP, 0).ToString("0") + "%" : "0%";

            TxtInactiveRecords.Text = inactiveCount.ToString();
            TxtInactiveRate.Text = totalSP > 0 ? Math.Round(inactiveCount * 100.0 / totalSP, 0).ToString("0") + "%" : "0%";

            // New registrations & Renewals from Breakdown or MonthlyAnalyticsDto
            int newRegCount = (breakdown != null && breakdown.TotalNewRegistrations > 0)
                ? breakdown.TotalNewRegistrations
                : d.NewRegistrations;

            int renewalsCount = (breakdown != null && breakdown.TotalRenewals > 0)
                ? breakdown.TotalRenewals
                : d.Renewals;

            TxtNewSpic.Text = newRegCount.ToString();
            TxtRenewedSpic.Text = renewalsCount.ToString();

            TxtPeriodLabel.Text = period;
            _currentPeriod = period;
            TxtLastRefresh.Text = "Updated " + DateTime.Now.ToString("MMM d, h:mm tt");

            int female = d.Sex != null && d.Sex.TryGetValue("Female", out var f) ? f : 0;
            int male = d.Sex != null && d.Sex.TryGetValue("Male", out var m) ? m : 0;
            TxtFemaleCircle.Text = female.ToString();
            TxtMaleCircle.Text = male.ToString();
            DrawGenderDonut(female, male);

            int totKids = d.DependentsAgeBrackets != null ? d.DependentsAgeBrackets.Values.Sum() : 0;
            TxtTotalChildren.Text = totKids.ToString();
            TxtAvgChildren.Text = totalSP > 0
                ? Math.Round((double)totKids / totalSP, 1).ToString("0.0") + " avg."
                : "—";

            // CHANGED: merge Separated + Annulled into one bucket
            var civil = new Dictionary<string, int>(d.CivilStatus);
            int sepAnnul = (civil.TryGetValue("Separated", out var sep) ? sep : 0)
                         + (civil.TryGetValue("Annulled", out var ann) ? ann : 0);
            civil.Remove("Separated");
            civil.Remove("Annulled");
            civil["SEP_ANNUL"] = sepAnnul;

            bindBucketedBars(AgeChart, d.AgeBrackets, AgeBracketOrder, "#702943");
            bindBucketedBars(CivilStatusChart, civil, CivilStatusOrder, "#9B3060");
            bindBucketedBars(EmploymentChart, d.EmploymentStatus, EmploymentStatusOrder, "#C4788E");
            bindBucketedBars(IncomeChart, d.MonthlyIncomeBrackets, IncomeBracketOrder, "#702943");
            bindBucketedBars(DependantsAgeChart, d.DependentsAgeBrackets, DependentsAgeBracketOrder, "#F57F17");

            // NEW: sector charts (need LgbtChart, PantawidChart, IndigenousChart in the XAML)
            bindBucketedBars(LgbtChart, d.Lgbt, LgbtOrder, "#702943");
            bindBucketedBars(PantawidChart, d.PantawidBeneficiary, PantawidOrder, "#9B3060");
            bindBucketedBars(IndigenousChart, d.Indigenous, IndigenousOrder, "#C4788E");

            var catRows = BuildBucketedRows(d.Categories, CategoryOrder, "#702943");
            int half = (catRows.Count + 1) / 2;
            CategoryChartLeft.ItemsSource = catRows.Take(half).ToList();
            CategoryChartRight.ItemsSource = catRows.Skip(half).ToList();

            if (breakdown != null && breakdown.Barangays != null && breakdown.Barangays.Any())
            {
                DrawBarangayChart(breakdown.Barangays);
            }
            else
            {
                BarangayCanvas.Children.Clear();
                BarangayLabels.ItemsSource = null;
            }
            RecentList.ItemsSource = null;
        }

        private List<ChartRow> BuildBucketedRows(Dictionary<string, int> data, (string key, string label)[] knownOrder, string hexColor)
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            var brush = new SolidColorBrush(color);
            int total = data.Values.Sum();

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

            return rows;
        }

        private void bindBucketedBars(ItemsControl chart, Dictionary<string, int> data, (string key, string label)[] knownOrder, string hexColor)
        {
            var rows = knownOrder.Select(k => new
            {
                label = k.label,
                count = data != null && data.TryGetValue(k.key, out var i) ? i : 0
            }).ToArray();

            int total = rows.Sum(r => r.count);
            int maxCount = rows.Length > 0 ? rows.Max(r => r.count) : 1;
            var c = (Color)ColorConverter.ConvertFromString(hexColor);

            var lighter = Color.FromRgb(
                (byte)Math.Min(255, c.R + 60),
                (byte)Math.Min(255, c.G + 40),
                (byte)Math.Min(255, c.B + 50));

            chart.ItemsSource = rows.Select(r => new ChartRow
            {
                Label = r.label,
                Count = r.count,
                Pct = total > 0 ? Math.Round(r.count * 100.0 / total, 0).ToString("0") + "%" : "—",
                BarColor = new SolidColorBrush(c),
                BarWidthPx = maxCount > 0 ? Math.Max(4, r.count * 180.0 / maxCount) : 0,
                GradStart = hexColor,
                GradEnd = "#" + lighter.R.ToString("X2") + lighter.G.ToString("X2") + lighter.B.ToString("X2"),
            }).ToList();
        }

        private void DrawBarangayChart(List<BarangayBreakdownItemDto> items)
        {
            BarangayCanvas.Children.Clear();
            BarangayLabels.ItemsSource = null;

            if (items == null || !items.Any()) return;

            var topItems = items.OrderByDescending(b => b.TotalSoloParents).Take(10).ToList();
            if (!topItems.Any()) return;

            double canvasH = BarangayCanvas.ActualHeight > 0 ? BarangayCanvas.ActualHeight : 160;
            double canvasW = BarangayCanvas.ActualWidth > 0 ? BarangayCanvas.ActualWidth : 400;
            int maxCount = Math.Max(1, topItems.Max(g => g.TotalSoloParents));
            int n = topItems.Count;
            double barW = Math.Max(12, Math.Floor((canvasW * 0.72) / n));
            double gap = Math.Max(6, (canvasW - barW * n) / (n + 1));
            double chartH = canvasH - 4;

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
                var item = topItems[i];
                double barH = Math.Max(6, item.TotalSoloParents * chartH / maxCount);
                double x = gap + i * (barW + gap);
                double y = chartH - barH;
                var fillC = palette[i % palette.Length];
                var lightC = Color.FromRgb(
                    (byte)Math.Min(255, fillC.R + 55),
                    (byte)Math.Min(255, fillC.G + 35),
                    (byte)Math.Min(255, fillC.B + 45));

                string tooltipText = $"{item.BarangayName}  ·  {item.TotalSoloParents} record(s) ({item.PercentageShare:F1}%)\n" +
                                     $"Active: {item.ActiveCount}  |  Inactive: {item.InactiveCount}\n" +
                                     $"New: {item.NewRegistrationsCount}  |  Renewals: {item.RenewalsCount}";

                var rect = new Rectangle
                {
                    Width = barW,
                    Height = barH,
                    RadiusX = 5,
                    RadiusY = 5,
                    Tag = item.BarangayName,
                    Cursor = Cursors.Hand,
                    ToolTip = tooltipText,
                    Fill = new LinearGradientBrush(
                        fillC, lightC,
                        new Point(0, 1), new Point(0, 0))
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);

                var captureHover = new SolidColorBrush(lightC);
                rect.MouseEnter += (s, e) => ((Rectangle)s).Fill = captureHover;
                rect.MouseLeave += (s, e) => ((Rectangle)s).Fill = new LinearGradientBrush(
                    fillC, lightC, new Point(0, 1), new Point(0, 0));
                BarangayCanvas.Children.Add(rect);

                var lbl = new TextBlock
                {
                    Text = item.TotalSoloParents.ToString(),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(fillC),
                    FontFamily = new FontFamily("Segoe UI")
                };
                Canvas.SetLeft(lbl, x + barW / 2 - 5);
                Canvas.SetTop(lbl, Math.Max(0, y - 16));
                BarangayCanvas.Children.Add(lbl);
            }

            BarangayLabels.ItemsSource = topItems.Select(g => new ChartRow { Label = g.BarangayName }).ToList();
        }

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
            double sweep = femalePct * 360.0;
            if (sweep < 1) sweep = 1;
            if (sweep > 359) sweep = 359;

            var femArc = BuildArcDonut(cx, cy, r, innerR, 0, sweep,
                Color.FromRgb(0x70, 0x29, 0x43), Color.FromRgb(0xA8, 0x41, 0x5E));
            GenderDonut.Children.Add(femArc);

            var maleArc = BuildArcDonut(cx, cy, r, innerR, sweep, 360.0 - sweep,
                Color.FromRgb(0xD4, 0xA0, 0xB0), Color.FromRgb(0xE8, 0xC8, 0xD4));
            GenderDonut.Children.Add(maleArc);

            var centerLbl = new TextBlock
            {
                Text = total.ToString(),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Canvas.SetLeft(centerLbl, cx - 12);
            Canvas.SetTop(centerLbl, cy - 9);
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
            var outerEnd = new System.Windows.Point(cx + r * Math.Cos(r2), cy + r * Math.Sin(r2));
            var innerEnd = new System.Windows.Point(cx + innerR * Math.Cos(r2), cy + innerR * Math.Sin(r2));
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
                    MainWindow.CurrentUserName,
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

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _cache.Remove((_filterYear, _filterMonth, _filterBarangay ?? "ALL"));
            await LoadAndBuildAnalyticsAsync();
        }

        private class ChartRow
        {
            public string Label { get; set; }
            public int Count { get; set; }
            public string Pct { get; set; }
            public SolidColorBrush BarColor { get; set; }
            public double BarWidthPx { get; set; }
            public string GradStart { get; set; }
            public string GradEnd { get; set; }
        }

        private class RecentRow
        {
            public string SpId { get; set; }
            public string Name { get; set; }
            public string Barangay { get; set; }
            public string DateLabel { get; set; }
            public string Status { get; set; }
            public string Initials { get; set; }
            public SolidColorBrush StatusBg { get; set; }
            public SolidColorBrush StatusFg { get; set; }
        }
    }
}
