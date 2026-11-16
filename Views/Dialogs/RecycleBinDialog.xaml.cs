using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SOLUM_UI.DTOs.Archive;
using SOLUM_UI.Services.Api;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class RecycleBinDialog : Window
    {
        public bool WasRecordReinstated { get; private set; }
        public int RemainingItemCount => _allItems.Count;

        private readonly List<RecycleBinItemViewModel> _allItems = new List<RecycleBinItemViewModel>();
        private readonly ObservableCollection<RecycleBinItemViewModel> _displayItems = new ObservableCollection<RecycleBinItemViewModel>();

        public RecycleBinDialog()
        {
            InitializeComponent();
            RecycleList.ItemsSource = _displayItems;
            Loaded += async (s, e) => await LoadRecycleBinAsync();
        }

        private async Task LoadRecycleBinAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                var response = await ArchiveApiService.Instance.GetRecycleBinAsync();

                _allItems.Clear();
                _displayItems.Clear();

                if (response != null && response.Succeeded && response.Data != null)
                {
                    foreach (var item in response.Data)
                    {
                        _allItems.Add(new RecycleBinItemViewModel
                        {
                            Id = item.Id,
                            FullName = item.FullName,
                            Barangay = item.Barangay,
                            PhilsysCardNumber = string.IsNullOrWhiteSpace(item.PhilsysCardNumber) ? "Not Provided" : item.PhilsysCardNumber,
                            DeletionRequestedAt = item.DeletionRequestedAt,
                            ReinstatementDeadline = item.ReinstatementDeadline,
                            DaysRemaining = item.DaysRemaining,
                            DeletionReason = item.DeletionReason
                        });
                    }
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                ActionConfirmDialog.Error("Load Error", $"Failed to load Recycle Bin items: {ex.Message}", this);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyFilter()
        {
            string query = TxtSearchFilter.Text?.Trim().ToLowerInvariant() ?? string.Empty;

            _displayItems.Clear();
            var filtered = string.IsNullOrEmpty(query)
                ? _allItems
                : _allItems.Where(i =>
                    (i.FullName != null && i.FullName.ToLowerInvariant().Contains(query)) ||
                    (i.PhilsysCardNumber != null && i.PhilsysCardNumber.ToLowerInvariant().Contains(query)) ||
                    (i.Barangay != null && i.Barangay.ToLowerInvariant().Contains(query)));

            foreach (var item in filtered)
            {
                _displayItems.Add(item);
            }

            bool isEmpty = _displayItems.Count == 0;
            EmptyStatePanel.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
            RecycleList.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;

            TxtFooterCount.Text = _allItems.Count == 1
                ? "1 record in Recycle Bin"
                : $"{_allItems.Count} records in Recycle Bin";
        }

        private void TxtSearchFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadRecycleBinAsync();
        }

        private async void BtnReinstate_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || !(btn.Tag is Guid id))
                return;

            var target = _allItems.FirstOrDefault(x => x.Id == id);
            string name = target?.FullName ?? "this record";

            bool confirmed = ActionConfirmDialog.Show(
                title: "Reinstate Record",
                message: $"Are you sure you want to reinstate {name} back to the active directory?\n\nThe 30-day deletion countdown will be cancelled.",
                confirmText: "Reinstate",
                cancelText: "Cancel",
                theme: ConfirmThemeType.Success,
                owner: this);

            if (!confirmed)
                return;

            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                var response = await ArchiveApiService.Instance.ReinstateFromRecycleBinAsync(id);

                if (response != null && response.Succeeded)
                {
                    WasRecordReinstated = true;
                    _allItems.RemoveAll(x => x.Id == id);
                    ApplyFilter();
                    Services.AuditLogService.Instance.LogReinstate(id.ToString(), name, MainWindow.CurrentUserName, MainWindow.CurrentUserRole);

                    ActionConfirmDialog.Show(
                        title: "Record Reinstated",
                        message: $"{name} has been restored to the active solo parent directory.",
                        confirmText: "OK",
                        theme: ConfirmThemeType.Success,
                        owner: this);
                }
                else
                {
                    string err = response?.Errors != null && response.Errors.Count > 0
                        ? string.Join("\n", response.Errors)
                        : "Failed to reinstate record. The grace period may have expired.";
                    ActionConfirmDialog.Error("Reinstatement Failed", err, this);
                }
            }
            catch (Exception ex)
            {
                ActionConfirmDialog.Error("Reinstatement Error", ex.Message, this);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void RecycleTableGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Grid grid && grid.ActualWidth > 0 && grid.ActualHeight > 0)
            {
                grid.Clip = new RectangleGeometry(new Rect(0, 0, grid.ActualWidth, grid.ActualHeight), 10, 10);
            }
        }

        private void BtnViewArchives_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = WasRecordReinstated;
            Close();
            (Application.Current.MainWindow as MainWindow)?.NavigateTo("MunicipalArchives");
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = WasRecordReinstated;
            Close();
        }
    }

    public class RecycleBinItemViewModel
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Barangay { get; set; } = string.Empty;
        public string PhilsysCardNumber { get; set; } = string.Empty;
        public DateTime DeletionRequestedAt { get; set; }
        public DateTime ReinstatementDeadline { get; set; }
        public int DaysRemaining { get; set; }
        public string DeletionReason { get; set; } = string.Empty;
        public string DeletionReasonDisplay => string.IsNullOrWhiteSpace(DeletionReason) ? "No reason specified" : DeletionReason;
        public bool IsUrgent => DaysRemaining <= 5;
        public string DaysRemainingText => DaysRemaining == 1 ? "1 Day Remaining" : $"{DaysRemaining} Days Remaining";
    }
}
