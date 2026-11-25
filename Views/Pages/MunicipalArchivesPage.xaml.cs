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
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Views;
using SOLUM_UI.Views.Dialogs;

namespace SOLUM_UI.Views.Pages
{
    public partial class MunicipalArchivesPage : Page
    {
        public static string CurrentUserName { get; set; } = string.Empty;
        public static string CurrentUserRole { get; set; } = string.Empty;
        public static string CurrentUserBarangay { get; set; } = string.Empty;

        private int _currentPage = 1;
        private readonly int _pageSize = 20;
        private int _totalPages = 1;
        private int _totalCount = 0;
        private bool _isLoading = false;

        private readonly ObservableCollection<ArchivedSoloParentRowViewModel> _items =
            new ObservableCollection<ArchivedSoloParentRowViewModel>();

        public MunicipalArchivesPage()
        {
            InitializeComponent();
            ArchivesList.ItemsSource = _items;

            ArchivesList.SizeChanged += (s, e) => ResizeColumns();
            ArchivesList.Loaded += (s, e) => ResizeColumns();
            SizeChanged += (s, e) => ResizeColumns();

            Loaded += async (s, e) =>
            {
                ApplyRolePermissions();
                ResizeColumns();
                await LoadArchivesAsync();
            };

            AppSettingsService.Instance.SettingsChanged += (s, e) =>
            {
                Dispatcher.Invoke(() => ArchivesList?.Items.Refresh());
            };
        }

        private void ApplyRolePermissions()
        {
            bool isAdmin = string.Equals(CurrentUserRole, "Administrator", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(CurrentUserRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(CurrentUserRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

            if (BtnRunPipeline != null)
            {
                BtnRunPipeline.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            }

            // If user is scoped to a specific barangay, lock and pre-select it
            if (!string.IsNullOrWhiteSpace(CurrentUserBarangay) && !isAdmin)
            {
                foreach (ComboBoxItem item in CmbBarangay.Items)
                {
                    if (string.Equals(item.Content?.ToString(), CurrentUserBarangay, StringComparison.OrdinalIgnoreCase))
                    {
                        CmbBarangay.SelectedItem = item;
                        CmbBarangay.IsEnabled = false;
                        break;
                    }
                }
            }
        }

        private async Task LoadArchivesAsync()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                if (ArchivesSkeletonLoader != null) ArchivesSkeletonLoader.Visibility = Visibility.Visible;

                string searchTerm = TxtSearchQuery.Text?.Trim();

                string selectedBarangay = null;
                if (CmbBarangay.SelectedItem is ComboBoxItem brgyItem)
                {
                    string brgy = brgyItem.Content?.ToString();
                    if (!string.IsNullOrWhiteSpace(brgy) && !brgy.Equals("All Barangays", StringComparison.OrdinalIgnoreCase))
                    {
                        selectedBarangay = brgy;
                    }
                }

                string selectedReason = null;
                if (CmbReason.SelectedItem is ComboBoxItem reasonItem)
                {
                    selectedReason = reasonItem.Tag?.ToString();
                }

                int? selectedYear = null;
                if (CmbYear.SelectedItem is ComboBoxItem yearItem && int.TryParse(yearItem.Content?.ToString(), out int parsedYear))
                {
                    selectedYear = parsedYear;
                }

                var req = new ArchiveSearchRequest
                {
                    SearchTerm = searchTerm,
                    Barangay = selectedBarangay,
                    ArchiveReason = selectedReason,
                    YearArchived = selectedYear,
                    PageNumber = _currentPage,
                    PageSize = _pageSize
                };

                var response = await ArchiveApiService.Instance.SearchArchivesAsync(req);

                _items.Clear();

                if (response != null && response.Succeeded && response.Data != null)
                {
                    var paged = response.Data;
                    _totalCount = paged.TotalCount;
                    _totalPages = Math.Max(1, paged.TotalPages);
                    _currentPage = paged.Page;

                    if (paged.Items != null)
                    {
                        foreach (var item in paged.Items)
                        {
                            if (item.IsReactivated) continue;

                            _items.Add(new ArchivedSoloParentRowViewModel
                            {
                                Id = item.Id,
                                FirstName = item.FirstName,
                                LastName = item.LastName,
                                Barangay = item.Barangay,
                                PhilsysCardNumber = string.IsNullOrWhiteSpace(item.PhilsysCardNumber) ? "Not Provided" : item.PhilsysCardNumber,
                                DateArchived = item.DateArchived,
                                ArchiveReason = item.ArchiveReason,
                                IsShredded = item.IsShredded,
                                IsReactivated = item.IsReactivated
                            });
                        }
                    }
                }
                else
                {
                    _totalCount = 0;
                    _totalPages = 1;
                }

                UpdatePaginationUI();
            }
            catch (Exception ex)
            {
                ActionConfirmDialog.Error("Archive Search Error", $"Failed to search archives: {ex.Message}", Window.GetWindow(this));
            }
            finally
            {
                if (ArchivesSkeletonLoader != null) ArchivesSkeletonLoader.Visibility = Visibility.Collapsed;
                _isLoading = false;
            }
        }

        private void UpdatePaginationUI()
        {
            bool hasRecords = _items.Count > 0;
            EmptyStatePanel.Visibility = hasRecords ? Visibility.Collapsed : Visibility.Visible;
            ArchivesList.Visibility = Visibility.Visible;

            int start = _totalCount == 0 ? 0 : ((_currentPage - 1) * _pageSize) + 1;
            int end = Math.Min(_totalCount, _currentPage * _pageSize);

            TxtPaginationInfo.Text = $"Showing {start} to {end} of {_totalCount} archived records";
            TxtPageCounter.Text = $"Page {_currentPage} of {_totalPages}";

            BtnPrevPage.IsEnabled = _currentPage > 1;
            BtnNextPage.IsEnabled = _currentPage < _totalPages;

            ResizeColumns();
        }

        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            await LoadArchivesAsync();
        }

        private async void TxtSearchQuery_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _currentPage = 1;
                await LoadArchivesAsync();
            }
        }

        private async void FilterControl_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            _currentPage = 1;
            await LoadArchivesAsync();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadArchivesAsync();
        }

        private async void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadArchivesAsync();
            }
        }

        private async void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                await LoadArchivesAsync();
            }
        }

        private void BtnBackToRecords_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as MainWindow)?.NavigateTo("SoloParentRecords");
        }

        private async void BtnRunPipeline_Click(object sender, RoutedEventArgs e)
        {
            bool confirmed = ActionConfirmDialog.Show(
                title: "Run Retention Pipeline",
                message: "Execute the automated statutory retention pipeline now?\n\nThis will:\n1. Move soft-deleted records past 30 days to cold archives.\n2. Archive inactive cases unrenewed for 5+ years.\n3. Cryptographically purge archives older than 10 years.",
                confirmText: "Execute Pipeline",
                cancelText: "Cancel",
                theme: ConfirmThemeType.Warning,
                owner: Window.GetWindow(this));

            if (!confirmed) return;

            try
            {
                if (ArchivesSkeletonLoader != null) ArchivesSkeletonLoader.Visibility = Visibility.Visible;
                var result = await ArchiveApiService.Instance.RunLifecyclePipelineAsync();

                if (result != null && result.Succeeded && result.Data != null)
                {
                    var data = result.Data;
                    ActionConfirmDialog.Show(
                        title: "Pipeline Completed",
                        message: $"Retention pipeline executed successfully:\n\n" +
                                 $"• Transferred Deletions: {data.ProcessedDeletions}\n" +
                                 $"• Archived Inactive: {data.ProcessedInactives}\n" +
                                 $"• Cryptographically Shredded: {data.ProcessedShredded}",
                        confirmText: "OK",
                        theme: ConfirmThemeType.Success,
                        owner: Window.GetWindow(this));

                    _currentPage = 1;
                    await LoadArchivesAsync();
                }
                else
                {
                    string err = result?.Errors != null && result.Errors.Count > 0
                        ? string.Join("\n", result.Errors)
                        : "Failed to execute lifecycle pipeline.";
                    ActionConfirmDialog.Error("Pipeline Error", err, Window.GetWindow(this));
                }
            }
            catch (Exception ex)
            {
                ActionConfirmDialog.Error("Pipeline Error", ex.Message, Window.GetWindow(this));
            }
            finally
            {
                if (ArchivesSkeletonLoader != null) ArchivesSkeletonLoader.Visibility = Visibility.Collapsed;
            }
        }

        private void ArchivesList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!AppSettingsService.Instance.SingleClickOpen) return;
            if (e.OriginalSource is DependencyObject dep)
            {
                DependencyObject cur = dep;
                while (cur != null && cur != ArchivesList)
                {
                    if (cur is Button) return;
                    cur = VisualTreeHelper.GetParent(cur);
                }

                var item = ItemsControl.ContainerFromElement(ArchivesList, dep) as ListViewItem;
                if (item?.DataContext is ArchivedSoloParentRowViewModel row && row.Id != Guid.Empty)
                {
                    ArchivesList.SelectedItem = row;
                    OpenDossierDialog(row.Id);
                }
            }
        }

        private void ArchivesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject dep)
            {
                var item = ItemsControl.ContainerFromElement(ArchivesList, dep) as ListViewItem;
                if (item?.DataContext is ArchivedSoloParentRowViewModel row && row.Id != Guid.Empty)
                {
                    OpenDossierDialog(row.Id);
                    return;
                }
            }

            if (ArchivesList.SelectedItem is ArchivedSoloParentRowViewModel sel && sel.Id != Guid.Empty)
            {
                OpenDossierDialog(sel.Id);
            }
        }

        private void BtnViewDossier_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || !(btn.Tag is Guid id)) return;
            // Phase 4: Opens ArchivedDossierDialog
            OpenDossierDialog(id);
        }

        private void BtnReactivate_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || !(btn.Tag is Guid id)) return;
            // Phase 4: Opens ReactivateCitizenDialog
            OpenReactivationDialog(id);
        }

        public void OpenDossierDialog(Guid id)
        {
            // Placeholder wired up in Phase 4
            var dialog = new ArchivedDossierDialog(id) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true && dialog.WasReactivated)
            {
                var item = _items.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    _items.Remove(item);
                    _totalCount = Math.Max(0, _totalCount - 1);
                    UpdatePaginationUI();
                }

                SoloParentRecordsPage.ForceRefreshOnLoad = true;
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("SoloParentRecords");
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Dashboard");
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Analytics");
                _ = LoadArchivesAsync();
            }
        }

        public void OpenReactivationDialog(Guid id)
        {
            // Placeholder wired up in Phase 4
            string citizenName = _items?.FirstOrDefault(x => x.Id == id)?.FullName ?? string.Empty;
            var dialog = new ReactivateCitizenDialog(id, citizenName) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                var item = _items.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    _items.Remove(item);
                    _totalCount = Math.Max(0, _totalCount - 1);
                    UpdatePaginationUI();
                }

                SoloParentRecordsPage.ForceRefreshOnLoad = true;
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("SoloParentRecords");
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Dashboard");
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Analytics");
                _ = LoadArchivesAsync();
            }
        }

        private void ArchivesTableGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Grid grid && grid.ActualWidth > 0 && grid.ActualHeight > 0)
            {
                grid.Clip = new RectangleGeometry(new Rect(0, 0, grid.ActualWidth, grid.ActualHeight), 15, 15);
            }
        }

        private bool _isScrollHooked = false;
        private void HookScrollViewer()
        {
            if (_isScrollHooked) return;
            var sv = FindVisualChild<ScrollViewer>(ArchivesList);
            if (sv != null)
            {
                sv.ScrollChanged += (s, e) =>
                {
                    if (e.ViewportHeightChange != 0 || e.ExtentHeightChange != 0)
                    {
                        ResizeColumns();
                    }
                };
                _isScrollHooked = true;
            }
        }

        private void ResizeColumns()
        {
            if (ArchivesList?.View is GridView gv && gv.Columns.Count >= 6)
            {
                double totalWidth = ArchivesList.ActualWidth;
                if (totalWidth <= 400) return;

                HookScrollViewer();

                var sv = FindVisualChild<ScrollViewer>(ArchivesList);
                bool hasVScroll = (sv != null && sv.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                                  || (_items.Count * 48 > Math.Max(100, ArchivesList.ActualHeight - 46));

                // 22px buffer for 18px scrollbar + 4px border margin; 4px when no scrollbar
                double reserved = hasVScroll ? 22.0 : 4.0;
                double available = Math.Max(400, totalWidth - reserved);

                const double actionsWidth = 220;
                const double archivedDateWidth = 140;
                const double philsysWidth = 160;
                double fixedTotal = actionsWidth + archivedDateWidth + philsysWidth; // 520px

                double flex = Math.Max(260, available - fixedTotal);

                // Proportional flex distribution:
                // Barangay: 25% (min 130)
                // Reason:   32% (min 170)
                // Name:     remainder (min 220) to absorb rounding and fill 100% of available space
                double barangayWidth = Math.Round(Math.Max(130, flex * 0.25));
                double reasonWidth = Math.Round(Math.Max(170, flex * 0.32));
                double nameWidth = Math.Max(220, available - (fixedTotal + barangayWidth + reasonWidth));

                if (ColName != null) ColName.Width = nameWidth;
                if (ColBarangay != null) ColBarangay.Width = barangayWidth;
                if (ColPhilsys != null) ColPhilsys.Width = philsysWidth;
                if (ColArchivedDate != null) ColArchivedDate.Width = archivedDateWidth;
                if (ColReason != null) ColReason.Width = reasonWidth;
                if (ColActions != null) ColActions.Width = actionsWidth;
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild) return typedChild;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }

    public class ArchivedSoloParentRowViewModel
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{LastName}, {FirstName}".Trim(',', ' ');
        public string Barangay { get; set; } = string.Empty;
        public string PhilsysCardNumber { get; set; } = string.Empty;
        public DateTime DateArchived { get; set; }
        public string ArchiveReason { get; set; } = string.Empty;
        public bool IsShredded { get; set; }
        public bool IsReactivated { get; set; }

        public string ArchiveReasonDisplay
        {
            get
            {
                if (string.Equals(ArchiveReason, "Inactive5Years", StringComparison.OrdinalIgnoreCase))
                    return "Inactive 5+ Years";
                if (string.Equals(ArchiveReason, "DeletionExpired", StringComparison.OrdinalIgnoreCase))
                    return "Deletion Expired";
                return ArchiveReason;
            }
        }

        public DateTime ScheduledShredDate => DateArchived.AddYears(10);
    }
}
