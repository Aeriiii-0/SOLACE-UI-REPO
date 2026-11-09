using System.Windows.Threading;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.ViewModels;
using SOLUM_UI.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public static string RequestedStatusFilter { get; set; } = null;

        private List<SoloParentRecordViewModel> _allRecords;

        private List<SoloParentRecordViewModel> _filteredRecords;

        private string _statusFilter = "All";

        private string _sexFilter = "All";

        private string _barangayFilter = "All";

        private bool? _isPantawidFilter = null;

        private bool? _isLGBTFilter = null;

        private bool? _isIndigenousFilter = null;

        private int _pageSize = 10;

        private int _currentPage = 1;

        private DispatcherTimer _pageResizeDebounceTimer;

        private int _totalPages = 1;

        private int _totalCount = 0;

        private bool _isLoading = false;

        private static readonly TimeSpan DataFreshnessTtl = TimeSpan.FromMinutes(2);

        private static readonly TimeSpan RefreshCooldown = TimeSpan.FromSeconds(2);

        private DateTime _lastLoadedTime = DateTime.MinValue;

        private DateTime _lastRefreshClickTime = DateTime.MinValue;

        private bool _hasLoadedData = false;

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

        // -- Notification card properties --------------------------------------
        private bool   _isNotificationVisible;
        private string _notificationTitle;
        private string _notificationMessage;
        private bool   _isNotificationSuccess;
        private DispatcherTimer _notifyTimer;

        public bool   IsNotificationVisible
        {
            get => _isNotificationVisible;
            private set { _isNotificationVisible = value; OnPropertyChanged(nameof(IsNotificationVisible)); }
        }
        public string NotificationTitle
        {
            get => _notificationTitle;
            private set { _notificationTitle = value; OnPropertyChanged(nameof(NotificationTitle)); }
        }
        public string NotificationMessage
        {
            get => _notificationMessage;
            private set { _notificationMessage = value; OnPropertyChanged(nameof(NotificationMessage)); }
        }
        public bool IsNotificationSuccess
        {
            get => _isNotificationSuccess;
            private set { _isNotificationSuccess = value; OnPropertyChanged(nameof(IsNotificationSuccess)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void ShowNotification(string title, string message, bool isSuccess)
        {
            NotificationTitle     = title;
            NotificationMessage   = message;
            IsNotificationSuccess = isSuccess;
            IsNotificationVisible = true;
            _notifyTimer?.Stop();
            _notifyTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _notifyTimer.Tick += (s, e) => { _notifyTimer.Stop(); IsNotificationVisible = false; };
            _notifyTimer.Start();

            var owner = Window.GetWindow(this) ?? Application.Current.MainWindow;
            if (isSuccess)
            {
                Views.Dialogs.ActionConfirmDialog.Success(title, message, owner);
            }
            else
            {
                Views.Dialogs.ActionConfirmDialog.Error(title, message, owner);
            }
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
                // Apply requested status filter from navigation
                if (!string.IsNullOrEmpty(RequestedStatusFilter))
                {
                    _statusFilter = RequestedStatusFilter;
                    RequestedStatusFilter = null; // Clear for next navigation
                    // Select the corresponding radio button
                    if (_statusFilter == "ExpiringSoon")
                    {
                        TabStatusExpiring.IsChecked = true;
                    }
                    else if (_statusFilter == "Active")
                    {
                        TabStatusActive.IsChecked = true;
                    }
                    else if (_statusFilter == "Expired" || _statusFilter == "Inactive")
                    {
                        TabStatusExpired.IsChecked = true;
                    }
                    else
                    {
                        TabStatusAll.IsChecked = true;
                    }
                    if (!IsEncoder)
                    {
                        _currentPage = 1;
                        if (RecordsSkeletonLoader != null) RecordsSkeletonLoader.Visibility = Visibility.Visible;
                        _ = LoadRecordsAsync();
                    }
                }
                else
                {
                    // No filter requested, load records normally (but not for Encoders � they must search first)
                    bool isStale = !_hasLoadedData || (DateTime.UtcNow - _lastLoadedTime) > DataFreshnessTtl;
                    if (isStale && !IsEncoder)
                    {
                        if (RecordsSkeletonLoader != null) RecordsSkeletonLoader.Visibility = Visibility.Visible;
                        _ = LoadRecordsAsync();
                    }
                }
                _ = Task.Run(() => OcrService.EnsureServerRunningAsync(25));

            };
            InitDynamicPaging();
            SizeChanged += Page_SizeChanged;
            // Also recompute when the ListView itself changes size
            RecordsList.SizeChanged += (s, e) =>
            {
                ResizeNameColumn();
                UpdateDynamicPageSize();

            };
            RecordsList.Loaded += (s, e) =>
            {
                ResizeNameColumn();
                UpdateDynamicPageSize();

            };

        }

        private void InitDynamicPaging()
        {
            _pageResizeDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)

            };
            _pageResizeDebounceTimer.Tick += (s, e) =>
            {
                _pageResizeDebounceTimer.Stop();
                if (_hasLoadedData && !_isLoading)
                {
                    _currentPage = 1;
                    _ = LoadRecordsAsync();

                }

            };

        }

        private void UpdateDynamicPageSize()
        {

            double availableHeight = RecordsList.ActualHeight;
            if (availableHeight <= 100) return;
            // GridView column header is 46px + 1px border. Leave 4px safety buffer.

            double rowsAvailable = availableHeight - 51;
            if (rowsAvailable <= 0) return;
            // Each row is locked to 48px height.

            int calculated = (int)Math.Floor(rowsAvailable / 48.0);

            int optimal = Math.Max(4, Math.Min(50, calculated));
            if (optimal != _pageSize)
            {
                _pageSize = optimal;
                if (_hasLoadedData)
                {
                    _pageResizeDebounceTimer.Stop();
                    _pageResizeDebounceTimer.Start();

                }

            }

        }

        private void ApplyRoleView()
        {
            if (IsBasicUser)
            {
                // BasicUser gets the same search-first panel as Encoder, but without tips and with a simpler subtitle
                AdminToolbar.Visibility = Visibility.Collapsed;
                BasicUserToolbar.Visibility = Visibility.Collapsed;
                EncoderSearchPanel.Visibility = Visibility.Visible;
                if (PageTitleGrid != null) PageTitleGrid.Visibility = Visibility.Collapsed;
                PagingRow.Visibility = Visibility.Collapsed;
                _barangayFilter = CurrentUserBarangay;
                // Start with the search-first state
                SetEncoderResultsVisible(false);
                if (ColSex != null) ColSex.Width = 0;
                if (ColCivilStatus != null) ColCivilStatus.Width = 0;
                if (ColChildren != null) ColChildren.Width = 0;
                if (ColLastUpdated != null) ColLastUpdated.Width = 0;
                if (ColBarangay != null) ColBarangay.Width = 0;

            }
            else if (IsEncoder)
            {
                // Encoder users: search banner + tips first. The results table only appears after a search.
                AdminToolbar.Visibility = Visibility.Collapsed;
                BasicUserToolbar.Visibility = Visibility.Collapsed;
                EncoderSearchPanel.Visibility = Visibility.Visible;
                if (PageTitleGrid != null) PageTitleGrid.Visibility = Visibility.Collapsed;
                PagingRow.Visibility = Visibility.Collapsed;
                SetEncoderResultsVisible(false);

            }
            else
            {
                // Administrator: Show everything
                AdminToolbar.Visibility = Visibility.Visible;
                BasicUserToolbar.Visibility = Visibility.Collapsed;
                EncoderSearchPanel.Visibility = Visibility.Collapsed;
                if (PageTitleGrid != null) PageTitleGrid.Visibility = Visibility.Visible;
                RecordsClipGrid.Visibility = Visibility.Visible;
                PagingRow.Visibility = Visibility.Visible;

            }
            // Hide delete buttons for non-admin users
            HideDeleteButtonsIfNotAdmin();

        }
        /// <summary>
        /// Encoder/BasicUser only.
        /// false = landing view: search card visible, results table hidden with empty-state placeholder.
        /// true  = results view: results table visible, empty-state placeholder hidden.
        /// The unified "How to use" banner is always visible and never toggled.
        /// </summary>

        private void SetEncoderResultsVisible(bool showResults)
        {
            if (!IsEncoder && !IsBasicUser) return;
            var show = Visibility.Visible;
            var hide = Visibility.Collapsed;
            // Results table (Row 3) � always visible for Encoder/BasicUser
            if (RecordsClipGrid != null)
                RecordsClipGrid.Visibility = show;
            // Table empty state overlay � shown inside table when no search done yet
            if (TableEmptyStateOverlay != null)
                TableEmptyStateOverlay.Visibility = showResults ? Visibility.Collapsed : Visibility.Visible;
            // If returning to landing view, hide skeleton loader
            if (!showResults && RecordsSkeletonLoader != null)
                RecordsSkeletonLoader.Visibility = Visibility.Collapsed;
            // Paging row
            if (PagingRow != null)
                PagingRow.Visibility = showResults ? show : hide;

        }

        private async Task LoadRecordsAsync()
        {
            if (_isLoading) return;
            // Encoders must search first � don't load records without a search query
            if (IsEncoder && string.IsNullOrWhiteSpace(EncSearchQuery?.Text?.Trim()))
            {
                SetEncoderResultsVisible(false);
                _allRecords = new List<SoloParentRecordViewModel>();
                _filteredRecords = new List<SoloParentRecordViewModel>();
                RecordsList.ItemsSource = null;
                // Show the initial empty state overlay (not the "no results" overlay)
                if (TableEmptyStateOverlay != null) TableEmptyStateOverlay.Visibility = Visibility.Visible;
                if (NoResultsPanel != null) NoResultsPanel.Visibility = Visibility.Collapsed;
                UpdatePagingUI();
                return;
            }
            // For all users: automatically hydrate initial records (scoped to user's barangay for BasicUser / Caseworker / Barangay Secretary)
            _isLoading = true;
            if (BtnRefreshRecords != null) BtnRefreshRecords.IsEnabled = false;
            if (RecordsSkeletonLoader != null) RecordsSkeletonLoader.Visibility = Visibility.Visible;
            // Encoder/BasicUser with a search: show the results card
            if (IsEncoder || IsBasicUser)
                SetEncoderResultsVisible(true);
            // Hide the LoadingOverlay while loading
            if (PageLoadingOverlay != null) PageLoadingOverlay.IsActive = false;
            try
            {

                string effectiveBarangay = IsBasicUser ? CurrentUserBarangay : _barangayFilter;
                // Handle search query based on user role

                string query = (IsEncoder || IsBasicUser)
                    ? (EncSearchQuery?.Text?.Trim() ?? string.Empty)
                    : (SearchName?.Text?.Trim() ?? string.Empty);
                Guid? searchGuid = null;

                string firstName = null;

                string lastName = null;

                string fullname = null;

                string philsysId = null;

                string barangay = null;
                if (!string.IsNullOrWhiteSpace(query))
                {

                    string selectedMode = "AUTO";
                    if (IsEncoder || IsBasicUser)
                    {
                        if (EncSearchMode?.SelectedItem is ComboBoxItem modeItemEnc)
                            selectedMode = modeItemEnc.Tag?.ToString() ?? "AUTO";

                    }
                    else
                    {
                        if (CmbSearchMode?.SelectedItem is ComboBoxItem modeItemAdmin)
                            selectedMode = modeItemAdmin.Tag?.ToString() ?? "AUTO";

                    }
                    if (selectedMode == "RECORD_ID" || (selectedMode == "AUTO" && Guid.TryParse(query, out var parsedGuidAuto)))
                    {
                        if (Guid.TryParse(query, out var parsedGuid))
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
                                fullname = query.Trim();
                                break;
                            case "PHILSYS_ID":
                                philsysId = query;
                                break;
                            case "AUTO":
                            default:

                                string digitsOnly = System.Text.RegularExpressions.Regex.Replace(query, @"\D", "");
                                if (digitsOnly.Length >= 4 && System.Text.RegularExpressions.Regex.IsMatch(query, @"^[\d\s\-]+$"))
                                {
                                    philsysId = query;

                                }
                                else if (query.Contains(","))
                                {
                                    var parts = query.Split(new[] { ',' }, 2);
                                    lastName = parts[0].Trim();
                                    firstName = parts.Length > 1 ? parts[1].Trim() : null;

                                }
                                else if (query.Contains(" "))
                                {
                                    fullname = query.Trim();

                                }
                                else
                                {
                                    lastName = query;

                                }
                                break;

                        }

                    }

                }
                bool? isActive = null;
                bool? isDeleted = null;
                bool? isDueForRenewal = null;
                DateTime? validUntilFrom = null;
                DateTime? validUntilTo = null;
                if (!IsEncoder && !IsBasicUser && _statusFilter != "All")
                {
                    if (string.Equals(_statusFilter, "Valid", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(_statusFilter, "Active (Valid)", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(_statusFilter, "Active", StringComparison.OrdinalIgnoreCase))
                    {
                        isActive = true;
                        isDeleted = false;
                    }
                    else if (string.Equals(_statusFilter, "ExpiringSoon", StringComparison.OrdinalIgnoreCase))
                    {
                        isActive = true;
                        isDeleted = false;
                        isDueForRenewal = true;
                    }
                    else if (string.Equals(_statusFilter, "Inactive", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(_statusFilter, "Inactive (Expired)", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(_statusFilter, "Expired", StringComparison.OrdinalIgnoreCase))
                    {
                        isActive = false;
                        isDeleted = false;
                    }
                    else if (string.Equals(_statusFilter, "Terminated (Deleted)", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(_statusFilter, "Terminated", StringComparison.OrdinalIgnoreCase))
                    {
                        isDeleted = true;
                    }
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
                var (sortBy, sortOrder) = SortControl != null
                    ? RecordSortingHelper.MapToApiParams(SortControl.SelectedField, SortControl.IsAscending)
                    : ("datecreated", "desc");
                var req = new GetSoloParentRequest
                {
                    Id = searchGuid,
                    FirstName = firstName,
                    LastName = lastName,
                    Fullname = fullname,
                    PhilsysId = philsysId,
                    Barangay = barangay,
                    Sex = sex,
                    IsActive = isActive,
                    IsDeleted = isDeleted,
                    IsDueForRenewal = isDueForRenewal,
                    ValidUntilFrom = validUntilFrom,
                    ValidUntilTo = validUntilTo,
                    IsLGBT = (!IsEncoder && !IsBasicUser) ? _isLGBTFilter : null,
                    IsPantawidBeneficiary = (!IsEncoder && !IsBasicUser) ? _isPantawidFilter : null,
                    IsIndigenous = (!IsEncoder && !IsBasicUser) ? _isIndigenousFilter : null,
                    SortBy = sortBy,
                    SortOrder = sortOrder,
                    Page = _currentPage,
                    PageSize = Math.Min(100, Math.Max(4, _pageSize))

                };
                // Fetch the list page, then retrieve full details in parallel (the list
                // endpoint does not return complete PersonalInfo � detail endpoint does).
                var resp = await SoloParentApiService.Instance.GetSoloParentsAsync(req);
                if (!resp.Succeeded || resp.Data == null)
                {

                    string err = resp.Errors != null && resp.Errors.Count > 0
                        ? resp.Errors[0]
                        : "Unable to load records from backend.";
                    ShowNotification("API Notice", err, false);
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
                        // Detail call failed � build a minimal record from summary fields
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
                    _allRecords.Add(vm);

                }
                if (SortControl != null)
                {
                    _allRecords = RecordSortingHelper.Sort(_allRecords, SortControl.SelectedField, SortControl.IsAscending);
                }
                if (!IsEncoder && !IsBasicUser && !string.Equals(_statusFilter, "All", StringComparison.OrdinalIgnoreCase))
                {
                    // Server handles isActive, isDeleted, and isDueForRenewal natively.
                    // Preserve the server-returned total count and total pages.
                    _totalCount = paged.TotalCount;
                    _totalPages = Math.Max(1, paged.TotalPages);
                }
                {

                }
                for (int i = 0; i < _allRecords.Count; i++)
                {
                    _allRecords[i].SetAlternate(i % 2 != 0);

                }
                _filteredRecords = new List<SoloParentRecordViewModel>(_allRecords);
                RecordsList.ItemsSource = null;
                RecordsList.ItemsSource = _filteredRecords;
                if (NoResultsPanel != null)
                    NoResultsPanel.Visibility = _filteredRecords.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                // After a search completes for Encoder/BasicUser, SearchFirstPanel should stay hidden
                // Hide the table empty state overlay when results are displayed
                if (TableEmptyStateOverlay != null) TableEmptyStateOverlay.Visibility = Visibility.Collapsed;
                // After a successful search for Encoder/BasicUser, ensure results table is visible
                if ((IsEncoder || IsBasicUser) && _filteredRecords.Count > 0)
                    SetEncoderResultsVisible(true);
                UpdatePagingUI();
                _lastLoadedTime = DateTime.UtcNow;
                _hasLoadedData = true;
                if (TxtLastRefreshed != null)
                    TxtLastRefreshed.Text = $"� Updated {DateTime.Now:h:mm tt}";
                if (BtnRefreshRecords != null)
                    BtnRefreshRecords.ToolTip = $"Refresh records (Last updated: {DateTime.Now:h:mm:ss tt})";
                // Ensure delete is hidden for non-admin roles after every data load
                HideDeleteButtonsIfNotAdmin();

            }
            catch (Exception ex)
            {
                ShowNotification("Error", ex.Message, false);

            }
            finally
            {
                _isLoading = false;
                if (BtnRefreshRecords != null)
                {
                    _ = Task.Run(async () =>
                    {
                        var elapsed = DateTime.UtcNow - _lastRefreshClickTime;
                        if (elapsed < RefreshCooldown)
                            await Task.Delay(RefreshCooldown - elapsed);
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (BtnRefreshRecords != null) BtnRefreshRecords.IsEnabled = true;

                        });

                    });

                }
                if (PageLoadingOverlay != null) PageLoadingOverlay.IsActive = false;
                if (RecordsSkeletonLoader != null) RecordsSkeletonLoader.Visibility = Visibility.Collapsed;

            }

        }

        private async void BtnRefreshRecords_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoading || (DateTime.UtcNow - _lastRefreshClickTime) < RefreshCooldown)
                return;
            _lastRefreshClickTime = DateTime.UtcNow;
            if (BtnRefreshRecords != null) BtnRefreshRecords.IsEnabled = false;
            _lastLoadedTime = DateTime.MinValue; // Force fresh fetch
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
            BuildPageNumbers();
            if (PageInfoText != null)
                PageInfoText.Text = TotalPages > 0 ? $"Page {_currentPage} of {TotalPages}" : "Page 1 of 1";
            if (TxtRecordsSummary != null)
                TxtRecordsSummary.Text = $"{_totalCount:N0} total records";
            if (TxtHeaderRecordCount != null)
                TxtHeaderRecordCount.Text = $"{_totalCount:N0} Records";
            if (TxtRecordsPageSummary != null)
            {

                int start = _totalCount == 0 ? 0 : ((_currentPage - 1) * _pageSize) + 1;

                int end = Math.Min(_totalCount, _currentPage * _pageSize);
                TxtRecordsPageSummary.Text = $"Showing {start} - {end} of {_totalCount:N0} records";

            }
            if (BtnFirst != null)
            {
                BtnFirst.Visibility = Visibility.Visible;
                BtnFirst.IsEnabled = _currentPage > 1;
            }
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
            if (BtnLast != null)
            {
                BtnLast.Visibility = Visibility.Visible;
                BtnLast.IsEnabled = _currentPage < TotalPages;
            }
        }

        private void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage = 1;
                _ = LoadRecordsAsync();
            }
        }

        private void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < TotalPages)
            {
                _currentPage = TotalPages;
                _ = LoadRecordsAsync();
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
                case "RECORD_ID":
                    SearchPlaceholder = "Search by record ID (GUID)...";
                    break;
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

        private void FilterCombo_Changed(object sender, SelectionChangedEventArgs e) { }

        private void FilterApply_Click(object sender, RoutedEventArgs e)
        {

            string sexVal = (CmbFilterSex?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";

            string barangayVal = (CmbFilterBarangay?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";

            // Keep existing _statusFilter from the active status tab
            _sexFilter = sexVal;
            _barangayFilter = barangayVal;
            _isPantawidFilter = null;
            _isLGBTFilter = null;
            _isIndigenousFilter = null;
            FilterPopup.IsOpen = false;
            UpdateFilterBadge();
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
            if (TabStatusAll != null) TabStatusAll.IsChecked = true;
            _isPantawidFilter = null;
            _isLGBTFilter = null;
            _isIndigenousFilter = null;
            FilterPopup.IsOpen = false;
            UpdateFilterBadge();
            ApplyFiltersAndPage();

        }

        private void StatusOpt_Click(object sender, MouseButtonEventArgs e) { }

        private void SexOpt_Click(object sender, MouseButtonEventArgs e) { }

        private void RecordSortControl_SortChanged(object sender, RecordSortEventArgs e)
        {
            // Triggers a backend API query with the updated sortBy and sortOrder parameters
            ApplyFiltersAndPage();

        }

        private void RecordsListColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader header && header.Column != null)
            {

                string colHeader = header.Column.Header?.ToString();
                RecordSortField? targetField = null;
                switch (colHeader)
                {
                    case "BARANGAY":
                        targetField = RecordSortField.Barangay;
                        break;
                    case "LAST UPDATED":
                        targetField = RecordSortField.LastUpdated;
                        break;
                    case "APPLICATION DATE":
                        targetField = RecordSortField.ApplicationDate;
                        break;

                }
                if (targetField.HasValue && SortControl != null)
                {

                    bool isAsc = (SortControl.SelectedField == targetField.Value)
                        ? !SortControl.IsAscending
                        : (targetField.Value != RecordSortField.LastUpdated && targetField.Value != RecordSortField.ApplicationDate);
                    SortControl.SetSort(targetField.Value, isAsc);

                }

            }

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
            SoloParentRecord recordToView = vm.RawModel;
            if (Guid.TryParse(vm.Id, out var id))
            {
                var detailResp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(id);
                if (detailResp.Succeeded && detailResp.Data != null)
                {
                    recordToView = SoloParentApiService.Instance.MapToRecord(detailResp.Data);

                }

            }
            var view = new RecordViewDialog(recordToView) { Owner = Application.Current.MainWindow };
            view.ShowDialog();
            if (view.OpenedRenew)
            {
                if (view.RenewalResult != null)
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
                await DeleteRecordAsync(recordToView.Id, recordToView.Name, skipConfirmation: true);

            }

        }

        private async void AddNewRecord_Click(object sender, RoutedEventArgs e)
        {
            var methodDialog = new EntryMethodDialog { Owner = Application.Current.MainWindow };
            bool? picked = methodDialog.ShowDialog();
            if (picked != true || methodDialog.Selected == EntryMethod.None)
            {
                return;

            }
            if (methodDialog.Selected == EntryMethod.Scan)
            {
                var ocr = new SOLUM_UI.Views.Dialogs.OcrScanDialog { Owner = Application.Current.MainWindow };
                if (ocr.ShowDialog() == true && ocr.Result != null)
                {
                    await SaveNewRecordAsync(ocr.Result);

                }
                return;

            }
            // Manual entry � open 3-step wizard (Step 1 performs validation & duplicate check)
            RecordDialog dialog = new RecordDialog { Owner = Application.Current.MainWindow };
            dialog.SaveAsyncHandler = async (rec) =>
            {
                rec.CreatedBy = CurrentUserName;
                rec.Barangay = string.IsNullOrWhiteSpace(rec.Barangay)
                    ? (!string.IsNullOrWhiteSpace(CurrentUserBarangay) ? CurrentUserBarangay : "ALL")
                    : rec.Barangay;
                var createReq = SoloParentApiService.Instance.MapToCreateRequest(rec);
                var createResp = await SoloParentApiService.Instance.CreateSoloParentAsync(createReq);
                if (!createResp.Succeeded)
                {
                    return BaseResponse<object>.FailWithStatus(createResp.StatusCode, createResp.Errors != null ? createResp.Errors.ToArray() : new string[0]);
                }
                rec.Id = createResp.Data.ToString();
                return BaseResponse<object>.Success(createResp.Data);
            };
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                ShowNotification("Record Added", dialog.Result.Name + " was added successfully.", true);
                AuditLogService.Instance.LogCreate(dialog.Result.Id, dialog.Result.Name, GetCurrentUser(), CurrentUserRole);
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("SubsidyRecommendation");
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Dashboard");
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Analytics");
                if (SortControl != null) { SortControl.SetSort(RecordSortField.LastUpdated, false); }
                _currentPage = 1;
                await LoadRecordsAsync();
            }

        }

        private async Task SaveNewRecordAsync(SoloParentRecord record)
        {
            // Show loading indicator
            PageLoadingOverlay.Message = "Saving record...";
            PageLoadingOverlay.IsActive = true;
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
                    string title = (createResp.StatusCode == 409 || (createResp.Errors != null && createResp.Errors.Any(e => e.IndexOf("PhilSys", StringComparison.OrdinalIgnoreCase) >= 0)))
                        ? "Duplicate PhilSys ID"
                        : "Creation Failed";
                    Views.Dialogs.ActionConfirmDialog.Error(title, err);
                    return;
                }
                record.Id = createResp.Data.ToString();
                ShowNotification("Record Added", record.Name + " was added successfully.", true);
                AuditLogService.Instance.LogCreate(record.Id, record.Name, GetCurrentUser(), CurrentUserRole);
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("SubsidyRecommendation");
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Dashboard");
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Analytics");
                if (SortControl != null) { SortControl.SetSort(RecordSortField.LastUpdated, false); }
                _currentPage = 1;
                await LoadRecordsAsync();

            }
            finally
            {
                // Hide loading indicator
                PageLoadingOverlay.IsActive = false;

            }

        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {

            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SoloParentRecordViewModel vm = _allRecords?.Find(r => r.Id == id);
            if (vm == null) return;
            if (IsBasicUser && !string.Equals(vm.RawModel.CreatedBy ?? string.Empty, CurrentUserName, StringComparison.OrdinalIgnoreCase))
            {
                ShowNotification("Access Denied", "You can only edit records you created.", false);
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
            if (!record.CanRenew(out string quickRenewReason, out _))
            {
                Views.Dialogs.ActionConfirmDialog.Warning(
                    "Renewal Not Available",
                    quickRenewReason ?? "This solo parent record has been active for less than a month and cannot be renewed yet.",
                    Window.GetWindow(this));
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
            PageLoadingOverlay.Message = "Processing renewal...";
            PageLoadingOverlay.IsActive = true;
            try
            {
                var resp = await SoloParentApiService.Instance.RenewSoloParentRecordAsync(guid, null);
                if (resp.Succeeded)
                {

                    string newExpiry = (resp.Data != null && DateTime.TryParse(resp.Data.ExpiresAt, out var expDt))
                        ? expDt.ToString("MMMM d, yyyy")
                        : DateTime.Today.AddYears(1).ToString("MMMM d, yyyy");
                    ShowNotification("Record Renewed", $"{record.Name} � valid until {newExpiry}.", true);
                    AuditLogService.Instance.LogUpdate(record.Id, record.Name, GetCurrentUser(), CurrentUserRole, $"Record quick renewed. Valid until {newExpiry}.");
                    (Application.Current.MainWindow as MainWindow)?.ClearPageCache("SubsidyRecommendation");
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Dashboard");
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Analytics");
                if (SortControl != null) { SortControl.SetSort(RecordSortField.LastUpdated, false); }
                _currentPage = 1;
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
                PageLoadingOverlay.IsActive = false;

            }

        }

        private async Task RenewRecordAsync(SoloParentRecord original, SoloParentRecord updated, SoloParentDto existingDto = null)
        {
            System.Diagnostics.Debug.WriteLine("[RenewRecordAsync] Starting renewal process...");
            System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] Original ID: {original?.Id}");
            System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] Original Name: {original?.Name}");
            
            if (!Guid.TryParse(original.Id, out var guid))
            {
                System.Diagnostics.Debug.WriteLine("[RenewRecordAsync] ERROR: Invalid record ID format");
                Views.Dialogs.ActionConfirmDialog.Warning(
                    "Renew",
                    "Cannot renew: invalid record ID.");
                return;

            }
            
            System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] Parsed GUID: {guid}");
            
            if (!original.CanRenew(out string renewReason, out _))
            {
                System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] ERROR: Cannot renew - {renewReason}");
                Views.Dialogs.ActionConfirmDialog.Warning(
                    "Renewal Not Available",
                    renewReason ?? "This solo parent record has been active for less than a month and cannot be renewed yet.",
                    Window.GetWindow(this));
                return;

            }
            
            System.Diagnostics.Debug.WriteLine("[RenewRecordAsync] Eligibility check passed");
            PageLoadingOverlay.Message = "Renewing record...";
            PageLoadingOverlay.IsActive = true;
            try
            {
                if (existingDto == null)
                {
                    System.Diagnostics.Debug.WriteLine("[RenewRecordAsync] Fetching existing DTO from API...");
                    var fetch = await SoloParentApiService.Instance.GetSoloParentByIdAsync(guid);
                    if (fetch.Succeeded && fetch.Data != null)
                    {
                        existingDto = fetch.Data;
                        System.Diagnostics.Debug.WriteLine("[RenewRecordAsync] Existing DTO fetched successfully");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] WARNING: Failed to fetch existing DTO - Succeeded: {fetch.Succeeded}");
                    }

                }
                
                System.Diagnostics.Debug.WriteLine("[RenewRecordAsync] Mapping renewal request...");
                var renewReq = SoloParentApiService.Instance.MapToRenewRequest(guid, updated, existingDto);
                System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] Request mapped - RenewalDate: {renewReq?.RenewalDate}");
                System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] Request mapped - CivilStatus: {renewReq?.CivilStatus}");
                System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] Request mapped - Family Members: {renewReq?.FamilyMembers?.ExistingFamilyMembers?.Count ?? 0} existing, {renewReq?.FamilyMembers?.NewFamilyMembers?.Count ?? 0} new");
                
                System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] Calling API: POST api/soloparent/{guid}/renew");
                var renewResp = await SoloParentApiService.Instance.RenewSoloParentRecordAsync(guid, renewReq);
                
                System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] API Response - Succeeded: {renewResp?.Succeeded}");
                if (renewResp?.Errors != null && renewResp.Errors.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] API Errors: {string.Join(" | ", renewResp.Errors)}");
                }
                if (renewResp?.Data != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] Response Data - ID: {renewResp.Data.Id}");
                    System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] Response Data - StartsAt: {renewResp.Data.StartsAt}");
                    System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] Response Data - ExpiresAt: {renewResp.Data.ExpiresAt}");
                }
                
                if (!renewResp.Succeeded)
                {
                    string err = renewResp.Errors != null && renewResp.Errors.Count > 0
                        ? string.Join("\n", renewResp.Errors) : "Failed to renew record.";
                    System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] RENEWAL FAILED: {err}");
                    Views.Dialogs.ActionConfirmDialog.Error(
                        "Renew Failed",
                        err);
                    return;

                }

                string newExpiry = (renewResp.Data != null && DateTime.TryParse(renewResp.Data.ExpiresAt, out var expDt))
                    ? expDt.ToString("MMMM d, yyyy")
                    : DateTime.Today.AddYears(1).ToString("MMMM d, yyyy");
                System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] SUCCESS! New expiry: {newExpiry}");
                
                ShowNotification("Record Renewed",
                    $"{original.Name} � valid until {newExpiry}.", true);
                var changeService = new ChangeTrackingService();
                var changes = changeService.DetectChanges(original, updated);
                if (changes.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] Detected {changes.Count} changes for audit log");
                    AuditLogService.Instance.LogUpdateWithChanges(original.Id, original.Name, GetCurrentUser(), CurrentUserRole, changes);

                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[RenewRecordAsync] No changes detected, logging simple renewal");
                    AuditLogService.Instance.LogUpdate(original.Id, original.Name,
                        GetCurrentUser(), CurrentUserRole, $"Record renewed. Valid until {newExpiry}.");

                }
                
                System.Diagnostics.Debug.WriteLine("[RenewRecordAsync] Clearing page caches...");
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("SubsidyRecommendation");
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Dashboard");
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Analytics");
                if (SortControl != null) { SortControl.SetSort(RecordSortField.LastUpdated, false); }
                _currentPage = 1;
                
                System.Diagnostics.Debug.WriteLine("[RenewRecordAsync] Reloading records...");
                await LoadRecordsAsync();
                System.Diagnostics.Debug.WriteLine("[RenewRecordAsync] Renewal process completed successfully!");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[RenewRecordAsync] Stack trace: {ex.StackTrace}");
                
                Views.Dialogs.ActionConfirmDialog.Error(
                    "Renewal Error",
                    $"An error occurred during renewal: {ex.Message}");
            }
            finally
            {
                PageLoadingOverlay.IsActive = false;
                System.Diagnostics.Debug.WriteLine("[RenewRecordAsync] Loading overlay hidden");

            }

        }

        private async Task EditRecordAsync(SoloParentRecord rawRecord)
        {
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
            RecordDialog dialog = new RecordDialog(rawRecord) { Owner = Application.Current.MainWindow };
            dialog.SaveAsyncHandler = async (rec) =>
            {
                if (Guid.TryParse(rawRecord.Id, out var guid))
                {
                    var updateReq = SoloParentApiService.Instance.MapToUpdateRequest(guid, rec, existingDto);
                    var updateResp = await SoloParentApiService.Instance.UpdateSoloParentAsync(guid, updateReq);
                    if (!updateResp.Succeeded)
                    {
                        return BaseResponse<object>.FailWithStatus(updateResp.StatusCode, updateResp.Errors != null ? updateResp.Errors.ToArray() : new string[0]);
                    }
                }
                return BaseResponse<object>.Success(true);
            };
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                ShowNotification("Record Updated", dialog.Result.Name + " was updated.", true);
                var changeList = BuildChangeList(rawRecord, dialog.Result);
                AuditLogService.Instance.LogUpdateWithChanges(dialog.Result.Id, dialog.Result.Name, GetCurrentUser(), CurrentUserRole, changeList);
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("SubsidyRecommendation");
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Dashboard");
                (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Analytics");
                await LoadRecordsAsync();
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
            SoloParentRecord rawRecord = vm?.RawModel;
            SoloParentDto existingDto = null;
            PageLoadingOverlay.Message = "Opening record...";
            PageLoadingOverlay.IsActive = true;
            try
            {
                var fetch = await SoloParentApiService.Instance.GetSoloParentByIdAsync(guid);
                if (fetch.Succeeded && fetch.Data != null)
                {
                    existingDto = fetch.Data;
                    rawRecord = SoloParentApiService.Instance.MapToRecord(existingDto);

                }

            }
            finally
            {
                PageLoadingOverlay.IsActive = false;

            }
            if (rawRecord == null) rawRecord = new SoloParentRecord { Id = id, Surname = recordName };
            if (!rawRecord.CanRenew(out string renewalEligibilityReason, out _))
            {
                Views.Dialogs.ActionConfirmDialog.Warning(
                    "Renewal Not Available",
                    renewalEligibilityReason ?? "This solo parent record has been active for less than a month and cannot be renewed yet.",
                    Window.GetWindow(this));
                return;

            }
            var dialog = new RenewalReassessmentDialog(rawRecord, existingDto)
            {
                Owner = Application.Current.MainWindow

            };
            if (dialog.ShowDialog() == true && dialog.UpdatedRecord != null)
            {
                await RenewRecordAsync(rawRecord, dialog.UpdatedRecord, existingDto);

            }

        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin)
            {
                ShowNotification("Access Denied", "Encoders cannot delete records. Only administrators have permission to delete records.", false);
                return;

            }

            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SoloParentRecordViewModel vm = _allRecords?.Find(r => r.Id == id);

            string recordName = vm != null ? vm.Name : id;
            await DeleteRecordAsync(id, recordName);

        }

        private async Task DeleteRecordAsync(string id, string recordName, bool skipConfirmation = false)
        {
            if (!IsAdmin)
            {
                ShowNotification("Access Denied", "Encoders cannot delete records. Only administrators have permission to delete records.", false);
                return;

            }
            if (!Guid.TryParse(id, out var guid))
            {
                ShowNotification("Invalid ID", "Cannot delete record without a valid ID.", false);
                return;

            }

            // Only show confirmation dialog if not already confirmed (e.g., from RecordViewDialog)
            if (!skipConfirmation)
            {
                bool isConfirmed = ActionConfirmDialog.Show(
                    title: "Delete Record",
                    message: $"Are you sure you want to permanently delete the record for {recordName}? This action cannot be undone.",
                    confirmText: "Delete Record",
                    cancelText: "Cancel",
                    theme: ConfirmThemeType.Danger,
                    owner: Window.GetWindow(this));
                if (!isConfirmed) return;
            }
            
            // Show loading indicator
            PageLoadingOverlay.Message = "Deleting record...";
            PageLoadingOverlay.IsActive = true;
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
                ShowNotification("Record Deleted", $"{recordName} was removed successfully.", true);
                AuditLogService.Instance.LogDelete(id, recordName, GetCurrentUser(), CurrentUserRole);
                await LoadRecordsAsync();

            }
            finally
            {
                // Hide loading indicator
                PageLoadingOverlay.IsActive = false;

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
                ShowNotification(
                    "Export Complete",
                    "Record exported to:\n" + System.IO.Path.GetFileName(dlg.FileName),
                    true);

            }
            catch (Exception ex)
            {
                ShowNotification(
                    "Export Error",
                    "Export failed:\n" + ex.Message,
                    false);

            }

        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeNameColumn();
            UpdateDynamicPageSize();

        }

        private void ResizeNameColumn()
        {
            if (RecordsList?.View is GridView gv && gv.Columns.Count >= 10)
            {
                // Fixed columns for streamlined layout (px)
                const double appDate    = 135;
                const double validUntil = 135;
                const double status     = 120;
                const double actions    = 110;

                double available = RecordsList.ActualWidth - 2;
                if (available <= 0) return;
                // Hidden columns kept at 0px for code-behind compatibility
                if (ColSex != null) ColSex.Width = 0;
                if (ColCivilStatus != null) ColCivilStatus.Width = 0;
                if (ColDob != null) ColDob.Width = 0;
                if (ColLastUpdated != null) ColLastUpdated.Width = 0;

                double totalFixed = appDate + validUntil + status + actions;
                double flex = Math.Max(280, available - totalFixed);
                if (ColBarangay != null && ColBarangay.Width == 0)
                {
                    if (ColName != null) ColName.Width = flex;
                }
                else
                {
                    if (ColName != null) ColName.Width = Math.Max(180, flex * 0.60);
                    if (ColBarangay != null) ColBarangay.Width = Math.Max(120, flex * 0.40);
                }
                if (ColApplicationDate != null) ColApplicationDate.Width = appDate;
                if (ColChildren != null) ColChildren.Width = validUntil;
                if (ColStatus != null) ColStatus.Width = status;
                if (ColActions != null) ColActions.Width = actions;
            }
        }

        private void BasicSearchName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndPage();

        }

        private async void BasicUserAddRecord_Click(object sender, RoutedEventArgs e)
        {
            var methodDialog = new EntryMethodDialog { Owner = Application.Current.MainWindow };
            bool? picked = methodDialog.ShowDialog();
            if (picked != true || methodDialog.Selected == EntryMethod.None)
            {
                return;

            }
            if (methodDialog.Selected == EntryMethod.Scan)
            {
                var ocr = new SOLUM_UI.Views.Dialogs.OcrScanDialog { Owner = Application.Current.MainWindow };
                if (ocr.ShowDialog() == true && ocr.Result != null)
                {
                    await SaveNewRecordAsync(ocr.Result);

                }
                return;

            }
            // Manual entry � open RecordDialog Step 1 (Primary Info) directly
            RecordDialog dialog = new RecordDialog() { Owner = Application.Current.MainWindow };
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
                if (IsEncoder || IsBasicUser)
                {
                    // Both Encoder and BasicUser now use the shared EncSearchQuery panel
                    return EncSearchQuery != null && !string.IsNullOrEmpty(EncSearchQuery.Text?.Trim());

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
        /// Hides the delete button in each row for non-admin users.
        /// This is called after items are loaded and after ApplyRoleView.
        /// </summary>

        private void HideDeleteButtonsIfNotAdmin()
        {
            if (IsAdmin) return;
            // Walk the visual tree after the ListView renders
            Dispatcher.BeginInvoke(new Action(() =>
            {
                HideDeleteInContainer(RecordsList);

            }), System.Windows.Threading.DispatcherPriority.Loaded);

        }

        private void HideDeleteInContainer(System.Windows.DependencyObject parent)
        {
            if (parent == null) return;

            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is Button btn && btn.Name == "BtnDelete")
                {
                    btn.Visibility = Visibility.Collapsed;

                }
                else
                {
                    HideDeleteInContainer(child);

                }

            }

        }
        /// <summary>
        /// Compares two records field-by-field and returns a structured list of
        /// <see cref="SOLUM_UI.Models.ChangeDetail"/> entries � one per changed field.
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
                    before.DateOfBirth != DateTime.MinValue ? before.DateOfBirth.ToString("yyyy-MM-dd") : "�",
                    after.DateOfBirth != DateTime.MinValue ? after.DateOfBirth.ToString("yyyy-MM-dd") : "�"));
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
        // ==================== Status Tabs & Filter Badges ====================

        private void QuickStatusTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                string tag = rb.Tag.ToString();
                _statusFilter = tag;
                _currentPage = 1;
                _ = LoadRecordsAsync();
            }
        }

        private void RemoveBarangayFilter_Click(object sender, RoutedEventArgs e)
        {
            _barangayFilter = "All";
            if (CmbFilterBarangay != null) CmbFilterBarangay.SelectedIndex = 0;
            UpdateFilterBadge();
            ApplyFiltersAndPage();
        }

        private void RemoveSexFilter_Click(object sender, RoutedEventArgs e)
        {
            _sexFilter = "All";
            if (CmbFilterSex != null) CmbFilterSex.SelectedIndex = 0;
            UpdateFilterBadge();
            ApplyFiltersAndPage();
        }

        private void ClearAllFilters_Click(object sender, RoutedEventArgs e)
        {
            FilterReset_Click(sender, e);
        }

        private void UpdateFilterBadge()
        {
            int activeCount = 0;
            bool hasSex = !string.IsNullOrEmpty(_sexFilter) && _sexFilter != "All";
            bool hasBarangay = !string.IsNullOrEmpty(_barangayFilter) && _barangayFilter != "All";
            if (hasSex) activeCount++;
            if (hasBarangay) activeCount++;
            if (FilterDropLabel != null)
            {
                FilterDropLabel.Text = activeCount > 0 ? $"Filter ({activeCount})" : "Filter";
            }
            if (ActiveFiltersPanel != null)
            {
                ActiveFiltersPanel.Visibility = activeCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            if (BadgeFilterSex != null)
            {
                BadgeFilterSex.Visibility = hasSex ? Visibility.Visible : Visibility.Collapsed;
                if (TxtBadgeSex != null) TxtBadgeSex.Text = $"Sex: {_sexFilter}";
            }
            if (BadgeFilterBarangay != null)
            {
                BadgeFilterBarangay.Visibility = hasBarangay ? Visibility.Visible : Visibility.Collapsed;
                if (TxtBadgeBarangay != null) TxtBadgeBarangay.Text = $"Barangay: {_barangayFilter}";
            }
        }

        private void RenewRecord_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SoloParentRecordViewModel vm = _allRecords?.Find(r => r.Id == id);
            if (vm == null) return;
            AuditLogService.Instance.LogView(vm.RawModel.Id, vm.Name, GetCurrentUser(), CurrentUserRole);
            OpenViewDialog(vm);
        }
    }
}
