using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI
{
    public partial class AnalyticsPage : Page
    {
        private int _filterYear;
        private int _filterMonth;
        private string _filterBarangay = "ALL";
        private AnalyticsViewModel _viewModel;

        private readonly Dictionary<(int year, int month, string barangay), MonthlyAnalyticsDto> _cache = 
            new Dictionary<(int, int, string), MonthlyAnalyticsDto>();

        private static readonly string[] Barangays =
        {
            "BiÃ±an", "Bungahan", "Canlalay", "Casile", "De La Paz", "Ganado",
            "Langkiwa", "Loma", "Malaban", "Malamig", "Mamplasan", "Platero",
            "Poblacion", "San Antonio", "San Francisco", "San Jose", "San Vicente",
            "Santo Domingo", "Santo NiÃ±o", "Santo Tomas", "Soro-Soro", "Timbao",
            "Tubigan", "Zapote"
        };

        public AnalyticsPage()
        {
            InitializeComponent();
            _viewModel = new AnalyticsViewModel();
            this.DataContext = _viewModel;
            
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

        private void FilterDrop_Click(object sender, RoutedEventArgs e)
        {
            // Open the filter popup
            FilterPopup.IsOpen = !FilterPopup.IsOpen;
        }

        private async void FilterApply_Click(object sender, RoutedEventArgs e)
        {
            _filterYear = (int)((CmbFilterYear.SelectedItem as ComboBoxItem)?.Tag ?? DateTime.Today.Year);
            _filterMonth = (int)((CmbFilterMonth.SelectedItem as ComboBoxItem)?.Tag ?? DateTime.Today.Month);
            _filterBarangay = (string)((CmbFilterBarangay.SelectedItem as ComboBoxItem)?.Tag ?? "ALL");
            FilterPopup.IsOpen = false;
            await LoadAndBuildAnalyticsAsync();
        }

        private async void FilterReset_Click(object sender, RoutedEventArgs e)
        {
            _filterYear = DateTime.Today.Year;
            _filterMonth = DateTime.Today.Month;
            _filterBarangay = "ALL";
            DpDateFrom.SelectedDate = null;
            DpDateTo.SelectedDate = null;
            PopulateFilterDropdowns();
            FilterPopup.IsOpen = false;
            await LoadAndBuildAnalyticsAsync();
        }

        private async void DateRange_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Trigger reload when date range is changed
            if (DpDateFrom.SelectedDate.HasValue && DpDateTo.SelectedDate.HasValue)
            {
                // For now, just reload with current filter
                // In the future, this could implement date range filtering
                await LoadAndBuildAnalyticsAsync();
            }
        }

        private async Task LoadAndBuildAnalyticsAsync()
        {
            if (AnalyticsLoadingOverlay != null) AnalyticsLoadingOverlay.IsLoading = true;
            if (_viewModel != null) _viewModel.IsLoading = true;

            try
            {
                System.Diagnostics.Debug.WriteLine($"[Analytics] Loading data for {_filterYear}-{_filterMonth} (Barangay: {_filterBarangay})");
                
                var response = await GetMonthlyAnalyticsCachedAsync(_filterYear, _filterMonth, _filterBarangay);

                System.Diagnostics.Debug.WriteLine($"[Analytics] API Response: Succeeded={response.Succeeded}, Data={response.Data != null}");
                
                if (!response.Succeeded || response.Data == null)
                {
                    string err = response.Errors != null && response.Errors.Count > 0
                        ? string.Join("\n", response.Errors)
                        : "Unable to load analytics for this period.";
                    System.Diagnostics.Debug.WriteLine($"[Analytics] Error: {err}");
                    ToastNotification.Show("Load Failed", err, ToastType.Warning);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[Analytics] Received data: TotalSoloParents={response.Data.TotalSoloParents}");

                // Populate ViewModel from DTO
                _viewModel.LoadFromAnalyticsDto(response.Data);
                
                System.Diagnostics.Debug.WriteLine($"[Analytics] ViewModel updated: TotalSoloParents={_viewModel.TotalSoloParents}, AgeGroupData.Count={_viewModel.AgeGroupData.Count}");
                
                // Update refresh timestamp
                _viewModel.LastRefreshTime = DateTime.Now;
                TxtLastRefresh.Text = "Updated " + DateTime.Now.ToString("MMM d, h:mm tt");
            }
            finally
            {
                if (AnalyticsLoadingOverlay != null) AnalyticsLoadingOverlay.IsLoading = false;
                if (_viewModel != null) _viewModel.IsLoading = false;
            }
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
        }
    }


