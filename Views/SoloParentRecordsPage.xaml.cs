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
        public static string CurrentUserName { get; set; } = string.Empty;
        public static string CurrentUserRole { get; set; } = string.Empty;
        public static string CurrentUserBarangay { get; set; } = string.Empty;

        private List<SoloParentRecordViewModel> _allRecords;
        private List<SoloParentRecordViewModel> _filteredRecords;

        private string _statusFilter = "All";
        private string _sexFilter = "All";
        private string _barangayFilter = "All";
        private string _sortOption = "Name A-Z";

        private int _pageSize = 10;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalCount = 0;
        private bool _isLoading = false;

        public static readonly DependencyProperty SearchPlaceholderProperty =
            DependencyProperty.Register(
                nameof(SearchPlaceholder),
                typeof(string),
                typeof(SoloParentRecordsPage),
                new PropertyMetadata("Search records (name, PhilSys ID)..."));

        public string SearchPlaceholder
        {
            get => (string)GetValue(SearchPlaceholderProperty);
            set => SetValue(SearchPlaceholderProperty, value);
        }

        private bool IsAdmin => string.Equals(CurrentUserRole, "Administrator", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(CurrentUserRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(CurrentUserRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                                    AuthApiService.Instance.IsAdmin;
        private bool IsBasicUser => string.Equals(CurrentUserRole, "BasicUser", StringComparison.OrdinalIgnoreCase);
        private bool IsEncoder => string.Equals(CurrentUserRole, "Encoder", StringComparison.OrdinalIgnoreCase);

        public SoloParentRecordsPage()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                ApplyRoleView();
                ResizeNameColumn();
                // Turn on loading overlay for non-Encoder users, or for Encoder users who have search fields filled
                if (!IsEncoder || HasSearchQuery())
                {
                    if (PageLoadingOverlay != null) PageLoadingOverlay.IsLoading = true;
                }
                _ = LoadRecordsAsync();
                _ = Task.Run(() => OcrService.EnsureServerRunningAsync(25));
            };
            SizeChanged += Page_SizeChanged;
            // Also recompute when the ListView itself changes size
            RecordsList.SizeChanged += (s, e) => ResizeNameColumn();
            RecordsList.Loaded += (s, e) => ResizeNameColumn();
        }

        private void ApplyRoleView()
        {
            if (IsBasicUser)
            {
                AdminToolbar.Visibility = Visibility.Collapsed;
                BasicUserToolbar.Visibility = Visibility.Visible;
                EncoderSearchPanel.Visibility = Visibility.Collapsed;
                RecordsClipGrid.Visibility = Visibility.Visible;
                PagingRow.Visibility = Visibility.Visible;
                _barangayFilter = CurrentUserBarangay;

                if (ColSex != null) ColSex.Width = 0;
                if (ColCivilStatus != null) ColCivilStatus.Width = 0;
                if (ColChildren != null) ColChildren.Width = 0;
                if (ColLastUpdated != null) ColLastUpdated.Width = 0;
                if (ColBarangay != null) ColBarangay.Width = 0;
                if (ColActions != null) ColActions.Width = 300;
            }
            else if (IsEncoder)
            {
                // Encoder users: search banner + tips first. The results table only appears after a search.
                AdminToolbar.Visibility = Visibility.Collapsed;
                BasicUserToolbar.Visibility = Visibility.Collapsed;
                EncoderSearchPanel.Visibility = Visibility.Visible;
                PagingRow.Visibility = Visibility.Collapsed;

                // if (PageSubtitle != null)
                //     PageSubtitle.Text = "Look up a registered solo parent, or register a new one.";
                // if (NoResultsAddBtn != null)
                //     NoResultsAddBtn.Visibility = Visibility.Visible;

                SetEncoderResultsVisible(false);
            }
            else
            {
                // Administrator: Show everything
                AdminToolbar.Visibility = Visibility.Visible;
                BasicUserToolbar.Visibility = Visibility.Collapsed;
                EncoderSearchPanel.Visibility = Visibility.Collapsed;
                RecordsClipGrid.Visibility = Visibility.Visible;
                PagingRow.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Encoder only. false = landing view (banner + tips, no table).
        /// true = results view (banner + results table, tips hidden).
        /// </summary>
        private void SetEncoderResultsVisible(bool showResults)
        {
            if (!IsEncoder) return;

            if (RecordsClipGrid != null)
                RecordsClipGrid.Visibility = showResults ? Visibility.Visible : Visibility.Collapsed;

            // if (EncoderTipsPanel != null)
            //     EncoderTipsPanel.Visibility = showResults ? Visibility.Collapsed : Visibility.Visible;
        }

        private async Task LoadRecordsAsync()
        {
            if (_isLoading) return;

            // For Encoder users: only load records if they have initiated a search
            if (IsEncoder && !HasSearchQuery())
            {
                _allRecords = new List<SoloParentRecordViewModel>();
                _filteredRecords = new List<SoloParentRecordViewModel>();
                if (RecordsList != null) RecordsList.ItemsSource = null;
                if (NoResultsPanel != null) NoResultsPanel.Visibility = Visibility.Collapsed;

                // Back to the landing view (banner + tips)
                SetEncoderResultsVisible(false);

                // Ensure loading overlay is turned off
                if (PageLoadingOverlay != null) PageLoadingOverlay.IsLoading = false;
                return;
            }

            _isLoading = true;
            if (BtnRefreshRecords != null) BtnRefreshRecords.IsEnabled = false;
            if (PageLoadingOverlay != null) PageLoadingOverlay.IsLoading = true;

            // Encoder with a search: show the results card (tips hide)
            SetEncoderResultsVisible(true);

            try
            {
                string effectiveBarangay = IsBasicUser ? CurrentUserBarangay : _barangayFilter;

                // Handle search query based on user role
                string query = string.Empty;
                Guid? searchGuid = null;
                string firstName = null;
                string lastName = null;
                string philsysId = null;
                string barangay = null;

                if (IsEncoder)
                {
                    // Encoder users: parse the unified search query
                    query = EncSearchQuery?.Text?.Trim() ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        // Get search mode from dropdown (if it exists)
                        string selectedMode = "AUTO";
                        if (EncSearchMode?.SelectedItem is ComboBoxItem modeItem)
                        {
                            selectedMode = modeItem.Tag?.ToString() ?? "AUTO";
                        }

                        // Try to parse as GUID first
                        if (Guid.TryParse(query, out var parsedGuid))
                        {
                            searchGuid = parsedGuid;
                        }
                        else
                        {
                            switch (selectedMode)
                            {
                                case "FIRST_NAME":
                                    firstName = query;
                                    break;

                                case "LAST_NAME":
                                    lastName = query;
                                    break;

                                case "FULL_NAME":
                                    // Parse as full name (last name, first name)
                                    if (query.Contains(","))
                                    {
                                        var parts = query.Split(new[] { ',' }, 2);
                                        lastName = parts[0].Trim();
                                        firstName = parts.Length > 1 ? parts[1].Trim() : null;
                                    }
                                    else if (query.Contains(" "))
                                    {
                                        int firstSpace = query.IndexOf(' ');
                                        firstName = query.Substring(0, firstSpace).Trim();
                                        lastName = query.Substring(firstSpace + 1).Trim();
                                    }
                                    else
                                    {
                                        // Single word: treat as last name so the search never runs unfiltered
                                        lastName = query;
                                    }
                                    break;

                                case "PHILSYS_ID":
                                    philsysId = query;
                                    break;

                                case "AUTO":
                                default:
                                    // Auto-detect: if digits + spaces/hyphens with >= 4 digits -> PhilSys ID
                                    string digitsOnly = System.Text.RegularExpressions.Regex.Replace(query, @"\D", "");
                                    if (digitsOnly.Length >= 4 && System.Text.RegularExpressions.Regex.IsMatch(query, @"^[\d\s\-]+$"))
                                    {
                                        philsysId = query;
                                    }
                                    else if (query.Contains(","))
                                    {
                                        // Comma format: "Dela Cruz, Juana" -> Last Name = "Dela Cruz", First Name = "Juana"
                                        var parts = query.Split(new[] { ',' }, 2);
                                        lastName = parts[0].Trim();
                                        firstName = parts.Length > 1 ? parts[1].Trim() : null;
                                    }
                                    else if (query.Contains(" "))
                                    {
                                        // Space format: "Juana Dela Cruz" -> First Name = "Juana", Last Name = "Dela Cruz"
                                        int firstSpace = query.IndexOf(' ');
                                        firstName = query.Substring(0, firstSpace).Trim();
                                        lastName = query.Substring(firstSpace + 1).Trim();
                                    }
                                    else
                                    {
                                        // Single word: assume last name
                                        lastName = query;
                                    }
                                    break;
                            }
                        }
                    }
                }
                else if (IsBasicUser)
                {
                    query = BasicSearchName?.Text?.Trim() ?? string.Empty;
                }
                else
                {
                    query = SearchName?.Text?.Trim() ?? string.Empty;
                }

                // Handle generic search query (for non-Encoder users)
                if (!IsEncoder && !string.IsNullOrWhiteSpace(query))
                {
                    if (Guid.TryParse(query, out var parsedGuid))
                    {
                        searchGuid = parsedGuid;
                    }
                    else
                    {
                        string selectedMode = "AUTO";
                        if (!IsBasicUser && CmbSearchMode?.SelectedItem is ComboBoxItem modeItem)
                        {
                            selectedMode = modeItem.Tag?.ToString() ?? "AUTO";
                        }

                        switch (selectedMode)
                        {
                            case "FIRST_NAME":
                                firstName = query;
                                break;

                            case "LAST_NAME":
                                lastName = query;
                                break;

                            case "FULL_NAME":
                                // fullname handled separately below
                                break;

                            case "PHILSYS_ID":
                                philsysId = query;
                                break;

                            case "AUTO":
                            default:
                                // If input consists only of digits and hyphens/spaces and has >= 4 digits, treat as PhilSys ID
                                string digitsOnly = System.Text.RegularExpressions.Regex.Replace(query, @"\D", "");
                                if (digitsOnly.Length >= 4 && System.Text.RegularExpressions.Regex.IsMatch(query, @"^[\d\s\-]+$"))
                                {
                                    philsysId = query;
                                }
                                else if (query.Contains(","))
                                {
                                    // Comma format: "Dela Cruz, Juana" -> Last Name = "Dela Cruz", First Name = "Juana"
                                    var parts = query.Split(new[] { ',' }, 2);
                                    lastName = parts[0].Trim();
                                    firstName = parts.Length > 1 ? parts[1].Trim() : null;
                                }
                                else if (query.Contains(" "))
                                {
                                    // Space format: "Juana Dela Cruz" -> First Name = "Juana", Last Name = "Dela Cruz"
                                    int firstSpace = query.IndexOf(' ');
                                    firstName = query.Substring(0, firstSpace).Trim();
                                    lastName = query.Substring(firstSpace + 1).Trim();
                                }
                                break;
                        }
                    }
                }

                bool? isActive = null;
                if (!IsEncoder && !IsBasicUser && _statusFilter != "All")
                {
                    isActive = string.Equals(_statusFilter, "Valid", StringComparison.OrdinalIgnoreCase);
                }

                string sex = (!IsEncoder && !IsBasicUser && _sexFilter != "All") ? _sexFilter : null;

                // For non-Encoder users, apply barangay filter
                if (!IsEncoder && string.IsNullOrEmpty(barangay))
                {
                    if (!string.IsNullOrEmpty(effectiveBarangay) && effectiveBarangay != "All")
                    {
                        barangay = effectiveBarangay;
                    }
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
                    FirstName = firstName,
                    LastName = lastName,
                    PhilsysId = philsysId,
                    Barangay = barangay,
                    Sex = sex,
                    IsActive = isActive,
                    SortBy = sortBy,
                    SortOrder = sortOrder,
                    Page = _currentPage,
                    PageSize = Math.Min(100, Math.Max(10, _pageSize))
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
                _totalCount = paged.TotalCount;
                _totalPages = Math.Max(1, paged.TotalPages);

                // Fetch every record's full detail in parallel
                var detailTasks = paged.Items
                    .Select(item => SoloParentApiService.Instance.GetSoloParentByIdAsync(item.Id))
                    .ToList();
                var detailResponses = await Task.WhenAll(detailTasks);

                _allRecords = new List<SoloParentRecordViewModel>();
                for (int i = 0; i < paged.Items.Count; i++)
                {
                    var summary = paged.Items[i];
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
                            Id = summary.Id.ToString(),
                            Barangay = summary.Barangay,
                            Sex = summary.Sex,
                            Status = summary.IsActive ? "Valid" : "Inactive",
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
                PageInfoText.Text = $"{_currentPage} of {TotalPages}";

            if (BtnBack != null)
            {
                BtnBack.Visibility = Visibility.Visible;
                BtnBack.IsEnabled = _currentPage > 1;
            }

            if (BtnNext != null)
            {
                BtnNext.Visibility = Visibility.Visible;
                BtnNext.IsEnabled = _currentPage < TotalPages;
            }
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

        private void CmbPageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbPageSize?.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out int newSize))
            {
                _pageSize = Math.Min(100, Math.Max(10, newSize));
                _currentPage = 1;
                _ = LoadRecordsAsync();
            }
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e) => ApplyFiltersAndPage();

        private void EncoderSearchBtn_Click(object sender, RoutedEventArgs e) => ApplyFiltersAndPage();

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ApplyFiltersAndPage();
        }

        /// <summary>Updates the search box hint to match the selected search type.</summary>
        private void UpdateSearchPlaceholder(string mode)
        {
            switch (mode)
            {
                case "FIRST_NAME":
                    SearchPlaceholder = "Search by first name (e.g. Juana)...";
                    break;
                case "LAST_NAME":
                    SearchPlaceholder = "Search by last name (e.g. Dela Cruz)...";
                    break;
                case "FULL_NAME":
                    SearchPlaceholder = "Search by full name (e.g. Juana Dela Cruz)...";
                    break;
                case "PHILSYS_ID":
                    SearchPlaceholder = "Search by PhilSys ID (e.g. 1234-5678-9012)...";
                    break;
                case "AUTO":
                default:
                    SearchPlaceholder = "Search records (name, PhilSys ID)...";
                    break;
            }
        }

        private void CmbSearchMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbSearchMode?.SelectedItem is ComboBoxItem item)
            {
                UpdateSearchPlaceholder(item.Tag?.ToString() ?? "AUTO");

                if (!string.IsNullOrWhiteSpace(SearchName?.Text))
                {
                    ApplyFiltersAndPage();
                }
            }
        }

        private void EncSearchMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EncSearchMode?.SelectedItem is ComboBoxItem item)
            {
                UpdateSearchPlaceholder(item.Tag?.ToString() ?? "AUTO");

                // Re-run only if the encoder already typed something
                if (IsLoaded && !string.IsNullOrWhiteSpace(EncSearchQuery?.Text))
                {
                    ApplyFiltersAndPage();
                }
            }
        }

        private void SearchFields_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool hasText = !string.IsNullOrEmpty(SearchName?.Text);

            if (ClearSearchBtn != null)
                ClearSearchBtn.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;

            if (!hasText)
                ApplyFiltersAndPage();
        }

        private void EncoderFields_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool hasText = !string.IsNullOrEmpty(EncSearchQuery?.Text);

            // if (EncClearBtn != null)
            //     EncClearBtn.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;

            // Emptying the box returns the encoder to the landing view
            if (!hasText && IsLoaded)
                ApplyFiltersAndPage();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            if (IsEncoder)
            {
                // Clear Encoder search field (unified query)
                if (EncSearchQuery != null) EncSearchQuery.Text = string.Empty;
                // if (EncClearBtn != null) EncClearBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Clear admin/basic user search field
                if (SearchName != null) SearchName.Text = string.Empty;
                if (ClearSearchBtn != null) ClearSearchBtn.Visibility = Visibility.Collapsed;
            }

            ApplyFiltersAndPage();
        }

        private void FilterDrop_Click(object sender, RoutedEventArgs e) => FilterPopup.IsOpen = true;
        private void SortDrop_Click(object sender, RoutedEventArgs e) => SortPopup.IsOpen = true;

        private void FilterCombo_Changed(object sender, SelectionChangedEventArgs e) { }

        private void FilterApply_Click(object sender, RoutedEventArgs e)
        {
            string sexVal = (CmbFilterSex?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
            string barangayVal = (CmbFilterBarangay?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
            string statusVal = (CmbFilterStatus?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
            _sexFilter = sexVal;
            _barangayFilter = barangayVal;
            _statusFilter = statusVal;
            FilterPopup.IsOpen = false;
            ApplyFiltersAndPage();
        }

        private void FilterReset_Click(object sender, RoutedEventArgs e)
        {
            if (CmbFilterSex != null) CmbFilterSex.SelectedIndex = 0;
            if (CmbFilterBarangay != null) CmbFilterBarangay.SelectedIndex = 0;
            if (CmbFilterStatus != null) CmbFilterStatus.SelectedIndex = 0;
            _sexFilter = "All";
            _barangayFilter = "All";
            _statusFilter = "All";
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

        /// <summary>Eye button in the Actions column: same as double-clicking the row.</summary>
        private void ViewRecord_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SoloParentRecordViewModel vm = _allRecords?.Find(r => r.Id == id);
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

            if (view.OpenedRenew)
            {
                if (view.IsQuickRenew || view.RenewalResult == null)
                {
                    await RenewRecordQuickAsync(recordToView, skipConfirmation: true);
                }
                else
                {
                    await RenewRecordAsync(recordToView, view.RenewalResult);
                }
                return;
            }

            if (view.Renewed)
            {
                await LoadRecordsAsync();
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

            // Manual entry — open 3-step wizard (Step 1 performs validation & duplicate check)
            RecordDialog dialog = new RecordDialog { Owner = Window.GetWindow(this) };
            dialog.Closed += (s, args) => { if (mainWindow != null) mainWindow.MainContent.Effect = null; };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                await SaveNewRecordAsync(dialog.Result);
            }
        }

        private async Task SaveNewRecordAsync(SoloParentRecord record)
        {
            // Show loading indicator
            PageLoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                record.CreatedBy = CurrentUserName;
                // Use safe barangay: if record doesn't have one, use user's barangay or fallback to "ALL"
                record.Barangay = string.IsNullOrWhiteSpace(record.Barangay)
                    ? (!string.IsNullOrWhiteSpace(CurrentUserBarangay) ? CurrentUserBarangay : "ALL")
                    : record.Barangay;

                var createReq = SoloParentApiService.Instance.MapToCreateRequest(record);
                var createResp = await SoloParentApiService.Instance.CreateSoloParentAsync(createReq);

                if (!createResp.Succeeded)
                {
                    string err = createResp.Errors != null && createResp.Errors.Count > 0
                        ? string.Join("\n", createResp.Errors)
                        : "Failed to create record on the server.";
                    Views.Dialogs.ActionConfirmDialog.Error(
                        "Creation Failed",
                        err);
                    return;
                }

                record.Id = createResp.Data.ToString();
                ToastNotification.Show("Record Added", record.Name + " was added successfully.", ToastType.Success);
                AuditLogService.Instance.LogCreate(record.Id, record.Name, GetCurrentUser(), CurrentUserRole);
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("SubsidyRecommendation");
                await LoadRecordsAsync();
            }
            finally
            {
                // Hide loading indicator
                PageLoadingOverlay.Visibility = Visibility.Collapsed;
            }
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

        private async Task RenewRecordQuickAsync(SoloParentRecord record, bool skipConfirmation = false)
        {
            if (record == null || !Guid.TryParse(record.Id, out var guid))
            {
                Views.Dialogs.ActionConfirmDialog.Warning(
                    "Renew",
                    "Cannot renew: invalid record ID.");
                return;
            }

            if (!skipConfirmation)
            {
                string personName = !string.IsNullOrWhiteSpace(record.Name) ? record.Name : "this solo parent";
                string nextExpiry = DateTime.Today.AddYears(1).ToString("MMMM d, yyyy");
                const string renewIcon = "M12,4V1L8,5L12,9V6C15.31,6 18,8.69 18,12C18,13.04 17.73,14.01 17.27,14.86L18.74,16.33C19.54,15.08 20,13.6 20,12C20,7.58 16.42,4 12,4M12,18C8.69,18 6,15.31 6,12C6,10.96 6.27,9.99 6.73,9.14L5.26,7.67C4.46,8.92 4,10.4 4,12C4,16.42 7.58,20 12,20V23L16,19L12,15V18Z";

                bool isConfirmed = Views.Dialogs.ActionConfirmDialog.Show(
                    title: "Confirm Quick Renewal",
                    message: $"Are you sure you want to renew the solo parent ID card for {personName}?\n\nThis will automatically extend record validity for 1 year until {nextExpiry} with no changes reported.",
                    confirmText: "Renew Record",
                    cancelText: "Cancel",
                    theme: Views.Dialogs.ConfirmThemeType.Success,
                    owner: Window.GetWindow(this),
                    customIconData: renewIcon);

                if (!isConfirmed) return;
            }

            PageLoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var resp = await SoloParentApiService.Instance.RenewSoloParentRecordAsync(guid, null);
                if (resp.Succeeded)
                {
                    string newExpiry = (resp.Data != null && DateTime.TryParse(resp.Data.ExpiresAt, out var expDt))
                        ? expDt.ToString("MMMM d, yyyy")
                        : DateTime.Today.AddYears(1).ToString("MMMM d, yyyy");

                    ToastNotification.Show("Record Renewed", $"{record.Name} — valid until {newExpiry}.", ToastType.Success);
                    AuditLogService.Instance.LogUpdate(record.Id, record.Name, GetCurrentUser(), CurrentUserRole, $"Record quick renewed. Valid until {newExpiry}.");
                    (Application.Current.MainWindow as MainWindow)?.ClearPageCache("SubsidyRecommendation");
                    await LoadRecordsAsync();
                }
                else
                {
                    string err = resp.Errors != null && resp.Errors.Count > 0
                        ? string.Join("\n", resp.Errors)
                        : "Failed to renew record.";
                    Views.Dialogs.ActionConfirmDialog.Error(
                        "Renew Failed",
                        err);
                }
            }
            finally
            {
                PageLoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task RenewRecordAsync(SoloParentRecord original, SoloParentRecord updated, SoloParentDto existingDto = null)
        {
            if (!Guid.TryParse(original.Id, out var guid))
            {
                Views.Dialogs.ActionConfirmDialog.Warning(
                    "Renew",
                    "Cannot renew: invalid record ID.");
                return;
            }

            PageLoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                if (existingDto == null)
                {
                    var fetch = await SoloParentApiService.Instance.GetSoloParentByIdAsync(guid);
                    if (fetch.Succeeded && fetch.Data != null)
                    {
                        existingDto = fetch.Data;
                    }
                }

                var renewReq = SoloParentApiService.Instance.MapToRenewRequest(guid, updated, existingDto);
                var renewResp = await SoloParentApiService.Instance.RenewSoloParentRecordAsync(guid, renewReq);

                if (!renewResp.Succeeded)
                {
                    string err = renewResp.Errors != null && renewResp.Errors.Count > 0
                        ? string.Join("\n", renewResp.Errors) : "Failed to renew record.";
                    Views.Dialogs.ActionConfirmDialog.Error(
                        "Renew Failed",
                        err);
                    return;
                }

                string newExpiry = (renewResp.Data != null && DateTime.TryParse(renewResp.Data.ExpiresAt, out var expDt))
                    ? expDt.ToString("MMMM d, yyyy")
                    : DateTime.Today.AddYears(1).ToString("MMMM d, yyyy");

                ToastNotification.Show("Record Renewed",
                    $"{original.Name} — valid until {newExpiry}.", ToastType.Success);

                var changeService = new ChangeTrackingService();
                var changes = changeService.DetectChanges(original, updated);
                if (changes.Count > 0)
                {
                    AuditLogService.Instance.LogUpdateWithChanges(original.Id, original.Name, GetCurrentUser(), CurrentUserRole, changes);
                }
                else
                {
                    AuditLogService.Instance.LogUpdate(original.Id, original.Name,
                        GetCurrentUser(), CurrentUserRole, $"Record renewed. Valid until {newExpiry}.");
                }

                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("SubsidyRecommendation");
                await LoadRecordsAsync();
            }
            finally
            {
                PageLoadingOverlay.Visibility = Visibility.Collapsed;
            }
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
                // Show loading indicator
                PageLoadingOverlay.Visibility = Visibility.Visible;

                try
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
                            Views.Dialogs.ActionConfirmDialog.Error(
                                "Update Failed",
                                err);
                            return;
                        }
                    }

                    ToastNotification.Show("Record Updated", dialog.Result.Name + " was updated.", ToastType.Info);
                    var changeList = BuildChangeList(rawRecord, dialog.Result);
                    AuditLogService.Instance.LogUpdateWithChanges(dialog.Result.Id, dialog.Result.Name,
                        GetCurrentUser(), CurrentUserRole, changeList);
                    await LoadRecordsAsync();
                }
                finally
                {
                    // Hide loading indicator
                    PageLoadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void Renew_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            if (!Guid.TryParse(id, out var guid))
            {
                Views.Dialogs.ActionConfirmDialog.Info(
                    "Renew",
                    "Cannot renew record without a valid backend ID.");
                return;
            }

            SoloParentRecordViewModel vm = _allRecords?.Find(r => r.Id == id);
            string recordName = vm != null ? vm.Name : id;

            var methodDialog = new RenewalMethodDialog
            {
                Owner = Window.GetWindow(this)
            };
            if (methodDialog.ShowDialog() != true) return;

            if (methodDialog.SelectedChoice == RenewalChoice.QuickRenew)
            {
                await RenewRecordQuickAsync(vm?.RawModel ?? new SoloParentRecord { Id = id, Surname = recordName });
                return;
            }

            if (methodDialog.SelectedChoice == RenewalChoice.ReassessAndRenew)
            {
                SoloParentRecord rawRecord = vm?.RawModel;
                SoloParentDto existingDto = null;
                var fetch = await SoloParentApiService.Instance.GetSoloParentByIdAsync(guid);
                if (fetch.Succeeded && fetch.Data != null)
                {
                    existingDto = fetch.Data;
                    rawRecord = SoloParentApiService.Instance.MapToRecord(existingDto);
                }

                if (rawRecord == null) rawRecord = new SoloParentRecord { Id = id, Surname = recordName };

                var dialog = new RenewalReassessmentDialog(rawRecord, existingDto)
                {
                    Owner = Window.GetWindow(this)
                };
                if (dialog.ShowDialog() == true && dialog.UpdatedRecord != null)
                {
                    await RenewRecordAsync(rawRecord, dialog.UpdatedRecord, existingDto);
                }
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin)
            {
                ToastNotification.Show("Access Denied", "Encoders cannot delete records. Only administrators have permission to delete records.", ToastType.Warning);
                return;
            }

            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SoloParentRecordViewModel vm = _allRecords?.Find(r => r.Id == id);
            string recordName = vm != null ? vm.Name : id;
            await DeleteRecordAsync(id, recordName);
        }

        private async Task DeleteRecordAsync(string id, string recordName)
        {
            if (!IsAdmin)
            {
                ToastNotification.Show("Access Denied", "Encoders cannot delete records. Only administrators have permission to delete records.", ToastType.Warning);
                return;
            }

            if (!Guid.TryParse(id, out var guid))
            {
                ToastNotification.Show("Invalid ID", "Cannot delete record without a valid ID.", ToastType.Warning);
                return;
            }

            bool isConfirmed = ActionConfirmDialog.Show(
                title: "Delete Record",
                message: $"Are you sure you want to permanently delete the record for {recordName}? This action cannot be undone.",
                confirmText: "Delete Record",
                cancelText: "Cancel",
                theme: ConfirmThemeType.Danger,
                owner: Window.GetWindow(this));

            if (!isConfirmed) return;

            // Show loading indicator
            PageLoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                var deleteResp = await SoloParentApiService.Instance.DeleteSoloParentAsync(guid);
                if (!deleteResp.Succeeded)
                {
                    string err = deleteResp.Errors != null && deleteResp.Errors.Count > 0
                        ? string.Join("\n", deleteResp.Errors)
                        : "Failed to delete record from server.";
                    Views.Dialogs.ActionConfirmDialog.Error(
                        "Delete Failed",
                        err);
                    return;
                }

                ToastNotification.Show("Record Deleted", "The record was removed.", ToastType.Warning);
                AuditLogService.Instance.LogDelete(id, recordName, GetCurrentUser(), CurrentUserRole);
                await LoadRecordsAsync();
            }
            finally
            {
                // Hide loading indicator
                PageLoadingOverlay.Visibility = Visibility.Collapsed;
            }
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
                Views.Dialogs.ActionConfirmDialog.Success(
                    "Export Complete",
                    "Record exported to:\n" + dlg.FileName);
            }
            catch (Exception ex)
            {
                Views.Dialogs.ActionConfirmDialog.Error(
                    "Export Error",
                    "Export failed:\n" + ex.Message);
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e) => ResizeNameColumn();

        private void ResizeNameColumn()
        {
            if (RecordsList.View is GridView gv && gv.Columns.Count >= 9)
            {
                // Fixed columns (px)
                const double sex = 80;
                const double civil = 110;
                const double dob = 108;
                const double lastUpd = 108;
                const double validUntil = 108;
                const double status = 90;
                const double actions = 145;

                double available = RecordsList.ActualWidth - 2;
                if (available <= 0) return;

                double totalFixed = sex + civil + dob + lastUpd + validUntil + status + actions;
                double flex = Math.Max(220, available - totalFixed);

                // Col 0 = NAME  (wider), Col 1 = BARANGAY  (narrower)
                gv.Columns[0].Width = Math.Max(140, flex * 0.58);
                gv.Columns[1].Width = Math.Max(80, flex * 0.42);
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

        private bool HasSearchQuery()
        {
            try
            {
                if (IsEncoder)
                {
                    // Check Encoder search field (unified query)
                    return EncSearchQuery != null && !string.IsNullOrEmpty(EncSearchQuery.Text?.Trim());
                }
                else if (IsBasicUser)
                {
                    return BasicSearchName != null && !string.IsNullOrEmpty(BasicSearchName.Text?.Trim());
                }
                else
                {
                    return SearchName != null && !string.IsNullOrEmpty(SearchName.Text?.Trim());
                }
            }
            catch
            {
                // If there's any error checking search fields, assume no query
                return false;
            }
        }

        /// <summary>
        /// Compares two records field-by-field and returns a structured list of
        /// <see cref="SOLUM_UI.Models.ChangeDetail"/> entries — one per changed field.
        /// </summary>
        private static List<SOLUM_UI.Models.ChangeDetail> BuildChangeList(
            SOLUM_UI.Models.SoloParentRecord before,
            SOLUM_UI.Models.SoloParentRecord after)
        {
            var changes = new List<SOLUM_UI.Models.ChangeDetail>();

            void Check(string label, string a, string b)
            {
                string oldVal = a ?? string.Empty;
                string newVal = b ?? string.Empty;
                if (!string.Equals(oldVal, newVal, StringComparison.Ordinal))
                    changes.Add(new SOLUM_UI.Models.ChangeDetail(label, oldVal, newVal));
            }

            Check("Last Name", before.Surname, after.Surname);
            Check("First Name", before.FirstName, after.FirstName);
            Check("Middle Name", before.MiddleName, after.MiddleName);
            Check("Extension", before.ExtensionName, after.ExtensionName);
            Check("Sex", before.Sex, after.Sex);
            Check("Civil Status", before.CivilStatus, after.CivilStatus);
            Check("Birthplace", before.PlaceOfBirth, after.PlaceOfBirth);
            Check("Address", before.Address, after.Address);
            Check("Barangay", before.Barangay, after.Barangay);
            Check("Contact", before.ContactNumber, after.ContactNumber);
            Check("Status", before.Status, after.Status);
            Check("Education", before.EducationalAttainment, after.EducationalAttainment);
            Check("Occupation", before.Occupation, after.Occupation);
            Check("Religion", before.Religion, after.Religion);
            Check("Monthly Income", before.MonthlyIncome, after.MonthlyIncome);
            bool circChanged =
                before.CircumstanceA1 != after.CircumstanceA1 ||
                before.CircumstanceA2 != after.CircumstanceA2 ||
                before.CircumstanceA3 != after.CircumstanceA3 ||
                before.CircumstanceA4 != after.CircumstanceA4 ||
                before.CircumstanceA5 != after.CircumstanceA5 ||
                before.CircumstanceA6 != after.CircumstanceA6 ||
                before.CircumstanceA7 != after.CircumstanceA7 ||
                before.CircumstanceB != after.CircumstanceB ||
                before.CircumstanceC != after.CircumstanceC ||
                before.CircumstanceD != after.CircumstanceD ||
                before.CircumstanceE != after.CircumstanceE ||
                before.CircumstanceF != after.CircumstanceF;

            if (!circChanged)
            {
                if (after.CircumstanceA2)
                    circChanged = !string.Equals((before.CircumstanceA2Cause ?? "").Trim(), (after.CircumstanceA2Cause ?? "").Trim(), StringComparison.OrdinalIgnoreCase) ||
                                  before.CircumstanceA2Date.Date != after.CircumstanceA2Date.Date;
                else if (after.CircumstanceA4)
                    circChanged = !string.Equals((before.CircumstanceA4Disability ?? "").Trim(), (after.CircumstanceA4Disability ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
                else if (after.CircumstanceA5)
                    circChanged = !string.Equals((before.CircumstanceA5Period ?? "").Trim(), (after.CircumstanceA5Period ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
                else if (after.CircumstanceA6)
                    circChanged = before.CircumstanceA6Nullity != after.CircumstanceA6Nullity || before.CircumstanceA6Annulment != after.CircumstanceA6Annulment;
                else if (after.CircumstanceB)
                    circChanged = !string.Equals((before.CircumstanceBStayAbroad ?? "").Trim(), (after.CircumstanceBStayAbroad ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
            }

            if (circChanged)
            {
                changes.Add(new SOLUM_UI.Models.ChangeDetail("Circumstances / Problem Presented", before.CircumstancesDisplay, after.CircumstancesDisplay));
            }
            Check("PhilSys Number", before.PhilSysNumber, after.PhilSysNumber);
            Check("Emergency Contact", before.EmergencyContactName, after.EmergencyContactName);
            Check("Emergency Number", before.EmergencyContactNumber, after.EmergencyContactNumber);
            Check("Needs & Problems", before.NeedsAndProblems, after.NeedsAndProblems);
            Check("Other Sources of Income", before.OtherIncomeSource, after.OtherIncomeSource);

            if (before.DateOfBirth != after.DateOfBirth)
                changes.Add(new SOLUM_UI.Models.ChangeDetail(
                    "Date of Birth",
                    before.DateOfBirth != DateTime.MinValue ? before.DateOfBirth.ToString("yyyy-MM-dd") : "—",
                    after.DateOfBirth != DateTime.MinValue ? after.DateOfBirth.ToString("yyyy-MM-dd") : "—"));

            return changes;
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