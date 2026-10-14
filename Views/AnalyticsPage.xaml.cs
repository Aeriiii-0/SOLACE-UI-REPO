using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI
{
    /// <summary>
    /// Interaction logic for AnalyticsPage.xaml
    /// </summary>
    public partial class AnalyticsPage : Page
    {
        private AnalyticsViewModel _vm;

        public AnalyticsPage()
        {
            // Load XAML content manually since code-behind isn't generated
            try
            {
                var resourceUri = new Uri("/Views/AnalyticsPage.xaml", UriKind.Relative);
                System.Windows.Application.LoadComponent(this, resourceUri);
                System.Diagnostics.Debug.WriteLine("✓ AnalyticsPage XAML loaded successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ XAML load error: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Failed to load Analytics page XAML: {ex.Message}", "XAML Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            try
            {
                _vm = new AnalyticsViewModel();
                System.Diagnostics.Debug.WriteLine("✓ AnalyticsViewModel created");
                
                DataContext = _vm;
                System.Diagnostics.Debug.WriteLine($"✓ DataContext set. BarangayData count: {_vm.BarangayData.Count}");
                
                // Add Loaded handler - this should trigger when page is fully loaded
                Loaded += async (s, e) => 
                {
                    System.Diagnostics.Debug.WriteLine("→ Page Loaded event firing, calling LoadDataAsync...");
                    InitFilterControls();
                    await LoadDataAsync();
                    SuppressDatePickerWatermarks();
                };

                // Also try loading data immediately as a fallback
                System.Diagnostics.Debug.WriteLine("→ Attempting immediate data load (may load before UI is ready)...");
                _ = LoadDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Constructor error: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Failed to initialize Analytics page: {ex.Message}", "Init Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    for (int y = currentYear + 1; y >= 2020; y--)
                    {
                        cmbYearlyYear.Items.Add(y);
                    }
                    cmbYearlyYear.SelectedItem = currentYear;
                }

                if (cmbMonthlyYear != null && cmbMonthlyYear.Items.Count == 0)
                {
                    for (int y = currentYear + 1; y >= 2020; y--)
                    {
                        cmbMonthlyYear.Items.Add(y);
                    }
                    cmbMonthlyYear.SelectedItem = currentYear;
                }

                if (cmbQuarterlyYear != null && cmbQuarterlyYear.Items.Count == 0)
                {
                    for (int y = currentYear + 1; y >= 2020; y--)
                    {
                        cmbQuarterlyYear.Items.Add(y);
                    }
                    cmbQuarterlyYear.SelectedItem = currentYear;
                }

                if (cmbMonthlyMonth != null && cmbMonthlyMonth.SelectedIndex < 0)
                {
                    cmbMonthlyMonth.SelectedIndex = currentMonth - 1;
                }

                if (cmbQuarterlyQuarter != null && cmbQuarterlyQuarter.SelectedIndex < 0)
                {
                    cmbQuarterlyQuarter.SelectedIndex = currentQuarter;
                }

                if (cmbPeriodType != null && cmbPeriodType.SelectedIndex < 0)
                {
                    cmbPeriodType.SelectedIndex = 0; // Default: By Monthly
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
                {
                    _vm.FilterType = tag;
                }

                var cmbYearlyYear = this.FindName("CmbYearlyYear") as ComboBox;
                if (cmbYearlyYear?.SelectedItem is int yYear)
                {
                    if (_vm.FilterType == "Yearly")
                        _vm.SelectedYear = yYear;
                }

                var cmbMonthlyYear = this.FindName("CmbMonthlyYear") as ComboBox;
                if (cmbMonthlyYear?.SelectedItem is int mYear)
                {
                    if (_vm.FilterType == "Monthly")
                        _vm.SelectedYear = mYear;
                }

                var cmbMonthlyMonth = this.FindName("CmbMonthlyMonth") as ComboBox;
                if (cmbMonthlyMonth != null && cmbMonthlyMonth.SelectedIndex >= 0)
                {
                    _vm.SelectedMonth = cmbMonthlyMonth.SelectedIndex + 1;
                }

                var cmbQuarterlyYear = this.FindName("CmbQuarterlyYear") as ComboBox;
                if (cmbQuarterlyYear?.SelectedItem is int qYear)
                {
                    if (_vm.FilterType == "Quarterly")
                        _vm.SelectedYear = qYear;
                }

                var cmbQuarterlyQuarter = this.FindName("CmbQuarterlyQuarter") as ComboBox;
                if (cmbQuarterlyQuarter != null && cmbQuarterlyQuarter.SelectedIndex >= 0)
                {
                    _vm.SelectedQuarter = cmbQuarterlyQuarter.SelectedIndex + 1;
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

                // Sync current UI selections into _vm
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
                        targetMonth = (_vm.SelectedYear == DateTime.Today.Year) ? DateTime.Today.Month : 12;
                        start = new DateTime(targetYear, 1, 1);
                        end = new DateTime(targetYear, 12, 31);
                        _vm.StartDate = start;
                        _vm.EndDate = end;
                        break;

                    case "Quarterly":
                        queryBreakdownType = "2";
                        queryQuarter = _vm.SelectedQuarter;
                        targetYear = _vm.SelectedYear;
                        int sMonth = (_vm.SelectedQuarter - 1) * 3 + 1;
                        int eMonth = _vm.SelectedQuarter * 3;
                        targetMonth = eMonth;
                        start = new DateTime(targetYear, sMonth, 1);
                        end = new DateTime(targetYear, eMonth, DateTime.DaysInMonth(targetYear, eMonth));
                        _vm.StartDate = start;
                        _vm.EndDate = end;
                        break;

                    case "CustomRange":
                        queryBreakdownType = "4";
                        if (!start.HasValue) start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        if (!end.HasValue) end = DateTime.Today;
                        if (end < start) end = start;

                        _vm.StartDate = start;
                        _vm.EndDate = end;
                        startYear = start.Value.Year;
                        startMonth = start.Value.Month;
                        endYear = end.Value.Year;
                        endMonth = end.Value.Month;
                        targetYear = endYear.Value;
                        targetMonth = endMonth.Value;
                        break;

                    case "Monthly":
                    default:
                        queryBreakdownType = "3";
                        targetYear = _vm.SelectedYear;
                        targetMonth = _vm.SelectedMonth;
                        queryMonth = targetMonth;
                        start = new DateTime(targetYear, targetMonth, 1);
                        end = new DateTime(targetYear, targetMonth, DateTime.DaysInMonth(targetYear, targetMonth));
                        _vm.StartDate = start;
                        _vm.EndDate = end;
                        break;
                }

                System.Diagnostics.Debug.WriteLine($"→ FetchAndPopulateAnalyticsAsync: type={queryBreakdownType}, year={targetYear}, month={targetMonth}, quarter={queryQuarter}, barangay={barangay}");

                // Concurrently fetch all 3 endpoints
                var dashboardTask = AnalyticsApiService.Instance.GetDashboardAnalyticsAsync();
                var monthlyTask = AnalyticsApiService.Instance.GetMonthlyAnalyticsAsync(targetYear, targetMonth, barangay, start, end);
                var breakdownTask = AnalyticsApiService.Instance.GetBarangayBreakdownAsync(
                    quarter: queryQuarter,
                    year: targetYear,
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
                {
                    _vm.LoadDashboardDto(dashRes.Data);
                    System.Diagnostics.Debug.WriteLine($"✓ Dashboard KPIs loaded: TotalRegistered={dashRes.Data.TotalRegistered}, Active={dashRes.Data.ActiveRecords}");
                }

                var monthRes = await monthlyTask;
                if (monthRes != null && monthRes.Succeeded && monthRes.Data != null)
                {
                    _vm.LoadFromAnalyticsDto(monthRes.Data);
                    System.Diagnostics.Debug.WriteLine($"✓ Monthly data loaded: TotalSoloParents={monthRes.Data.TotalSoloParents}");
                }
                else if (showErrors && monthRes != null && !monthRes.Succeeded)
                {
                    string errorMsg = monthRes.Errors != null && monthRes.Errors.Count > 0
                        ? string.Join("\n", monthRes.Errors)
                        : "Unable to load analytics data.";
                    MessageBox.Show(errorMsg, "Filter Result", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                var breakdownRes = await breakdownTask;
                if (breakdownRes != null && breakdownRes.Succeeded && breakdownRes.Data != null)
                {
                    _vm.LoadBarangayBreakdownDto(breakdownRes.Data);
                    System.Diagnostics.Debug.WriteLine($"✓ Barangay breakdown loaded: {breakdownRes.Data.Barangays?.Count ?? 0} barangays");
                }

                _vm.UpdateActiveFilterSummary();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Analytics load error: {ex.Message}\n{ex.StackTrace}");
                if (showErrors)
                {
                    MessageBox.Show($"Analytics load error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                if (_vm != null) _vm.IsLoading = false;
            }
        }

        /// <summary>
        /// Suppresses the default "Select a date" watermark in all DatePickerTextBox controls
        /// to avoid overlapping with our custom MM/DD/YYYY placeholder.
        /// </summary>
        /// <summary>
        /// Watermark suppression is now handled via XAML ControlTemplate in BrandDatePicker style.
        /// This method is kept for compatibility but no longer needed.
        /// </summary>
        private void SuppressDatePickerWatermarks()
        {
            // XAML template now properly suppresses native watermark by wrapping ScrollViewer in Grid
            // This prevents rendering of PART_Watermark element, so no code-behind suppression needed
        }

        /// <summary>
        /// Helper method to find all visual children of a given type.
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                    if (child is T typed)
                    {
                        yield return typed;
                    }
                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void PreviousBarangayButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _vm.PreviousBarangayPage();
        }

        private void NextBarangayButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _vm.NextBarangayPage();
        }

        private void FilterDrop_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var overlay = this.FindName("FilterModalOverlay") as Grid;
                if (overlay != null)
                {
                    overlay.Visibility = (overlay.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
                }
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

        private void FilterModalClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var overlay = this.FindName("FilterModalOverlay") as Grid;
            if (overlay != null)
                overlay.Visibility = Visibility.Collapsed;
        }

        private void FilterCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Guard: event can fire during InitializeComponent() before _vm is assigned
            try
            {
                var cmbFilterBarangay = this.FindName("CmbFilterBarangay") as ComboBox;
                if (_vm == null || cmbFilterBarangay == null || cmbFilterBarangay.SelectedItem == null)
                    return;

                // Extract Content property from ComboBoxItem instead of calling ToString()
                string barangay = "ALL";

                if (cmbFilterBarangay.SelectedItem is ComboBoxItem comboItem && comboItem.Content is string content)
                    barangay = content;
                else if (cmbFilterBarangay.SelectedItem is string strItem)
                    barangay = strItem;

                // Normalize "All" → "ALL"
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

                if (_vm != null)
                {
                    _vm.FilterType = tag;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CmbPeriodType_SelectionChanged error: {ex.Message}");
            }
        }

        private void FilterReset_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var cmbPeriodType = this.FindName("CmbPeriodType") as ComboBox;
                if (cmbPeriodType != null)
                    cmbPeriodType.SelectedIndex = 0; // By Monthly

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

                // Reset barangay combo
                var cmbFilterBarangay = this.FindName("CmbFilterBarangay") as ComboBox;
                if (cmbFilterBarangay != null)
                    cmbFilterBarangay.SelectedIndex = 0;

                // Reset ViewModel
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

        private void FilterApply_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var overlay = this.FindName("FilterModalOverlay") as Grid;
                if (overlay != null)
                {
                    overlay.Visibility = Visibility.Collapsed;
                }

                // Apply filters - reload data with selected filters
                _ = ApplyFiltersAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FilterApply_Click error: {ex.Message}");
            }
        }

        private void ExportBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                // Get current filter values
                string selectedBarangay = _vm?.SelectedBarangay ?? "ALL";
                if (string.IsNullOrWhiteSpace(selectedBarangay))
                    selectedBarangay = "ALL";

                // Get analytics data from the view model
                if (_vm == null || _vm.AnalyticsData == null)
                {
                    System.Windows.MessageBox.Show(
                        "No analytics data loaded. Please refresh the data first.",
                        "No Data",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Open file save dialog
                var saveDialog = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    FileName = $"Solo_Parents_Analytics_{DateTime.Today:yyyyMMdd}.xlsx",
                    InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)
                };

                if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // Get municipality and region (can be enhanced to be dynamic)
                    string municipality = selectedBarangay != "ALL" ? selectedBarangay : "Biñan City";
                    string region = "CALABARZON (IV-A)";
                    string focalPersonName = MainWindow.FullName;

                    // Export using the new service
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

                    // Log the export action
                    Services.AuditLogService.Instance.LogSystem(
                        $"Analytics report exported for {municipality} by {focalPersonName}");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Export failed: {ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                System.Diagnostics.Debug.WriteLine($"Export error: {ex.StackTrace}");
            }
        }

        private MonthlyAnalyticsDto BuildMonthlyAnalyticsFromViewModel()
        {
            // Create analytics DTO from current ViewModel data
            var dto = new MonthlyAnalyticsDto
            {
                Year = DateTime.Now.Year,
                Month = DateTime.Now.Month,
                Barangay = "ALL",
                TotalSoloParents = _vm.TotalSoloParents,
                NewRegistrations = 0,  // TODO: Get from ViewModel if available
                Renewals = 0,           // TODO: Get from ViewModel if available
                Sex = new System.Collections.Generic.Dictionary<string, int>
                {
                    { "Female", _vm.FemaleCount },
                    { "Male", _vm.MaleCount }
                },
                CivilStatus = new System.Collections.Generic.Dictionary<string, int>
                {
                    { "Single", _vm.SingleCount },
                    { "Married", _vm.MarriedCount },
                    { "Widowed", _vm.WidowedCount },
                    { "Separated", _vm.SeparatedCount },
                    { "Annulled", _vm.AnnulledCount }
                },
                EmploymentStatus = new System.Collections.Generic.Dictionary<string, int>(),
                AgeBrackets = new System.Collections.Generic.Dictionary<string, int>(),
                MonthlyIncomeBrackets = new System.Collections.Generic.Dictionary<string, int>(),
                DependentsAgeBrackets = new System.Collections.Generic.Dictionary<string, int>
                {
                    { "6_AND_BELOW", _vm.AgeGroup0_5 },
                    { "7_TO_22", _vm.AgeGroup6_12 },
                    { "22_AND_ABOVE", _vm.AgeGroup13_17 }
                },
                Categories = new System.Collections.Generic.Dictionary<string, int>(),
                Lgbt = new System.Collections.Generic.Dictionary<string, int>
                {
                    { "Yes", _vm.LgbtCount },
                    { "No", _vm.TotalSoloParents - _vm.LgbtCount }
                },
                PantawidBeneficiary = new System.Collections.Generic.Dictionary<string, int>
                {
                    { "Yes", _vm.PantawidBeneficiaryCount },
                    { "No", _vm.TotalSoloParents - _vm.PantawidBeneficiaryCount }
                },
                Indigenous = new System.Collections.Generic.Dictionary<string, int>()
            };

            // Populate employment from ViewModel text values
            if (!string.IsNullOrEmpty(_vm.EmployedText))
            {
                var parts = _vm.EmployedText.Split('(');
                if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out int employed))
                    dto.EmploymentStatus["employed"] = employed;
            }
            if (!string.IsNullOrEmpty(_vm.SelfEmployedText))
            {
                var parts = _vm.SelfEmployedText.Split('(');
                if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out int selfEmp))
                    dto.EmploymentStatus["self_employed"] = selfEmp;
            }
            if (!string.IsNullOrEmpty(_vm.NotEmployedText))
            {
                var parts = _vm.NotEmployedText.Split('(');
                if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out int notEmp))
                    dto.EmploymentStatus["not_employed"] = notEmp;
            }

            // Populate from observable collections if available
            if (_vm.EmploymentStatusData != null && _vm.EmploymentStatusData.Count > 0)
            {
                var empLabels = new[] { "employed", "self_employed", "not_employed" };
                int i = 0;
                foreach (var item in _vm.EmploymentStatusData)
                {
                    if (i < empLabels.Length)
                        dto.EmploymentStatus[empLabels[i]] = item.Value;
                    i++;
                }
            }

            if (_vm.AgeGroupData != null && _vm.AgeGroupData.Count > 0)
            {
                var ageKeys = new[] { "19_AND_BELOW", "20_39", "40_59", "60_AND_ABOVE" };
                int i = 0;
                foreach (var item in _vm.AgeGroupData)
                {
                    if (i < ageKeys.Length)
                        dto.AgeBrackets[ageKeys[i]] = item.Value;
                    i++;
                }
            }

            if (_vm.IncomeBracketData != null && _vm.IncomeBracketData.Count > 0)
            {
                var incomeKeys = new[] { "BELOW_MINIMUM_WAGE", "MIN_WAGE_PLUS1_TO_20833", "20834_AND_ABOVE" };
                int i = 0;
                foreach (var item in _vm.IncomeBracketData)
                {
                    if (i < incomeKeys.Length)
                        dto.MonthlyIncomeBrackets[incomeKeys[i]] = item.Value;
                    i++;
                }
            }

            // Populate categories from BarangayData or TopCasesData if representing categories
            if (_vm.TopCasesData != null && _vm.TopCasesData.Count > 0)
            {
                int i = 0;
                var categoryKeys = new[] { "A1", "A2", "A3", "A4", "A5", "A6", "A7", "B", "C", "D", "E", "F" };
                foreach (var item in _vm.TopCasesData)
                {
                    if (i < categoryKeys.Length)
                        dto.Categories[categoryKeys[i]] = item.Value;
                    i++;
                }
            }

            return dto;
        }

        private async void RefreshBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var refreshBtn = this.FindName("RefreshBtn") as Button;
                if (refreshBtn == null)
                    return;

                // Disable button and show loader
                refreshBtn.IsEnabled = false;
                
                // Find the loader and icon elements in the button content
                if (refreshBtn.Content is Grid contentGrid)
                {
                    var refreshIcon = contentGrid.Children.OfType<System.Windows.Shapes.Path>().FirstOrDefault();
                    var loadingSpinner = contentGrid.Children.OfType<StackPanel>().FirstOrDefault();
                    
                    if (refreshIcon != null)
                        refreshIcon.Visibility = System.Windows.Visibility.Collapsed;
                    if (loadingSpinner != null)
                        loadingSpinner.Visibility = System.Windows.Visibility.Visible;
                }
                
                // Refresh analytics data
                await LoadDataAsync();
                
                // Show success notification (only if no error message was already shown)
                MessageBox.Show("Analytics data refreshed successfully.", "Refresh Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Show error notification
                MessageBox.Show($"Refresh failed: {ex.Message}", "Refresh Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Refresh error: {ex}");
            }
            finally
            {
                // Restore icon and hide loader
                try
                {
                    var refreshBtn = this.FindName("RefreshBtn") as Button;
                    if (refreshBtn != null && refreshBtn.Content is Grid contentGrid)
                    {
                        var refreshIcon = contentGrid.Children.OfType<System.Windows.Shapes.Path>().FirstOrDefault();
                        var loadingSpinner = contentGrid.Children.OfType<StackPanel>().FirstOrDefault();
                        
                        if (refreshIcon != null)
                            refreshIcon.Visibility = System.Windows.Visibility.Visible;
                        if (loadingSpinner != null)
                            loadingSpinner.Visibility = System.Windows.Visibility.Collapsed;
                        
                        refreshBtn.IsEnabled = true;
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
