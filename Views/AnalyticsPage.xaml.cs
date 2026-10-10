using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI
{
    public partial class AnalyticsPage : Page
    {
        private AnalyticsViewModel _vm;

        public AnalyticsPage()
        {
            InitializeComponent();
            _vm = new AnalyticsViewModel();
            DataContext = _vm;
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                // Use ViewModel's current date range (defaults to current month)
                int year = _vm.StartDate.HasValue ? _vm.StartDate.Value.Year : DateTime.Today.Year;
                int month = _vm.StartDate.HasValue ? _vm.StartDate.Value.Month : DateTime.Today.Month;
                string barangay = _vm?.SelectedBarangay ?? "ALL";

                var response = await AnalyticsApiService.Instance.GetMonthlyAnalyticsAsync(
                    year, month, barangay,
                    _vm?.StartDate, _vm?.EndDate);

                if (response.Succeeded && response.Data != null)
                {
                    _vm.LoadFromAnalyticsDto(response.Data);
                }
                else if (!response.Succeeded)
                {
                    string errorMsg = response.Errors != null && response.Errors.Count > 0
                        ? string.Join("\n", response.Errors)
                        : "Unable to load analytics data.";
                    MessageBox.Show(errorMsg, "Load Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Analytics load error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Analytics load error: {ex.Message}");
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
            if (FilterPopup != null)
            {
                FilterPopup.IsOpen = !FilterPopup.IsOpen;
            }
        }

        private void FilterCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Guard: event can fire during InitializeComponent() before _vm is assigned
            if (_vm == null || CmbFilterBarangay == null || CmbFilterBarangay.SelectedItem == null)
                return;

            // Extract Content property from ComboBoxItem instead of calling ToString()
            string barangay = "ALL";

            if (CmbFilterBarangay.SelectedItem is ComboBoxItem comboItem && comboItem.Content is string content)
                barangay = content;
            else if (CmbFilterBarangay.SelectedItem is string strItem)
                barangay = strItem;

            // Normalize "All" → "ALL"
            if (barangay.Equals("All", StringComparison.OrdinalIgnoreCase))
                barangay = "ALL";

            _vm.SelectedBarangay = barangay;
        }

        private void FilterReset_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Reset barangay combo
            if (CmbFilterBarangay != null)
                CmbFilterBarangay.SelectedIndex = 0;

            // Reset ViewModel dates to current month
            if (_vm != null)
            {
                _vm.StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                _vm.EndDate = DateTime.Now;
                _vm.SelectedBarangay = "ALL";
            }
        }

        private void FilterApply_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (FilterPopup != null)
            {
                FilterPopup.IsOpen = false;
                
                // Apply filters - reload data with selected filters
                _ = ApplyFiltersAsync();
            }
        }

        private async System.Threading.Tasks.Task ApplyFiltersAsync()
        {
            try
            {
                // Get selected barangay from filter dropdown with null safety
                string barangay = _vm?.SelectedBarangay ?? "ALL";
                
                // Validate barangay is not empty or only whitespace
                if (string.IsNullOrWhiteSpace(barangay))
                    barangay = "ALL";
                
                // Normalize "All" variants to "ALL"
                if (barangay.Equals("All", StringComparison.OrdinalIgnoreCase))
                    barangay = "ALL";

                // Resolve year/month from StartDate (fall back to current month)
                DateTime start = _vm?.StartDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                DateTime end   = _vm?.EndDate   ?? DateTime.Now;

                // Clamp: end must not be before start
                if (end < start) end = start;

                int year  = start.Year;
                int month = start.Month;

                // Get analytics data for selected period and barangay
                var response = await AnalyticsApiService.Instance.GetMonthlyAnalyticsAsync(
                    year, month, barangay, start, end);

                if (response.Succeeded && response.Data != null)
                {
                    _vm.LoadFromAnalyticsDto(response.Data);
                    System.Diagnostics.Debug.WriteLine(
                        $"[Analytics] Filters applied — Barangay: {barangay}, Period: {start:yyyy-MM-dd} → {end:yyyy-MM-dd}");
                }
                else
                {
                    string errorMsg = response.Errors != null && response.Errors.Count > 0
                        ? string.Join("\n", response.Errors)
                        : "Failed to load filtered data.";
                    MessageBox.Show(errorMsg, "Filter Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Filter application failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Filter error: {ex}");
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
                // Disable button and show loader
                RefreshBtn.IsEnabled = false;
                
                // Find the loader and icon elements in the button content
                if (RefreshBtn.Content is Grid contentGrid)
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
                if (RefreshBtn.Content is Grid contentGrid)
                {
                    var refreshIcon = contentGrid.Children.OfType<System.Windows.Shapes.Path>().FirstOrDefault();
                    var loadingSpinner = contentGrid.Children.OfType<StackPanel>().FirstOrDefault();
                    
                    if (refreshIcon != null)
                        refreshIcon.Visibility = System.Windows.Visibility.Visible;
                    if (loadingSpinner != null)
                        loadingSpinner.Visibility = System.Windows.Visibility.Collapsed;
                }
                
                RefreshBtn.IsEnabled = true;
            }
        }
    }
}
