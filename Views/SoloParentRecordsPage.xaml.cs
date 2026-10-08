using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.ViewModels;
using SOLUM_UI.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SOLUM_UI
{
    public partial class SoloParentRecordsPage : Page
    {
        public static string CurrentUserName    { get; set; } = string.Empty;
        public static string CurrentUserRole    { get; set; } = string.Empty;
        public static string CurrentUserBarangay { get; set; } = string.Empty;

        private List<SoloParentRecordViewModel> _allRecords;
        private List<SoloParentRecordViewModel> _filteredRecords;

        private string _statusFilter   = "All";
        private string _sexFilter      = "All";
        private string _barangayFilter = "All";
        private string _sortOption     = "Name A-Z";

        private const int PageSize = 10;
        private int _currentPage   = 1;
        private int _totalPages    = 1;
        private bool _isLoading    = false;

        private bool IsAdmin     => string.Equals(CurrentUserRole, "Administrator", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(CurrentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);
        private bool IsBasicUser => string.Equals(CurrentUserRole, "BasicUser", StringComparison.OrdinalIgnoreCase);
        private bool IsEncoder   => string.Equals(CurrentUserRole, "Encoder", StringComparison.OrdinalIgnoreCase);

        public SoloParentRecordsPage()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                ApplyRoleView();
                ResizeNameColumn();
                _ = LoadRecordsAsync();
            };
            SizeChanged += Page_SizeChanged;
            // Also recompute when the ListView itself changes size
            RecordsList.SizeChanged += (s, e) => ResizeNameColumn();
            RecordsList.Loaded      += (s, e) => ResizeNameColumn();
        }

        private void ApplyRoleView()
        {
            if (IsBasicUser)
            {
                AdminToolbar.Visibility       = Visibility.Collapsed;
                BasicUserToolbar.Visibility   = Visibility.Visible;
                EncoderSearchPanel.Visibility = Visibility.Collapsed;
                PagingRow.Visibility          = Visibility.Visible;
                _barangayFilter               = CurrentUserBarangay;

                if (ColSex         != null) ColSex.Width         = 0;
                if (ColCivilStatus != null) ColCivilStatus.Width = 0;
                if (ColChildren    != null) ColChildren.Width    = 0;
                if (ColLastUpdated != null) ColLastUpdated.Width = 0;
                if (ColBarangay    != null) ColBarangay.Width    = 0;
                if (ColActions     != null) ColActions.Width     = 300;
            }
            else if (IsEncoder)
            {
                AdminToolbar.Visibility       = Visibility.Collapsed;
                BasicUserToolbar.Visibility   = Visibility.Collapsed;
                EncoderSearchPanel.Visibility = Visibility.Visible;
                PagingRow.Visibility          = Visibility.Visible;
            }
            else
            {
                AdminToolbar.Visibility       = Visibility.Visible;
                BasicUserToolbar.Visibility   = Visibility.Collapsed;
                EncoderSearchPanel.Visibility = Visibility.Collapsed;
                PagingRow.Visibility          = Visibility.Visible;
            }
        }

        private async Task LoadRecordsAsync()
        {
            if (_isLoading) return;
            _isLoading = true;
            if (BtnRefreshRecords != null) BtnRefreshRecords.IsEnabled = false;
            if (PageLoadingOverlay != null) PageLoadingOverlay.IsLoading = true;

            try
            {
                string effectiveBarangay = IsBasicUser ? CurrentUserBarangay : _barangayFilter;
                string idQuery = SearchId?.Text?.Trim() ?? string.Empty;
                string nameQuery = SearchName?.Text?.Trim() ?? string.Empty;
                string barangayQuery = SearchBarangay?.Text?.Trim() ?? string.Empty;

                if (IsBasicUser)
                {
                    nameQuery = BasicSearchName?.Text?.Trim() ?? string.Empty;
                    barangayQuery = string.Empty;
                }

                Guid? searchGuid = null;
                if (Guid.TryParse(idQuery, out var parsedGuid))
                {
                    searchGuid = parsedGuid;
                }

                bool? isActive = null;
                if (!IsBasicUser && _statusFilter != "All")
                {
                    isActive = string.Equals(_statusFilter, "Valid", StringComparison.OrdinalIgnoreCase);
                }

                string sex = (!IsBasicUser && _sexFilter != "All") ? _sexFilter : null;

                string barangay = null;
                if (!string.IsNullOrWhiteSpace(barangayQuery))
                {
                    barangay = barangayQuery;
                }
                else if (!string.IsNullOrEmpty(effectiveBarangay) && effectiveBarangay != "All")
                {
                    barangay = effectiveBarangay;
                }

                string sortBy = "LastName";
                string sortOrder = "asc";

                switch (_sortOption)
                {
                    case "Name A-Z": sortBy = "LastName"; sortOrder = "asc"; break;
                    case "Name Z-A": sortBy = "LastName"; sortOrder = "desc"; break;
                    case "Newest": sortBy = "datecreated"; sortOrder = "desc"; break;
                    case "Oldest": sortBy = "datecreated"; sortOrder = "asc"; break;
                    case "Barangay": sortBy = "barangay"; sortOrder = "asc"; break;
                }

                var req = new GetSoloParentRequest
                {
                    Id = searchGuid,
                    Fullname = !string.IsNullOrWhiteSpace(nameQuery) ? nameQuery : null,
                    Barangay = barangay,
                    Sex = sex,
                    IsActive = isActive,
                    SortBy = sortBy,
                    SortOrder = sortOrder,
                    Page = _currentPage,
                    PageSize = PageSize
                };

                // Fetch the list page, then retrieve full details in parallel (the list
                // endpoint does not return complete PersonalInfo — detail endpoint does).
                var resp = await SoloParentApiService.Instance.GetSoloParentsAsync(req);

                if (!resp.Succeeded || resp.Data == null)
                {
                    string err = resp.Errors != null && resp.Errors.Count > 0
                        ? resp.Errors[0]
                        : "Unable to load records from backend.";
                    ToastNotification.Show("API Notice", err, ToastType.Warning);

                    _allRecords = new List<SoloParentRecordViewModel>();
                    _filteredRecords = new List<SoloParentRecordViewModel>();
                    RecordsList.ItemsSource = null;
                    if (NoResultsPanel != null) NoResultsPanel.Visibility = Visibility.Visible;
                    UpdatePagingUI();
                    return;
                }

                var paged = resp.Data;
                _totalPages = Math.Max(1, paged.TotalPages);

                // Fetch every record's full detail in parallel
                var detailTasks = paged.Items
                    .Select(item => SoloParentApiService.Instance.GetSoloParentByIdAsync(item.Id))
                    .ToList();
                var detailResponses = await Task.WhenAll(detailTasks);

                _allRecords = new List<SoloParentRecordViewModel>();
                for (int i = 0; i < paged.Items.Count; i++)
                {
                    var summary    = paged.Items[i];
                    var detailResp = detailResponses[i];

                    SoloParentRecord rec;
                    if (detailResp.Succeeded && detailResp.Data != null)
                    {
                        rec = SoloParentApiService.Instance.MapToRecord(detailResp.Data);
                    }
                    else
                    {
                        // Detail call failed — build a minimal record from summary fields
                        rec = new SoloParentRecord
                        {
                            Id          = summary.Id.ToString(),
                            Barangay    = summary.Barangay,
                            Sex         = summary.Sex,
                            Status      = summary.IsActive ? "Valid" : "Inactive",
                            LastUpdated = summary.DateCreated,
                        };
                        rec.Name = rec.Surname = rec.FirstName = rec.MiddleName = string.Empty;
                    }

                    var vm = new SoloParentRecordViewModel(rec);
                    vm.SetAlternate(i % 2 != 0);
                    _allRecords.Add(vm);
                }

                _filteredRecords = new List<SoloParentRecordViewModel>(_allRecords);
                RecordsList.ItemsSource = null;
                RecordsList.ItemsSource = _filteredRecords;

                if (NoResultsPanel != null)
                    NoResultsPanel.Visibility = _filteredRecords.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                UpdatePagingUI();
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Error", ex.Message, ToastType.Error);
            }
            finally
            {
                _isLoading = false;
                if (BtnRefreshRecords != null) BtnRefreshRecords.IsEnabled = true;
                if (PageLoadingOverlay != null) PageLoadingOverlay.IsLoading = false;
            }
        }

        private async void BtnRefreshRecords_Click(object sender, RoutedEventArgs e)
        {
            await LoadRecordsAsync();
        }

        private void ApplyFiltersAndPage()
        {
            _currentPage = 1;
            _ = LoadRecordsAsync();
        }

        private void ApplyFilters() => ApplyFiltersAndPage();

        private int TotalPages => _totalPages;

        private void UpdatePagingUI()
        {
            if (PageInfoText != null)
                PageInfoText.Text = "Page " + _currentPage + " of " + TotalPages;

            if (BtnBack != null)
                BtnBack.Visibility = _currentPage > 1 ? Visibility.Visible : Visibility.Collapsed;

            if (BtnNext != null)
                BtnNext.Visibility = _currentPage < TotalPages ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < TotalPages)
            {
                _currentPage++;
                _ = LoadRecordsAsync();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                _ = LoadRecordsAsync();
            }
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e) => ApplyFiltersAndPage();

        private void EncoderSearchBtn_Click(object sender, RoutedEventArgs e) => ApplyFiltersAndPage();

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ApplyFiltersAndPage();
        }

        private void SearchFields_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool hasText = !string.IsNullOrEmpty(SearchId?.Text)
                        || !string.IsNullOrEmpty(SearchName?.Text)
                        || !string.IsNullOrEmpty(SearchBarangay?.Text);

            if (ClearSearchBtn != null)
                ClearSearchBtn.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;

            if (!hasText)
                ApplyFiltersAndPage();
        }

        private void EncoderFields_TextChanged(object sender, TextChangedEventArgs e) { }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchId.Text = string.Empty;
            SearchName.Text = string.Empty;
            SearchBarangay.Text = string.Empty;
            ClearSearchBtn.Visibility = Visibility.Collapsed;
            ApplyFiltersAndPage();
        }

        private void FilterDrop_Click(object sender, RoutedEventArgs e) => FilterPopup.IsOpen = true;
        private void SortDrop_Click(object sender, RoutedEventArgs e) => SortPopup.IsOpen = true;

        private void FilterCombo_Changed(object sender, SelectionChangedEventArgs e) { }

        private void FilterApply_Click(object sender, RoutedEventArgs e)
        {
            string sexVal      = (CmbFilterSex?.SelectedItem      as ComboBoxItem)?.Content?.ToString() ?? "All";
            string barangayVal = (CmbFilterBarangay?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
            string statusVal   = (CmbFilterStatus?.SelectedItem   as ComboBoxItem)?.Content?.ToString() ?? "All";
            _sexFilter      = sexVal;
            _barangayFilter = barangayVal;
            _statusFilter   = statusVal;
            FilterPopup.IsOpen = false;
            ApplyFiltersAndPage();
        }

        private void FilterReset_Click(object sender, RoutedEventArgs e)
        {
            if (CmbFilterSex      != null) CmbFilterSex.SelectedIndex      = 0;
            if (CmbFilterBarangay != null) CmbFilterBarangay.SelectedIndex = 0;
            if (CmbFilterStatus   != null) CmbFilterStatus.SelectedIndex   = 0;
            _sexFilter      = "All";
            _barangayFilter = "All";
            _statusFilter   = "All";
            FilterPopup.IsOpen = false;
            ApplyFiltersAndPage();
        }

        private void StatusOpt_Click(object sender, MouseButtonEventArgs e) { }

        private void SexOpt_Click(object sender, MouseButtonEventArgs e) { }

        private void SortOpt_Click(object sender, MouseButtonEventArgs e)
        {
            _sortOption = (sender as FrameworkElement)?.Tag?.ToString() ?? "Barangay";
            SortPopup.IsOpen = false;
            ApplyFiltersAndPage();
        }

        private void RecordsList_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void RecordsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SoloParentRecordViewModel vm = RecordsList.SelectedItem as SoloParentRecordViewModel;
            if (vm == null) return;

            AuditLogService.Instance.LogView(vm.RawModel.Id, vm.Name, GetCurrentUser(), CurrentUserRole);
            OpenViewDialog(vm);
        }

        private async void OpenViewDialog(SoloParentRecordViewModel vm)
        {
            MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
                mainWindow.MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 };

            SoloParentRecord recordToView = vm.RawModel;

            if (Guid.TryParse(vm.Id, out var id))
            {
                var detailResp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(id);
                if (detailResp.Succeeded && detailResp.Data != null)
                {
                    recordToView = SoloParentApiService.Instance.MapToRecord(detailResp.Data);
                }
            }

            var view = new RecordViewDialog(recordToView) { Owner = Window.GetWindow(this) };
            view.Closed += (s, args) => { if (mainWindow != null) mainWindow.MainContent.Effect = null; };
            view.ShowDialog();

            if (view.Renewed)
            {
                await LoadRecordsAsync();
                return;
            }

            if (view.OpenedRenew)
            {
                await RenewRecordAsync(recordToView, view.RenewalResult);
                return;
            }

            if (view.OpenedEdit)
            {
                await EditRecordAsync(recordToView);
                return;
            }

            if (view.OpenedDelete)
            {
                await DeleteRecordAsync(recordToView.Id, recordToView.Name);
            }
        }

        private async void AddNewRecord_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
                mainWindow.MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 };

            var methodDialog = new EntryMethodDialog { Owner = Window.GetWindow(this) };
            bool? picked = methodDialog.ShowDialog();

            if (picked != true || methodDialog.Selected == EntryMethod.None)
            {
                if (mainWindow != null) mainWindow.MainContent.Effect = null;
                return;
            }

            if (methodDialog.Selected == EntryMethod.Scan)
            {
                var ocr = new OcrScanDialog { Owner = Window.GetWindow(this) };
                ocr.Closed += (s, args) => { if (mainWindow != null) mainWindow.MainContent.Effect = null; };
                
                if (ocr.ShowDialog() == true && ocr.Result != null)
                {
                    await SaveNewRecordAsync(ocr.Result);
                }
                return;
            }

            // Manual entry — run identity pre-check first
            var preCheck = new PreCheckDialog { Owner = Window.GetWindow(this) };
            bool? preResult = preCheck.ShowDialog();

            if (preResult != true)
            {
                if (mainWindow != null) mainWindow.MainContent.Effect = null;
                return;
            }

            // User clicked "View Existing Record" from the duplicate banner
            if (preCheck.ExistingRecordToView != null)
            {
                var viewer = new RecordViewDialog(preCheck.ExistingRecordToView) { Owner = Window.GetWindow(this) };
                viewer.Closed += (s, args) => { if (mainWindow != null) mainWindow.MainContent.Effect = null; };
                viewer.ShowDialog();
                return;
            }

            // No duplicate — open full form pre-filled with the primary data
            RecordDialog dialog = new RecordDialog(preCheck.PrimaryData) { Owner = Window.GetWindow(this) };
            dialog.Closed += (s, args) => { if (mainWindow != null) mainWindow.MainContent.Effect = null; };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                await SaveNewRecordAsync(dialog.Result);
            }
        }

        private async Task SaveNewRecordAsync(SoloParentRecord record)
        {
            record.CreatedBy = CurrentUserName;
            record.Barangay  = string.IsNullOrEmpty(record.Barangay) ? CurrentUserBarangay : record.Barangay;

            var createReq = SoloParentApiService.Instance.MapToCreateRequest(record);
            var createResp = await SoloParentApiService.Instance.CreateSoloParentAsync(createReq);

            if (!createResp.Succeeded)
            {
                string err = createResp.Errors != null && createResp.Errors.Count > 0
                    ? string.Join("\n", createResp.Errors)
                    : "Failed to create record on the server.";
                MessageBox.Show(err, "Creation Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            record.Id = createResp.Data.ToString();
            ToastNotification.Show("Record Added", record.Name + " was added successfully.", ToastType.Success);
            AuditLogService.Instance.LogCreate(record.Id, record.Name, GetCurrentUser(), CurrentUserRole);
            await LoadRecordsAsync();
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SoloParentRecordViewModel vm = _allRecords?.Find(r => r.Id == id);
            if (vm == null) return;

            if (IsBasicUser && !string.Equals(vm.RawModel.CreatedBy ?? string.Empty, CurrentUserName, StringComparison.OrdinalIgnoreCase))
            {
                ToastNotification.Show("Access Denied", "You can only edit records you created.", ToastType.Warning);
                return;
            }

            await EditRecordAsync(vm.RawModel);
        }

        private async Task RenewRecordAsync(SoloParentRecord original, SoloParentRecord updated)
        {
            if (!Guid.TryParse(original.Id, out var guid))
            {
                MessageBox.Show("Cannot renew: invalid record ID.", "Renew", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 1. Extend the validity window on the server
            var renewResp = await SoloParentApiService.Instance.RenewSoloParentRecordAsync(guid);
            if (!renewResp.Succeeded)
            {
                string err = renewResp.Errors != null && renewResp.Errors.Count > 0
                    ? string.Join("\n", renewResp.Errors) : "Failed to renew record.";
                MessageBox.Show(err, "Renew Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Persist the edited mutable fields (civil status, income, employment)
            var updateReq  = SoloParentApiService.Instance.MapToUpdateRequest(guid, updated);
            var updateResp = await SoloParentApiService.Instance.UpdateSoloParentAsync(guid, updateReq);
            if (!updateResp.Succeeded)
            {
                string err = updateResp.Errors != null && updateResp.Errors.Count > 0
                    ? string.Join("\n", updateResp.Errors) : "Failed to save updated fields.";
                MessageBox.Show(err, "Update Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newExpiry = DateTime.Today.AddYears(1).ToString("MMMM d, yyyy");
            ToastNotification.Show("Record Renewed",
                $"{original.Name} — valid until {newExpiry}.", ToastType.Success);
            AuditLogService.Instance.LogUpdate(original.Id, original.Name,
                GetCurrentUser(), CurrentUserRole, $"Record renewed. Valid until {newExpiry}.");
            await LoadRecordsAsync();
        }

        private async Task EditRecordAsync(SoloParentRecord rawRecord)
        {
            MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
                mainWindow.MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 };

            SoloParentDto existingDto = null;
            if (Guid.TryParse(rawRecord.Id, out var id))
            {
                var fetch = await SoloParentApiService.Instance.GetSoloParentByIdAsync(id);
                if (fetch.Succeeded && fetch.Data != null)
                {
                    existingDto = fetch.Data;
                    rawRecord = SoloParentApiService.Instance.MapToRecord(existingDto);
                }
            }

            RecordDialog dialog = new RecordDialog(rawRecord) { Owner = Window.GetWindow(this) };
            dialog.Closed += (s, args) => { if (mainWindow != null) mainWindow.MainContent.Effect = null; };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                if (Guid.TryParse(rawRecord.Id, out var guid))
                {
                    var updateReq = SoloParentApiService.Instance.MapToUpdateRequest(guid, dialog.Result, existingDto);
                    var updateResp = await SoloParentApiService.Instance.UpdateSoloParentAsync(guid, updateReq);
                    if (!updateResp.Succeeded)
                    {
                        string err = updateResp.Errors != null && updateResp.Errors.Count > 0
                            ? string.Join("\n", updateResp.Errors)
                            : "Failed to update record on the server.";
                        MessageBox.Show(err, "Update Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                ToastNotification.Show("Record Updated", dialog.Result.Name + " was updated.", ToastType.Info);
                AuditLogService.Instance.LogUpdate(dialog.Result.Id, dialog.Result.Name, GetCurrentUser(), CurrentUserRole,
                    BuildDiff(rawRecord, dialog.Result));
                await LoadRecordsAsync();
            }
        }

        private async void Renew_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            if (!Guid.TryParse(id, out var guid))
            {
                MessageBox.Show("Cannot renew record without a valid backend ID.", "Renew", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SoloParentRecordViewModel vm = _allRecords?.Find(r => r.Id == id);
            string recordName = vm != null ? vm.Name : id;

            var resp = await SoloParentApiService.Instance.RenewSoloParentRecordAsync(guid);
            if (resp.Succeeded)
            {
                ToastNotification.Show("Record Renewed", recordName + " validity extended for 1 year.", ToastType.Success);
                AuditLogService.Instance.LogUpdate(id, recordName, GetCurrentUser(), CurrentUserRole, "Validity extended for 1 year");
                await LoadRecordsAsync();
            }
            else
            {
                string err = resp.Errors != null && resp.Errors.Count > 0
                    ? string.Join("\n", resp.Errors)
                    : "Failed to renew record.";
                MessageBox.Show(err, "Renew Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (IsBasicUser)
            {
                ToastNotification.Show("Access Denied", "Basic users cannot delete records.", ToastType.Warning);
                return;
            }

            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SoloParentRecordViewModel vm = _allRecords?.Find(r => r.Id == id);
            string recordName = vm != null ? vm.Name : id;
            await DeleteRecordAsync(id, recordName);
        }

        private async Task DeleteRecordAsync(string id, string recordName)
        {
            if (IsBasicUser)
            {
                ToastNotification.Show("Access Denied", "Basic users cannot delete records.", ToastType.Warning);
                return;
            }

            if (!Guid.TryParse(id, out var guid))
            {
                ToastNotification.Show("Invalid ID", "Cannot delete record without a valid ID.", ToastType.Warning);
                return;
            }

            var deleteResp = await SoloParentApiService.Instance.DeleteSoloParentAsync(guid);
            if (!deleteResp.Succeeded)
            {
                string err = deleteResp.Errors != null && deleteResp.Errors.Count > 0
                    ? string.Join("\n", deleteResp.Errors)
                    : "Failed to delete record from server.";
                MessageBox.Show(err, "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ToastNotification.Show("Record Deleted", "The record was removed.", ToastType.Warning);
            AuditLogService.Instance.LogDelete(id, recordName, GetCurrentUser(), CurrentUserRole);
            await LoadRecordsAsync();
        }

        private void ExportRecord_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SoloParentRecordViewModel vm = _allRecords?.Find(r => r.Id == id);
            if (vm == null) return;

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = (vm.Id ?? "record") + "_" + (vm.Surname ?? "export"),
                DefaultExt = ".pdf",
                Filter = "PDF file (*.pdf)|*.pdf"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                SOLUM_UI.Services.SoloParentExportService.ExportToPdf(dlg.FileName, vm.RawModel);
                MessageBox.Show("Record exported to:\n" + dlg.FileName, "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed:\n" + ex.Message, "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e) => ResizeNameColumn();

        private void ResizeNameColumn()
        {
            if (RecordsList.View is GridView gv && gv.Columns.Count >= 9)
            {
                // Fixed columns (px)
                const double sex        =  80;
                const double civil      = 110;
                const double dob        = 108;
                const double lastUpd    = 108;
                const double validUntil = 108;
                const double status     =  90;
                const double actions    = 145;

                double available = RecordsList.ActualWidth - 2;
                if (available <= 0) return;

                double totalFixed = sex + civil + dob + lastUpd + validUntil + status + actions;
                double flex = Math.Max(220, available - totalFixed);

                // Col 0 = NAME  (wider), Col 1 = BARANGAY  (narrower)
                gv.Columns[0].Width = Math.Max(140, flex * 0.58);
                gv.Columns[1].Width = Math.Max(80,  flex * 0.42);
                gv.Columns[2].Width = sex;
                gv.Columns[3].Width = civil;
                gv.Columns[4].Width = dob;
                gv.Columns[5].Width = lastUpd;
                gv.Columns[6].Width = validUntil;
                gv.Columns[7].Width = status;
                gv.Columns[8].Width = actions;
            }
        }

        private void BasicSearchName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndPage();
        }

        private async void BasicUserAddRecord_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
                mainWindow.MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 };

            var methodDialog = new EntryMethodDialog { Owner = Window.GetWindow(this) };
            bool? picked = methodDialog.ShowDialog();

            if (picked != true || methodDialog.Selected == EntryMethod.None)
            {
                if (mainWindow != null) mainWindow.MainContent.Effect = null;
                return;
            }

            if (methodDialog.Selected == EntryMethod.Scan)
            {
                var ocr = new OcrScanDialog { Owner = Window.GetWindow(this) };
                ocr.Closed += (s, args) => { if (mainWindow != null) mainWindow.MainContent.Effect = null; };
                
                if (ocr.ShowDialog() == true && ocr.Result != null)
                {
                    await SaveNewRecordAsync(ocr.Result);
                }
                return;
            }

            // Manual entry — run identity pre-check first
            var preCheck = new PreCheckDialog { Owner = Window.GetWindow(this) };
            bool? preResult = preCheck.ShowDialog();

            if (preResult != true)
            {
                if (mainWindow != null) mainWindow.MainContent.Effect = null;
                return;
            }

            // User clicked "View Existing Record" from the duplicate banner
            if (preCheck.ExistingRecordToView != null)
            {
                var viewer = new RecordViewDialog(preCheck.ExistingRecordToView) { Owner = Window.GetWindow(this) };
                viewer.Closed += (s, args) => { if (mainWindow != null) mainWindow.MainContent.Effect = null; };
                viewer.ShowDialog();
                return;
            }

            // No duplicate — open full form pre-filled with the primary data
            RecordDialog dialog = new RecordDialog(preCheck.PrimaryData) { Owner = Window.GetWindow(this) };
            dialog.Closed += (s, args) => { if (mainWindow != null) mainWindow.MainContent.Effect = null; };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                await SaveNewRecordAsync(dialog.Result);
            }
        }

        private string GetCurrentUser() => !string.IsNullOrEmpty(CurrentUserName) ? CurrentUserName : "Admin";

        /// <summary>Compares two records field-by-field and returns a comma-separated summary of changed fields.</summary>
        private static string BuildDiff(SOLUM_UI.Models.SoloParentRecord before, SOLUM_UI.Models.SoloParentRecord after)
        {
            var changes = new System.Collections.Generic.List<string>();
            void Check(string label, string a, string b)
            {
                if (!string.Equals(a ?? "", b ?? "", StringComparison.Ordinal))
                    changes.Add(label + ": \"" + (a ?? "") + "\" → \"" + (b ?? "") + "\"");
            }
            Check("Last Name",    before.Surname,       after.Surname);
            Check("First Name",   before.FirstName,     after.FirstName);
            Check("Middle Name",  before.MiddleName,    after.MiddleName);
            Check("Extension",    before.ExtensionName, after.ExtensionName);
            Check("Sex",          before.Sex,           after.Sex);
            Check("Civil Status", before.CivilStatus,   after.CivilStatus);
            Check("Birthplace",   before.PlaceOfBirth,  after.PlaceOfBirth);
            Check("Address",      before.Address,       after.Address);
            Check("Barangay",     before.Barangay,      after.Barangay);
            Check("Contact",      before.ContactNumber, after.ContactNumber);
            Check("Status",       before.Status,        after.Status);
            Check("Education",    before.EducationalAttainment, after.EducationalAttainment);
            Check("Occupation",   before.Occupation,    after.Occupation);
            Check("Religion",     before.Religion,      after.Religion);
            if (before.DateOfBirth != after.DateOfBirth)
                changes.Add("Date of Birth: \"" + before.DateOfBirth.ToString("yyyy-MM-dd") + "\" → \"" + after.DateOfBirth.ToString("yyyy-MM-dd") + "\"");
            return string.Join("; ", changes);
        }

        public SoloParentRecordViewModel FindRecord(string spId)
        {
            return _allRecords?.Find(r => r.Id == spId);
        }

        public void HighlightRecord(string spId)
        {
            var vm = _allRecords?.Find(r => r.Id == spId);
            if (vm == null) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                RecordsList.SelectedItem = vm;
                RecordsList.ScrollIntoView(vm);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
}
