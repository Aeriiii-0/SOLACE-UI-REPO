using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.ViewModels;
using System.Threading.Tasks;
using SOLUM_UI.Views.Dialogs;

namespace SOLUM_UI
{
    public partial class AnalyticsPage : Page
    {
        private AnalyticsViewModel _vm;
        private bool _redrawPending;
        private INotifyCollectionChanged _hookedBarangay;

        private static readonly TimeSpan DataFreshnessTtl = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan RefreshCooldown = TimeSpan.FromSeconds(2);
        private DateTime _lastLoadedTime = DateTime.MinValue;
        private DateTime _lastRefreshClickTime = DateTime.MinValue;
        private bool _hasLoadedData = false;

        private static readonly string[] AgeLabels = { "19 & below", "20-39", "40-59", "60 & above" };
        private static readonly string[] AgeProps = { "ParentAge19Below", "ParentAge20_39", "ParentAge40_59", "ParentAge60Above" };
        private static readonly string[] AgeColors = { "#7D2245", "#A14452", "#D9A3AB", "#EAD9DB" };

        public AnalyticsPage()
        {
            try
            {
                var resourceUri = new Uri("/Views/AnalyticsPage.xaml", UriKind.Relative);
                System.Windows.Application.LoadComponent(this, resourceUri);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"XAML load error: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Failed to load Analytics page XAML: {ex.Message}", "XAML Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _vm = new AnalyticsViewModel();
                DataContext = _vm;
                _vm.PropertyChanged += OnVmPropertyChanged;
                HookBarangayCollection();

                Loaded += async (s, e) =>
                {
                    InitFilterControls();
                    HookChartCanvases();

                    bool isStale = !_hasLoadedData || (DateTime.UtcNow - _lastLoadedTime) > DataFreshnessTtl;
                    if (isStale)
                    {
                        if (AnalyticsSkeleton != null) AnalyticsSkeleton.Visibility = Visibility.Visible;
                        await LoadDataAsync();
                    }

                    ScheduleRedraw();
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Constructor error: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Failed to initialize Analytics page: {ex.Message}", "Init Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnVmPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AnalyticsViewModel.IsLoading))
            {
                if (AnalyticsSkeleton != null)
                    AnalyticsSkeleton.Visibility = _vm.IsLoading ? Visibility.Visible : Visibility.Collapsed;
            }

            if (e.PropertyName == nameof(AnalyticsViewModel.BarangayData))
                HookBarangayCollection();

            ScheduleRedraw();
        }

        private void HookBarangayCollection()
        {
            var current = (object)_vm.BarangayData as INotifyCollectionChanged;
            if (ReferenceEquals(current, _hookedBarangay))
                return;

            if (_hookedBarangay != null)
                _hookedBarangay.CollectionChanged -= OnBarangayCollectionChanged;

            _hookedBarangay = current;

            if (_hookedBarangay != null)
                _hookedBarangay.CollectionChanged += OnBarangayCollectionChanged;
        }

        private void OnBarangayCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ScheduleRedraw();
        }

        private void HookChartCanvases()
        {
            foreach (var name in new[] { "CvBarangay", "CvSexDonut", "CvAgeBars", "CvTopCategories", "CvEmployDonut", "CvCivilBars" })
            {
                if (FindName(name) is Canvas canvas)
                {
                    canvas.SizeChanged -= Canvas_SizeChanged;
                    canvas.SizeChanged += Canvas_SizeChanged;
                }
            }
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScheduleRedraw();
        }

        private void ScheduleRedraw()
        {
            if (_redrawPending)
                return;

            _redrawPending = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _redrawPending = false;
                RedrawCharts();
            }), DispatcherPriority.Background);
        }

        private void InitFilterControls()
        {
            try
            {
                var cmbYearlyYear = this.FindName("CmbYearlyYear") as ComboBox;
                var cmbMonthlyYear = this.FindName("CmbMonthlyYear") as ComboBox;
                var cmbQuarterlyYear = this.FindName("CmbQuarterlyYear") as ComboBox;
                var cmbMonthlyMonth = this.FindName("CmbMonthlyMonth") as ComboBox;
                var cmbQuarterlyQuarter = this.FindName("CmbQuarterlyQuarter") as ComboBox;
                var cmbPeriodType = this.FindName("CmbPeriodType") as ComboBox;

                int currentYear = DateTime.Today.Year;
                int currentMonth = DateTime.Today.Month;
                int currentQuarter = (currentMonth - 1) / 3;

                if (cmbYearlyYear != null && cmbYearlyYear.Items.Count == 0)
                {
                    for (int y = currentYear; y >= 2020; y--)
                        cmbYearlyYear.Items.Add(y);
                    cmbYearlyYear.SelectedItem = currentYear;
                }

                if (cmbMonthlyYear != null && cmbMonthlyYear.Items.Count == 0)
                {
                    for (int y = currentYear; y >= 2020; y--)
                        cmbMonthlyYear.Items.Add(y);
                    cmbMonthlyYear.SelectedItem = currentYear;
                }

                if (cmbQuarterlyYear != null && cmbQuarterlyYear.Items.Count == 0)
                {
                    for (int y = currentYear; y >= 2020; y--)
                        cmbQuarterlyYear.Items.Add(y);
                    cmbQuarterlyYear.SelectedItem = currentYear;
                }

                if (cmbMonthlyMonth != null && cmbMonthlyMonth.SelectedIndex < 0)
                    cmbMonthlyMonth.SelectedIndex = currentMonth - 1;

                if (cmbQuarterlyQuarter != null && cmbQuarterlyQuarter.SelectedIndex < 0)
                    cmbQuarterlyQuarter.SelectedIndex = currentQuarter;

                var dpStartDate = this.FindName("DpStartDate") as DatePicker;
                if (dpStartDate != null)
                {
                    dpStartDate.DisplayDateEnd = DateTime.Today;
                    if (!dpStartDate.SelectedDate.HasValue)
                        dpStartDate.SelectedDate = _vm?.StartDate ?? new DateTime(currentYear, 1, 1);
                }

                var dpEndDate = this.FindName("DpEndDate") as DatePicker;
                if (dpEndDate != null)
                {
                    dpEndDate.DisplayDateEnd = DateTime.Today;
                    if (!dpEndDate.SelectedDate.HasValue)
                        dpEndDate.SelectedDate = _vm?.EndDate ?? DateTime.Today;
                }

                if (cmbPeriodType != null && cmbPeriodType.SelectedIndex < 0)
                    cmbPeriodType.SelectedIndex = 0;

                // If Barangay Secretary, lock barangay filter to assigned barangay
                if (SOLUM_UI.Services.Api.AuthApiService.Instance.IsBarangaySecretary &&
                    !string.IsNullOrWhiteSpace(SOLUM_UI.Services.Api.AuthApiService.Instance.CurrentUserBarangay))
                {
                    string userBrgy = SOLUM_UI.Services.Api.AuthApiService.Instance.CurrentUserBarangay;
                    var cmbFilterBarangay = this.FindName("CmbFilterBarangay") as ComboBox;
                    if (cmbFilterBarangay != null)
                    {
                        foreach (var item in cmbFilterBarangay.Items)
                        {
                            if (item is ComboBoxItem cbItem && string.Equals(cbItem.Content?.ToString(), userBrgy, StringComparison.OrdinalIgnoreCase))
                            {
                                cmbFilterBarangay.SelectedItem = cbItem;
                                break;
                            }
                        }
                        cmbFilterBarangay.IsEnabled = false;
                        cmbFilterBarangay.ToolTip = $"Scoped to your assigned barangay ({userBrgy})";
                    }
                    if (_vm != null)
                    {
                        _vm.SelectedBarangay = userBrgy;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitFilterControls error: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            await FetchAndPopulateAnalyticsAsync(showErrors: false);
        }

        private async System.Threading.Tasks.Task ApplyFiltersAsync()
        {
            await FetchAndPopulateAnalyticsAsync(showErrors: true);
        }

        private void SyncFilterValuesFromUi()
        {
            try
            {
                var cmbPeriodType = this.FindName("CmbPeriodType") as ComboBox;
                if (cmbPeriodType?.SelectedItem is ComboBoxItem periodItem && periodItem.Tag is string tag)
                    _vm.FilterType = tag;

                var cmbYearlyYear = this.FindName("CmbYearlyYear") as ComboBox;
                if (cmbYearlyYear?.SelectedItem is int yYear && _vm.FilterType == "Yearly")
                    _vm.SelectedYear = yYear;

                var cmbMonthlyYear = this.FindName("CmbMonthlyYear") as ComboBox;
                if (cmbMonthlyYear?.SelectedItem is int mYear && _vm.FilterType == "Monthly")
                    _vm.SelectedYear = mYear;

                var cmbMonthlyMonth = this.FindName("CmbMonthlyMonth") as ComboBox;
                if (cmbMonthlyMonth != null && cmbMonthlyMonth.SelectedIndex >= 0)
                    _vm.SelectedMonth = cmbMonthlyMonth.SelectedIndex + 1;

                var cmbQuarterlyYear = this.FindName("CmbQuarterlyYear") as ComboBox;
                if (cmbQuarterlyYear?.SelectedItem is int qYear && _vm.FilterType == "Quarterly")
                    _vm.SelectedYear = qYear;

                var cmbQuarterlyQuarter = this.FindName("CmbQuarterlyQuarter") as ComboBox;
                if (cmbQuarterlyQuarter != null && cmbQuarterlyQuarter.SelectedIndex >= 0)
                    _vm.SelectedQuarter = cmbQuarterlyQuarter.SelectedIndex + 1;

                var dpStartDate = this.FindName("DpStartDate") as DatePicker;
                if (dpStartDate?.SelectedDate.HasValue == true)
                    _vm.StartDate = dpStartDate.SelectedDate.Value;

                var dpEndDate = this.FindName("DpEndDate") as DatePicker;
                if (dpEndDate?.SelectedDate.HasValue == true)
                    _vm.EndDate = dpEndDate.SelectedDate.Value;

                var cmbFilterBarangay = this.FindName("CmbFilterBarangay") as ComboBox;
                if (cmbFilterBarangay?.SelectedItem is ComboBoxItem bItem && bItem.Content is string bContent)
                {
                    _vm.SelectedBarangay = (string.IsNullOrWhiteSpace(bContent) || bContent.Equals("All", StringComparison.OrdinalIgnoreCase)) ? "ALL" : bContent;
                }
                else if (cmbFilterBarangay?.SelectedItem is string bStr)
                {
                    _vm.SelectedBarangay = (string.IsNullOrWhiteSpace(bStr) || bStr.Equals("All", StringComparison.OrdinalIgnoreCase)) ? "ALL" : bStr;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SyncFilterValuesFromUi error: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task FetchAndPopulateAnalyticsAsync(bool showErrors)
        {
            try
            {
                if (_vm == null) return;
                _vm.IsLoading = true;
                if (AnalyticsSkeleton != null) AnalyticsSkeleton.Visibility = Visibility.Visible;

                SyncFilterValuesFromUi();
                _vm.UpdateActiveFilterSummary();

                string barangay = _vm.SelectedBarangay ?? "ALL";
                if (string.IsNullOrWhiteSpace(barangay) || barangay.Equals("All", StringComparison.OrdinalIgnoreCase))
                    barangay = "ALL";

                string filterType = _vm.FilterType ?? "Monthly";
                int targetYear = _vm.SelectedYear;
                int targetMonth = _vm.SelectedMonth;
                int? queryQuarter = null;
                int? queryMonth = null;
                string queryBreakdownType = null;
                DateTime? start = _vm.StartDate;
                DateTime? end = _vm.EndDate;
                int? startYear = null;
                int? startMonth = null;
                int? endYear = null;
                int? endMonth = null;

                switch (filterType)
                {
                    case "Yearly":
                        queryBreakdownType = "1";
                        targetYear = _vm.SelectedYear;
                        targetMonth = (_vm.SelectedYear >= DateTime.Today.Year) ? DateTime.Today.Month : 12;
                        start = new DateTime(targetYear, 1, 1);
                        end = new DateTime(targetYear, 12, 31);
                        if (targetYear >= DateTime.Today.Year && end > DateTime.Today)
                            end = DateTime.Today;
                        _vm.StartDate = start;
                        _vm.EndDate = end;
                        break;

                    case "Quarterly":
                        queryBreakdownType = "2";
                        queryQuarter = _vm.SelectedQuarter;
                        targetYear = _vm.SelectedYear;
                        int sMonth = (_vm.SelectedQuarter - 1) * 3 + 1;
                        int eMonth = _vm.SelectedQuarter * 3;
                        targetMonth = (targetYear >= DateTime.Today.Year && eMonth > DateTime.Today.Month)
                            ? DateTime.Today.Month
                            : eMonth;
                        start = new DateTime(targetYear, sMonth, 1);
                        end = new DateTime(targetYear, eMonth, DateTime.DaysInMonth(targetYear, eMonth));
                        if (targetYear >= DateTime.Today.Year && end > DateTime.Today)
                            end = DateTime.Today;
                        _vm.StartDate = start;
                        _vm.EndDate = end;
                        break;

                    case "CustomRange":
                        queryBreakdownType = "4";
                        if (!start.HasValue) start = new DateTime(DateTime.Today.Year, 1, 1);
                        if (!end.HasValue) end = DateTime.Today;
                        if (end > DateTime.Today) end = DateTime.Today;
                        if (end < start) end = start;

                        _vm.StartDate = start;
                        _vm.EndDate = end;
                        startYear = start.Value.Year;
                        startMonth = start.Value.Month;
                        endYear = end.Value.Year;
                        endMonth = end.Value.Month;
                        targetYear = endYear.Value;
                        targetMonth = (targetYear >= DateTime.Today.Year && endMonth.Value > DateTime.Today.Month)
                            ? DateTime.Today.Month
                            : endMonth.Value;
                        break;

                    case "Monthly":
                    default:
                        queryBreakdownType = "3";
                        targetYear = _vm.SelectedYear;
                        targetMonth = _vm.SelectedMonth;
                        queryMonth = targetMonth;
                        start = new DateTime(targetYear, targetMonth, 1);
                        end = new DateTime(targetYear, targetMonth, DateTime.DaysInMonth(targetYear, targetMonth));
                        if (targetYear >= DateTime.Today.Year && end > DateTime.Today)
                            end = DateTime.Today;
                        _vm.StartDate = start;
                        _vm.EndDate = end;
                        break;
                }

                int? queryYear = (filterType == "CustomRange") ? (int?)null : targetYear;

                var dashboardTask = AnalyticsApiService.Instance.GetDashboardAnalyticsAsync();
                var monthlyTask = AnalyticsApiService.Instance.GetMonthlyAnalyticsAsync(targetYear, targetMonth, barangay);
                var breakdownTask = AnalyticsApiService.Instance.GetBarangayBreakdownAsync(
                    quarter: queryQuarter,
                    year: queryYear,
                    month: queryMonth,
                    barangay: barangay,
                    type: queryBreakdownType,
                    startYear: startYear,
                    startMonth: startMonth,
                    endYear: endYear,
                    endMonth: endMonth,
                    startDate: start,
                    endDate: end);

                await System.Threading.Tasks.Task.WhenAll(dashboardTask, monthlyTask, breakdownTask);

                var dashRes = await dashboardTask;
                if (dashRes != null && dashRes.Succeeded && dashRes.Data != null)
                    _vm.LoadDashboardDto(dashRes.Data);

                var breakdownRes = await breakdownTask;
                if (breakdownRes != null && breakdownRes.Succeeded && breakdownRes.Data != null)
                    _vm.LoadBarangayBreakdownDto(breakdownRes.Data);

                var monthRes = await monthlyTask;
                if (monthRes != null && monthRes.Succeeded && monthRes.Data != null)
                {
                    _vm.LoadFromAnalyticsDto(monthRes.Data);
                }
                else
                {
                    // Clear demographics gracefully if not returned for this custom/aggregated period
                    _vm.ClearDemographicCharts();
                }

                if (showErrors)
                {
                    bool breakdownOk = breakdownRes != null && breakdownRes.Succeeded;
                    bool monthOk = monthRes != null && monthRes.Succeeded;

                    if (!breakdownOk && !monthOk)
                    {
                        string errorMsg = "Unable to load analytics data for the selected period.";
                        if (breakdownRes?.Errors != null && breakdownRes.Errors.Count > 0)
                            errorMsg = string.Join("\n", breakdownRes.Errors);
                        else if (monthRes?.Errors != null && monthRes.Errors.Count > 0)
                            errorMsg = string.Join("\n", monthRes.Errors);

                        ToastNotification.Show("Filter Result", errorMsg, ToastType.Warning);
                    }
                }

                _vm.UpdateActiveFilterSummary();
                HookBarangayCollection();
                ScheduleRedraw();

                _lastLoadedTime = DateTime.UtcNow;
                _hasLoadedData = true;

                if (TxtAnalyticsLastRefreshed != null)
                    TxtAnalyticsLastRefreshed.Text = $"Updated: {DateTime.Now:h:mm tt}";
                if (RefreshBtn != null)
                    RefreshBtn.ToolTip = $"Refresh Analytics Data (Last refreshed: {DateTime.Now:h:mm:ss tt})";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Analytics load error: {ex.Message}\n{ex.StackTrace}");
                if (showErrors)
                    ToastNotification.Show("Analytics Error", $"Analytics load error: {ex.Message}", ToastType.Error);
            }
            finally
            {
                if (_vm != null) _vm.IsLoading = false;
                if (AnalyticsSkeleton != null) AnalyticsSkeleton.Visibility = Visibility.Collapsed;
                if (PageLoadingOverlay != null) PageLoadingOverlay.IsLoading = false;
            }
        }

        private double N(string propertyName)
        {
            try
            {
                var prop = _vm?.GetType().GetProperty(propertyName);
                if (prop == null)
                    return 0;
                return Convert.ToDouble(prop.GetValue(_vm));
            }
            catch
            {
                return 0;
            }
        }

        private static Brush B(string hex)
        {
            return (Brush)new BrushConverter().ConvertFromString(hex);
        }

        private static List<(string Label, double Value)> ReadSeries(IEnumerable source)
        {
            var list = new List<(string Label, double Value)>();
            if (source == null)
                return list;

            foreach (var item in source)
            {
                if (item == null)
                    continue;

                var type = item.GetType();
                object labelObj = type.GetProperty("DisplayLabel")?.GetValue(item) ?? type.GetProperty("Label")?.GetValue(item);
                string label = Convert.ToString(labelObj) ?? string.Empty;

                double value = 0;
                var valueProp = type.GetProperty("Value");
                if (valueProp != null)
                {
                    try { value = Convert.ToDouble(valueProp.GetValue(item)); }
                    catch { value = 0; }
                }

                list.Add((label, value));
            }

            return list;
        }

        private static TextBlock MakeText(string text, double size, Brush brush, FontWeight weight)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = size,
                Foreground = brush,
                FontWeight = weight,
                FontFamily = new FontFamily("Segoe UI")
            };
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return tb;
        }

        private void RedrawCharts()
        {
            if (_vm == null)
                return;

            try
            {
                DrawLineChart(FindName("CvBarangay") as Canvas, ReadSeries((object)_vm.BarangayData as IEnumerable));

                double female = N("FemaleCount");
                double male = N("MaleCount");
                DrawDonut(FindName("CvSexDonut") as Canvas, 26, female + male, new List<(double, Brush)>
                {
                    (female, B("#C9536E")),  // Brand pink - nice cool tone
                    (male, B("#3B9FC9"))     // Brand blue - nice cool tone
                });

                var ageBars = new List<(string, double, Brush)>();
                for (int i = 0; i < AgeLabels.Length; i++)
                    ageBars.Add((AgeLabels[i], N(AgeProps[i]), B(AgeColors[i])));
                DrawBars(FindName("CvAgeBars") as Canvas, ageBars);

                // Top Categories horizontal bars (with x-axis grid)
                DrawCategoryBars(FindName("CvTopCategories") as Canvas, ReadSeries((object)_vm.TopCasesData as IEnumerable));

                double employed = N("EmployedPercent");
                double selfEmployed = N("SelfEmployedPercent");
                double notEmployed = N("NotEmployedPercent");
                DrawDonut(FindName("CvEmployDonut") as Canvas, 24, Math.Max(100, employed + selfEmployed + notEmployed), new List<(double, Brush)>
                {
                    (employed, B("#7D2245")),
                    (selfEmployed, B("#E3BF2E")),
                    (notEmployed, B("#A89AA0"))
                });

                DrawBars(FindName("CvCivilBars") as Canvas, new List<(string, double, Brush)>
                {
                    ("Single", N("SingleCount"), B("#7D2245")),
                    ("Married", N("MarriedCount"), B("#E3BF2E")),
                    ("Widowed", N("WidowedCount"), B("#7DBB57")),
                    ("Separated", N("SeparatedCount"), B("#4FB0C6")),
                    ("Annulled", N("AnnulledCount"), B("#A89AA0"))
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Redraw error: {ex}");
            }
        }

        private void DrawLineChart(Canvas c, List<(string Label, double Value)> data)
        {
            if (c == null)
                return;

            c.Children.Clear();

            double w = c.ActualWidth;
            double h = c.ActualHeight;
            if (w <= 0 || h <= 0)
                return;

            if (data.Count == 0)
            {
                var empty = MakeText("No barangay data for the selected filter", 12, B("#8A6A75"), FontWeights.Normal);
                Canvas.SetLeft(empty, (w - empty.DesiredSize.Width) / 2);
                Canvas.SetTop(empty, (h - empty.DesiredSize.Height) / 2);
                c.Children.Add(empty);
                return;
            }

            const double left = 44, right = 20, top = 14, bottom = 78;
            double pw = w - left - right;
            double ph = h - top - bottom;
            if (pw <= 0 || ph <= 0)
                return;

            const int ticks = 4;
            double max = data.Max(d => d.Value);
            double niceMax = Math.Max(ticks, Math.Ceiling(max / ticks) * ticks);

            var gridBrush = B("#EEE3E7");
            var textBrush = B("#8A6A75");
            var maroon = B("#7D2245");

            for (int i = 0; i <= ticks; i++)
            {
                double y = top + ph - ph * i / ticks;
                c.Children.Add(new Line { X1 = left, X2 = left + pw, Y1 = y, Y2 = y, Stroke = gridBrush, StrokeThickness = 1 });

                var lbl = MakeText(((int)Math.Round(niceMax * i / ticks)).ToString(), 10, textBrush, FontWeights.Normal);
                Canvas.SetLeft(lbl, left - 8 - lbl.DesiredSize.Width);
                Canvas.SetTop(lbl, y - lbl.DesiredSize.Height / 2);
                c.Children.Add(lbl);
            }

            int n = data.Count;
            var positions = new List<Point>();
            for (int i = 0; i < n; i++)
            {
                double x = n == 1 ? left + pw / 2 : left + pw * i / (n - 1);
                double y = top + ph - ph * (data[i].Value / niceMax);
                positions.Add(new Point(x, y));
            }

            var area = new PointCollection { new Point(positions[0].X, top + ph) };
            foreach (var p in positions)
                area.Add(p);
            area.Add(new Point(positions[n - 1].X, top + ph));

            c.Children.Add(new Polygon
            {
                Points = area,
                Fill = new SolidColorBrush(Color.FromArgb(40, 0x7D, 0x22, 0x45))
            });

            c.Children.Add(new Polyline
            {
                Points = new PointCollection(positions),
                Stroke = maroon,
                StrokeThickness = 2.5,
                StrokeLineJoin = PenLineJoin.Round
            });

            for (int i = 0; i < n; i++)
            {
                var dot = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.White,
                    Stroke = maroon,
                    StrokeThickness = 2,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = $"{data[i].Label}: {data[i].Value:N0} solo parents"
                };
                Canvas.SetLeft(dot, positions[i].X - 5);
                Canvas.SetTop(dot, positions[i].Y - 5);
                c.Children.Add(dot);

                var valueLabel = MakeText(data[i].Value.ToString("N0"), 10, maroon, FontWeights.SemiBold);
                Canvas.SetLeft(valueLabel, positions[i].X - valueLabel.DesiredSize.Width / 2);
                Canvas.SetTop(valueLabel, positions[i].Y - valueLabel.DesiredSize.Height - 8);
                c.Children.Add(valueLabel);

                string text = data[i].Label.Length > 18 ? data[i].Label.Substring(0, 17) + "…" : data[i].Label;
                var axis = MakeText(text, 10, textBrush, FontWeights.Normal);
                axis.RenderTransformOrigin = new Point(1, 0);
                axis.RenderTransform = new RotateTransform(-40);
                Canvas.SetLeft(axis, positions[i].X - axis.DesiredSize.Width + 4);
                Canvas.SetTop(axis, top + ph + 12);
                c.Children.Add(axis);
            }
        }

        private void DrawBars(Canvas c, List<(string Label, double Value, Brush Fill)> items)
        {
            if (c == null)
                return;

            c.Children.Clear();

            double w = c.ActualWidth;
            double h = c.ActualHeight;
            if (w <= 0 || h <= 0 || items.Count == 0)
                return;

            const double top = 26, bottom = 30, side = 8;
            double pw = w - side * 2;
            double ph = h - top - bottom;
            if (pw <= 0 || ph <= 0)
                return;

            double max = items.Max(i => i.Value);
            if (max <= 0)
                max = 1;

            double slot = pw / items.Count;
            double barW = Math.Min(64, slot * 0.6);
            var textBrush = B("#8A6A75");
            var darkBrush = B("#111827");

            c.Children.Add(new Line
            {
                X1 = side,
                X2 = side + pw,
                Y1 = top + ph,
                Y2 = top + ph,
                Stroke = B("#EEE3E7"),
                StrokeThickness = 1
            });

            for (int i = 0; i < items.Count; i++)
            {
                double barH = items[i].Value > 0 ? Math.Max(3, ph * items[i].Value / max) : 2;
                double x = side + slot * i + (slot - barW) / 2;
                double y = top + ph - barH;

                var bar = new Border
                {
                    Width = barW,
                    Height = barH,
                    Background = items[i].Fill,
                    CornerRadius = new CornerRadius(6, 6, 0, 0),
                    ToolTip = $"{items[i].Label}: {items[i].Value:N0}"
                };
                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, y);
                c.Children.Add(bar);

                var val = MakeText(items[i].Value.ToString("N0"), 12, darkBrush, FontWeights.Bold);
                Canvas.SetLeft(val, x + (barW - val.DesiredSize.Width) / 2);
                Canvas.SetTop(val, y - val.DesiredSize.Height - 3);
                c.Children.Add(val);

                var lbl = MakeText(items[i].Label, 10, textBrush, FontWeights.Normal);
                Canvas.SetLeft(lbl, side + slot * i + (slot - lbl.DesiredSize.Width) / 2);
                Canvas.SetTop(lbl, top + ph + 8);
                c.Children.Add(lbl);
            }
        }

        /// <summary>
        /// Horizontal bar chart with vertical gridlines and an x-axis scale (0, 50, 100, ...).
        /// Top category is dark maroon, the rest are a lighter mauve.
        /// </summary>
        private void DrawCategoryBars(Canvas canvas, List<(string Label, double Value)> categories)
        {
            if (canvas == null)
                return;

            canvas.Children.Clear();

            double w = canvas.ActualWidth;
            double h = canvas.ActualHeight;
            if (w <= 0 || h <= 0)
                return;

            var textBrush = B("#8A6A75");
            var gridBrush = B("#EEE3E7");
            var topBar = B("#7D2245");
            var otherBar = B("#CFA3B4");

            if (categories.Count == 0)
            {
                var empty = MakeText("No category data for the selected filter", 12, textBrush, FontWeights.Normal);
                Canvas.SetLeft(empty, (w - empty.DesiredSize.Width) / 2);
                Canvas.SetTop(empty, (h - empty.DesiredSize.Height) / 2);
                canvas.Children.Add(empty);
                return;
            }

            // Category labels (right-aligned next to the axis)
            var labels = new List<TextBlock>();
            foreach (var cat in categories)
            {
                string text = cat.Label.Length > 26 ? cat.Label.Substring(0, 25) + "…" : cat.Label;
                labels.Add(MakeText(text, 11, textBrush, FontWeights.Normal));
            }

            double maxLabelW = labels.Max(l => l.DesiredSize.Width);
            double left = Math.Min(200, maxLabelW + 18);
            const double right = 24, top = 8, bottom = 28;

            double pw = w - left - right;
            double ph = h - top - bottom;
            if (pw <= 0 || ph <= 0)
                return;

            // Nice x-axis scale
            double max = categories.Max(item => item.Value);
            if (max <= 0)
                max = 1;

            double rawStep = max / 4.0;
            double mag = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
            double norm = rawStep / mag;
            double step = (norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 5 ? 5 : 10) * mag;
            if (step < 1) step = 1;
            double niceMax = Math.Ceiling(max / step) * step;
            int tickCount = (int)Math.Round(niceMax / step);

            // Vertical gridlines + x-axis tick labels
            for (int t = 0; t <= tickCount; t++)
            {
                double x = left + pw * t / tickCount;
                canvas.Children.Add(new Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = top,
                    Y2 = top + ph,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                });

                var tick = MakeText(((int)Math.Round(step * t)).ToString(), 10, textBrush, FontWeights.Normal);
                Canvas.SetLeft(tick, x - tick.DesiredSize.Width / 2);
                Canvas.SetTop(tick, top + ph + 6);
                canvas.Children.Add(tick);
            }

            // Bars
            double slot = ph / categories.Count;
            double barH = Math.Min(34, slot * 0.7);

            for (int i = 0; i < categories.Count; i++)
            {
                double y = top + slot * i + (slot - barH) / 2;
                double barW = categories[i].Value > 0 ? Math.Max(3, pw * categories[i].Value / niceMax) : 2;

                var lbl = labels[i];
                Canvas.SetLeft(lbl, left - 10 - lbl.DesiredSize.Width);
                Canvas.SetTop(lbl, y + (barH - lbl.DesiredSize.Height) / 2);
                canvas.Children.Add(lbl);

                var bar = new Border
                {
                    Width = barW,
                    Height = barH,
                    Background = i == 0 ? topBar : otherBar,
                    CornerRadius = new CornerRadius(5),
                    ToolTip = $"{categories[i].Label}: {categories[i].Value:N0} cases"
                };
                Canvas.SetLeft(bar, left);
                Canvas.SetTop(bar, y);
                canvas.Children.Add(bar);
            }
        }

        private void DrawDonut(Canvas c, double thickness, double total, List<(double Value, Brush Fill)> parts)
        {
            if (c == null)
                return;

            c.Children.Clear();

            double size = Math.Min(c.ActualWidth, c.ActualHeight);
            if (size <= 0)
                return;

            double radius = (size - thickness) / 2;
            double cx = c.ActualWidth / 2;
            double cy = c.ActualHeight / 2;

            var track = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Stroke = B("#F5ECF0"),
                StrokeThickness = thickness,
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(track, cx - radius);
            Canvas.SetTop(track, cy - radius);
            c.Children.Add(track);

            if (total <= 0)
                return;

            double angle = -90;
            foreach (var part in parts)
            {
                double value = Math.Max(0, part.Value);
                if (value <= 0)
                    continue;

                double sweep = Math.Min(360, value / total * 360);

                if (sweep >= 359.9)
                {
                    var ring = new Ellipse
                    {
                        Width = radius * 2,
                        Height = radius * 2,
                        Stroke = part.Fill,
                        StrokeThickness = thickness,
                        Fill = Brushes.Transparent
                    };
                    Canvas.SetLeft(ring, cx - radius);
                    Canvas.SetTop(ring, cy - radius);
                    c.Children.Add(ring);
                    return;
                }

                double a0 = angle * Math.PI / 180;
                double a1 = (angle + sweep) * Math.PI / 180;
                var startPoint = new Point(cx + radius * Math.Cos(a0), cy + radius * Math.Sin(a0));
                var endPoint = new Point(cx + radius * Math.Cos(a1), cy + radius * Math.Sin(a1));

                var figure = new PathFigure { StartPoint = startPoint, IsClosed = false };
                figure.Segments.Add(new ArcSegment(endPoint, new Size(radius, radius), 0, sweep > 180, SweepDirection.Clockwise, true));
                var geometry = new PathGeometry();
                geometry.Figures.Add(figure);

                c.Children.Add(new System.Windows.Shapes.Path
                {
                    Data = geometry,
                    Stroke = part.Fill,
                    StrokeThickness = thickness,
                    StrokeStartLineCap = PenLineCap.Flat,
                    StrokeEndLineCap = PenLineCap.Flat
                });

                angle += sweep;
            }
        }

        private void PreviousBarangayButton_Click(object sender, RoutedEventArgs e)
        {
            _vm.PreviousBarangayPage();
            ScheduleRedraw();
        }

        private void NextBarangayButton_Click(object sender, RoutedEventArgs e)
        {
            _vm.NextBarangayPage();
            ScheduleRedraw();
        }

        private void FilterDrop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var overlay = this.FindName("FilterModalOverlay") as Grid;
                if (overlay != null)
                    overlay.Visibility = (overlay.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FilterDrop_Click error: {ex.Message}");
            }
        }

        private void FilterModalOverlay_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                var overlay = this.FindName("FilterModalOverlay") as Grid;
                if (overlay != null)
                    overlay.Visibility = Visibility.Collapsed;
            }
        }

        private void FilterModalCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void FilterModalClose_Click(object sender, RoutedEventArgs e)
        {
            var overlay = this.FindName("FilterModalOverlay") as Grid;
            if (overlay != null)
                overlay.Visibility = Visibility.Collapsed;
        }

        private void FilterCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var cmbFilterBarangay = this.FindName("CmbFilterBarangay") as ComboBox;
                if (_vm == null || cmbFilterBarangay == null || cmbFilterBarangay.SelectedItem == null)
                    return;

                string barangay = "ALL";

                if (cmbFilterBarangay.SelectedItem is ComboBoxItem comboItem && comboItem.Content is string content)
                    barangay = content;
                else if (cmbFilterBarangay.SelectedItem is string strItem)
                    barangay = strItem;

                if (barangay.Equals("All", StringComparison.OrdinalIgnoreCase))
                    barangay = "ALL";

                _vm.SelectedBarangay = barangay;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FilterCombo_Changed error: {ex.Message}");
            }
        }

        private void CmbPeriodType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var cmb = this.FindName("CmbPeriodType") as ComboBox;
                var pnlYearly = this.FindName("PnlYearly") as StackPanel;
                var pnlMonthly = this.FindName("PnlMonthly") as StackPanel;
                var pnlQuarterly = this.FindName("PnlQuarterly") as StackPanel;
                var pnlCustomRange = this.FindName("PnlCustomRange") as StackPanel;

                if (cmb == null || pnlYearly == null || pnlMonthly == null || pnlQuarterly == null || pnlCustomRange == null)
                    return;

                string tag = "Monthly";
                if (cmb.SelectedItem is ComboBoxItem item && item.Tag is string sTag)
                    tag = sTag;

                pnlYearly.Visibility = (tag == "Yearly") ? Visibility.Visible : Visibility.Collapsed;
                pnlMonthly.Visibility = (tag == "Monthly") ? Visibility.Visible : Visibility.Collapsed;
                pnlQuarterly.Visibility = (tag == "Quarterly") ? Visibility.Visible : Visibility.Collapsed;
                pnlCustomRange.Visibility = (tag == "CustomRange") ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CmbPeriodType_SelectionChanged error: {ex.Message}");
            }
        }

        private void FilterReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cmbPeriodType = this.FindName("CmbPeriodType") as ComboBox;
                if (cmbPeriodType != null)
                    cmbPeriodType.SelectedIndex = 0;

                var cmbYearlyYear = this.FindName("CmbYearlyYear") as ComboBox;
                if (cmbYearlyYear != null)
                    cmbYearlyYear.SelectedItem = DateTime.Today.Year;

                var cmbMonthlyYear = this.FindName("CmbMonthlyYear") as ComboBox;
                if (cmbMonthlyYear != null)
                    cmbMonthlyYear.SelectedItem = DateTime.Today.Year;

                var cmbMonthlyMonth = this.FindName("CmbMonthlyMonth") as ComboBox;
                if (cmbMonthlyMonth != null)
                    cmbMonthlyMonth.SelectedIndex = DateTime.Today.Month - 1;

                var cmbQuarterlyYear = this.FindName("CmbQuarterlyYear") as ComboBox;
                if (cmbQuarterlyYear != null)
                    cmbQuarterlyYear.SelectedItem = DateTime.Today.Year;

                var cmbQuarterlyQuarter = this.FindName("CmbQuarterlyQuarter") as ComboBox;
                if (cmbQuarterlyQuarter != null)
                    cmbQuarterlyQuarter.SelectedIndex = (DateTime.Today.Month - 1) / 3;

                var dpStartDate = this.FindName("DpStartDate") as DatePicker;
                if (dpStartDate != null)
                {
                    dpStartDate.DisplayDateEnd = DateTime.Today;
                    dpStartDate.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
                }

                var dpEndDate = this.FindName("DpEndDate") as DatePicker;
                if (dpEndDate != null)
                {
                    dpEndDate.DisplayDateEnd = DateTime.Today;
                    dpEndDate.SelectedDate = DateTime.Today;
                }

                var cmbFilterBarangay = this.FindName("CmbFilterBarangay") as ComboBox;
                if (cmbFilterBarangay != null)
                    cmbFilterBarangay.SelectedIndex = 0;

                if (_vm != null)
                {
                    _vm.FilterType = "Monthly";
                    _vm.SelectedYear = DateTime.Today.Year;
                    _vm.SelectedMonth = DateTime.Today.Month;
                    _vm.SelectedQuarter = (DateTime.Today.Month - 1) / 3 + 1;
                    _vm.StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    _vm.EndDate = DateTime.Now;
                    _vm.SelectedBarangay = "ALL";
                    _vm.UpdateActiveFilterSummary();
                }

                var overlay = this.FindName("FilterModalOverlay") as Grid;
                if (overlay != null)
                    overlay.Visibility = Visibility.Collapsed;

                _ = LoadDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FilterReset_Click error: {ex.Message}");
            }
        }

        private void FilterApply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cmbPeriodType = this.FindName("CmbPeriodType") as ComboBox;
                string periodType = "Monthly";
                if (cmbPeriodType?.SelectedItem is ComboBoxItem item && item.Tag is string sTag)
                    periodType = sTag;

                // Validation against future dates or invalid combinations
                if (periodType == "CustomRange")
                {
                    var dpStartDate = this.FindName("DpStartDate") as DatePicker;
                    var dpEndDate = this.FindName("DpEndDate") as DatePicker;

                    if (!dpStartDate?.SelectedDate.HasValue == true || !dpEndDate?.SelectedDate.HasValue == true)
                    {
                        ToastNotification.Show(
                            "Invalid Date Range",
                            "Please select both a start date and an end date for custom range.",
                            ToastType.Warning);
                        return;
                    }

                    DateTime startDate = dpStartDate.SelectedDate.Value.Date;
                    DateTime endDate = dpEndDate.SelectedDate.Value.Date;
                    DateTime today = DateTime.Today;

                    if (startDate > today || endDate > today)
                    {
                        ToastNotification.Show(
                            "Future Date Not Allowed",
                            "Dates cannot be in the future. Please select dates on or before today.",
                            ToastType.Warning);
                        return;
                    }

                    if (startDate > endDate)
                    {
                        ToastNotification.Show(
                            "Invalid Date Range",
                            "The start date cannot be later than the end date.",
                            ToastType.Warning);
                        return;
                    }
                }
                else if (periodType == "Quarterly")
                {
                    var cmbYear = this.FindName("CmbQuarterlyYear") as ComboBox;
                    var cmbQuarter = this.FindName("CmbQuarterlyQuarter") as ComboBox;
                    if (cmbYear?.SelectedItem is int qYear && cmbQuarter != null && cmbQuarter.SelectedIndex >= 0)
                    {
                        int quarter = cmbQuarter.SelectedIndex + 1;
                        int quarterStartMonth = (quarter - 1) * 3 + 1;
                        DateTime quarterStartDate = new DateTime(qYear, quarterStartMonth, 1);
                        if (quarterStartDate > DateTime.Today)
                        {
                            ToastNotification.Show(
                                "Future Quarter Not Allowed",
                                "The selected quarter has not started yet. Please choose a current or past quarter.",
                                ToastType.Warning);
                            return;
                        }
                    }
                }
                else if (periodType == "Monthly")
                {
                    var cmbYear = this.FindName("CmbMonthlyYear") as ComboBox;
                    var cmbMonth = this.FindName("CmbMonthlyMonth") as ComboBox;
                    if (cmbYear?.SelectedItem is int mYear && cmbMonth != null && cmbMonth.SelectedIndex >= 0)
                    {
                        int month = cmbMonth.SelectedIndex + 1;
                        DateTime monthStartDate = new DateTime(mYear, month, 1);
                        if (monthStartDate > DateTime.Today)
                        {
                            ToastNotification.Show(
                                "Future Month Not Allowed",
                                "The selected month is in the future. Please choose a current or past month.",
                                ToastType.Warning);
                            return;
                        }
                    }
                }
                else if (periodType == "Yearly")
                {
                    var cmbYear = this.FindName("CmbYearlyYear") as ComboBox;
                    if (cmbYear?.SelectedItem is int yYear)
                    {
                        if (yYear > DateTime.Today.Year)
                        {
                            ToastNotification.Show(
                                "Future Year Not Allowed",
                                "The selected year is in the future. Please choose a current or past year.",
                                ToastType.Warning);
                            return;
                        }
                    }
                }

                var overlay = this.FindName("FilterModalOverlay") as Grid;
                if (overlay != null)
                    overlay.Visibility = Visibility.Collapsed;

                _ = ApplyFiltersAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FilterApply_Click error: {ex.Message}");
            }
        }

        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string selectedBarangay = _vm?.SelectedBarangay ?? "ALL";
                if (string.IsNullOrWhiteSpace(selectedBarangay))
                    selectedBarangay = "ALL";

                if (_vm == null || _vm.AnalyticsData == null)
                {
                    ActionConfirmDialog.Warning(
                        "No Data",
                        "No analytics data loaded. Please refresh the data first.");
                    return;
                }

                var saveDialog = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    FileName = $"Solo_Parents_Analytics_{DateTime.Today:yyyyMMdd}.xlsx",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string municipality = selectedBarangay != "ALL" ? selectedBarangay : "Biñan City";
                    string region = "CALABARZON (IV-A)";
                    string focalPersonName = MainWindow.FullName;

                    AnalyticsExcelExportService.ExportAnalyticsReport(
                        saveDialog.FileName,
                        _vm.AnalyticsData,
                        municipality,
                        region,
                        focalPersonName);

                    ToastNotification.Show(
                        "Export Successful",
                        $"Analytics report exported to:\n{System.IO.Path.GetFileName(saveDialog.FileName)}",
                        ToastType.Success);

                    Services.AuditLogService.Instance.LogSystem(
                        $"Analytics report exported for {municipality} by {focalPersonName}");
                }
            }
            catch (Exception ex)
            {
                ActionConfirmDialog.Error(
                    "Export Error",
                    $"Export failed: {ex.Message}");

                System.Diagnostics.Debug.WriteLine($"Export error: {ex.StackTrace}");
            }
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            if ((_vm != null && _vm.IsLoading) || (DateTime.UtcNow - _lastRefreshClickTime) < RefreshCooldown)
                return;

            _lastRefreshClickTime = DateTime.UtcNow;

            try
            {
                var refreshBtn = this.FindName("RefreshBtn") as Button;
                if (refreshBtn == null)
                    return;

                refreshBtn.IsEnabled = false;

                if (refreshBtn.Content is Grid contentGrid)
                {
                    var refreshIcon = contentGrid.Children.OfType<System.Windows.Shapes.Path>().FirstOrDefault();
                    var loadingSpinner = contentGrid.Children.OfType<StackPanel>().FirstOrDefault();

                    if (refreshIcon != null)
                        refreshIcon.Visibility = Visibility.Collapsed;
                    if (loadingSpinner != null)
                        loadingSpinner.Visibility = Visibility.Visible;
                }

                await LoadDataAsync();

                ToastNotification.Show("Analytics Refreshed", "Analytics data refreshed successfully.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Refresh Error", $"Refresh failed: {ex.Message}", ToastType.Error);
                System.Diagnostics.Debug.WriteLine($"Refresh error: {ex}");
            }
            finally
            {
                try
                {
                    var refreshBtn = this.FindName("RefreshBtn") as Button;
                    if (refreshBtn != null && refreshBtn.Content is Grid contentGrid)
                    {
                        var refreshIcon = contentGrid.Children.OfType<System.Windows.Shapes.Path>().FirstOrDefault();
                        var loadingSpinner = contentGrid.Children.OfType<StackPanel>().FirstOrDefault();

                        if (refreshIcon != null)
                            refreshIcon.Visibility = Visibility.Visible;
                        if (loadingSpinner != null)
                            loadingSpinner.Visibility = Visibility.Collapsed;

                        _ = Task.Run(async () =>
                        {
                            var elapsed = DateTime.UtcNow - _lastRefreshClickTime;
                            if (elapsed < RefreshCooldown)
                                await Task.Delay(RefreshCooldown - elapsed);
                            await Dispatcher.InvokeAsync(() =>
                            {
                                var btn = this.FindName("RefreshBtn") as Button;
                                if (btn != null) btn.IsEnabled = true;
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Refresh finally error: {ex.Message}");
                }
            }
        }
    }
}