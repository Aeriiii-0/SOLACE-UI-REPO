using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Services.Interfaces;

namespace SOLUM_UI.ViewModels
{
    /// <summary>
    /// Page-level ViewModel for SoloParentRecordsPage.
    /// Owns all state, commands, and API calls.
    /// Raises events when dialog-level operations are needed (visual-tree access).
    /// </summary>
    public class SoloParentRecordsViewModel : INotifyPropertyChanged
    {
        // ── Dependencies ────────────────────────────────────────────────────
        private readonly ISoloParentApiService _api;
        private readonly AuditLogService _auditLog;

        // ── User context (set by MainWindow before navigation) ───────────────
        public static string CurrentUserName     { get; set; } = string.Empty;
        public static string CurrentUserRole     { get; set; } = string.Empty;
        public static string CurrentUserBarangay { get; set; } = string.Empty;

        private bool IsAdmin     => string.Equals(CurrentUserRole, "Administrator", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(CurrentUserRole, "Admin",          StringComparison.OrdinalIgnoreCase);
        private bool IsBasicUser => string.Equals(CurrentUserRole, "BasicUser",    StringComparison.OrdinalIgnoreCase);
        private bool IsEncoder   => string.Equals(CurrentUserRole, "Encoder",      StringComparison.OrdinalIgnoreCase);

        // ── INotifyPropertyChanged ───────────────────────────────────────────
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ── Events raised to code-behind (need visual tree) ─────────────────
        public event EventHandler<SoloParentRecordViewModel> RequestViewRecord;
        public event EventHandler                             RequestAddRecord;
        public event EventHandler<SoloParentRecord>          RequestEditRecord;
        public event EventHandler<RenewRecordArgs>           RequestRenewRecord;
        public event EventHandler<SoloParentRecordViewModel> RequestExportRecord;

        // ── Observable records collection ────────────────────────────────────
        public ObservableCollection<SoloParentRecordViewModel> Records { get; }
            = new ObservableCollection<SoloParentRecordViewModel>();

        private List<SoloParentRecordViewModel> _allRecords = new List<SoloParentRecordViewModel>();

        // ── Pagination ───────────────────────────────────────────────────────
        private const int PageSize = 10;
        private int _currentPage = 1;
        private int _totalPages  = 1;

        public int TotalPages
        {
            get => _totalPages;
            private set { if (_totalPages == value) return; _totalPages = value; OnPropertyChanged(nameof(TotalPages)); UpdatePagingProperties(); }
        }

        private string _pageInfoText = "Page 1 of 1";
        public string PageInfoText
        {
            get => _pageInfoText;
            private set { if (_pageInfoText == value) return; _pageInfoText = value; OnPropertyChanged(nameof(PageInfoText)); }
        }

        private Visibility _isBackVisible = Visibility.Collapsed;
        public Visibility IsBackVisible
        {
            get => _isBackVisible;
            private set { if (_isBackVisible == value) return; _isBackVisible = value; OnPropertyChanged(nameof(IsBackVisible)); }
        }

        private Visibility _isNextVisible = Visibility.Collapsed;
        public Visibility IsNextVisible
        {
            get => _isNextVisible;
            private set { if (_isNextVisible == value) return; _isNextVisible = value; OnPropertyChanged(nameof(IsNextVisible)); }
        }

        // ── Loading ──────────────────────────────────────────────────────────
        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            private set { if (_isLoading == value) return; _isLoading = value; OnPropertyChanged(nameof(IsLoading)); OnPropertyChanged(nameof(IsRefreshEnabled)); }
        }

        public bool IsRefreshEnabled => !_isLoading;

        // ── No results ───────────────────────────────────────────────────────
        private Visibility _hasNoResults = Visibility.Collapsed;
        public Visibility HasNoResults
        {
            get => _hasNoResults;
            private set { if (_hasNoResults == value) return; _hasNoResults = value; OnPropertyChanged(nameof(HasNoResults)); }
        }

        // ── Filter / Sort state ──────────────────────────────────────────────
        private string _statusFilter   = "All";
        private string _sexFilter      = "All";
        private string _barangayFilter = "All";
        private string _sortOption     = "Name A-Z";

        public string StatusFilter
        {
            get => _statusFilter;
            set { if (_statusFilter == value) return; _statusFilter = value; OnPropertyChanged(nameof(StatusFilter)); }
        }
        public string SexFilter
        {
            get => _sexFilter;
            set { if (_sexFilter == value) return; _sexFilter = value; OnPropertyChanged(nameof(SexFilter)); }
        }
        public string BarangayFilter
        {
            get => _barangayFilter;
            set { if (_barangayFilter == value) return; _barangayFilter = value; OnPropertyChanged(nameof(BarangayFilter)); }
        }
        public string SortOption
        {
            get => _sortOption;
            set { if (_sortOption == value) return; _sortOption = value; OnPropertyChanged(nameof(SortOption)); }
        }

        // ── Admin search fields (two-way bound) ──────────────────────────────
        private string _searchIdText = string.Empty;
        public string SearchIdText
        {
            get => _searchIdText;
            set
            {
                if (_searchIdText == value) return;
                _searchIdText = value;
                OnPropertyChanged(nameof(SearchIdText));
                UpdateClearSearchVisibility();
                if (string.IsNullOrEmpty(value)) ApplyFiltersAndPage();
            }
        }

        private string _searchNameText = string.Empty;
        public string SearchNameText
        {
            get => _searchNameText;
            set
            {
                if (_searchNameText == value) return;
                _searchNameText = value;
                OnPropertyChanged(nameof(SearchNameText));
                UpdateClearSearchVisibility();
                if (string.IsNullOrEmpty(value)) ApplyFiltersAndPage();
            }
        }

        private string _searchBarangayText = string.Empty;
        public string SearchBarangayText
        {
            get => _searchBarangayText;
            set
            {
                if (_searchBarangayText == value) return;
                _searchBarangayText = value;
                OnPropertyChanged(nameof(SearchBarangayText));
                UpdateClearSearchVisibility();
                if (string.IsNullOrEmpty(value)) ApplyFiltersAndPage();
            }
        }

        // ── Basic user search field ───────────────────────────────────────────
        private string _basicSearchNameText = string.Empty;
        public string BasicSearchNameText
        {
            get => _basicSearchNameText;
            set
            {
                if (_basicSearchNameText == value) return;
                _basicSearchNameText = value;
                OnPropertyChanged(nameof(BasicSearchNameText));
                ApplyFiltersAndPage();
            }
        }

        // ── Encoder panel search fields ───────────────────────────────────────
        private string _encSearchId        = string.Empty;
        private string _encSearchFirstName = string.Empty;
        private string _encSearchLastName  = string.Empty;
        private string _encSearchBarangay  = string.Empty;
        private string _encSearchBirthday  = string.Empty;

        public string EncSearchId        { get => _encSearchId;        set { if (_encSearchId == value) return;        _encSearchId = value;        OnPropertyChanged(nameof(EncSearchId)); } }
        public string EncSearchFirstName { get => _encSearchFirstName; set { if (_encSearchFirstName == value) return; _encSearchFirstName = value; OnPropertyChanged(nameof(EncSearchFirstName)); } }
        public string EncSearchLastName  { get => _encSearchLastName;  set { if (_encSearchLastName == value) return;  _encSearchLastName = value;  OnPropertyChanged(nameof(EncSearchLastName)); } }
        public string EncSearchBarangay  { get => _encSearchBarangay;  set { if (_encSearchBarangay == value) return;  _encSearchBarangay = value;  OnPropertyChanged(nameof(EncSearchBarangay)); } }
        public string EncSearchBirthday  { get => _encSearchBirthday;  set { if (_encSearchBirthday == value) return;  _encSearchBirthday = value;  OnPropertyChanged(nameof(EncSearchBirthday)); } }

        private Visibility _isClearSearchVisible = Visibility.Collapsed;
        public Visibility IsClearSearchVisible
        {
            get => _isClearSearchVisible;
            private set { if (_isClearSearchVisible == value) return; _isClearSearchVisible = value; OnPropertyChanged(nameof(IsClearSearchVisible)); }
        }

        // ── Role-based toolbar visibility ────────────────────────────────────
        private Visibility _adminToolbarVisible;
        public Visibility AdminToolbarVisible
        {
            get => _adminToolbarVisible;
            private set { if (_adminToolbarVisible == value) return; _adminToolbarVisible = value; OnPropertyChanged(nameof(AdminToolbarVisible)); }
        }

        private Visibility _basicToolbarVisible;
        public Visibility BasicToolbarVisible
        {
            get => _basicToolbarVisible;
            private set { if (_basicToolbarVisible == value) return; _basicToolbarVisible = value; OnPropertyChanged(nameof(BasicToolbarVisible)); }
        }

        private Visibility _encoderPanelVisible;
        public Visibility EncoderPanelVisible
        {
            get => _encoderPanelVisible;
            private set { if (_encoderPanelVisible == value) return; _encoderPanelVisible = value; OnPropertyChanged(nameof(EncoderPanelVisible)); }
        }

        // ── Commands ─────────────────────────────────────────────────────────
        public ICommand LoadRecordsCommand   { get; }
        public ICommand RefreshCommand       { get; }
        public ICommand NextPageCommand      { get; }
        public ICommand PrevPageCommand      { get; }
        public ICommand SearchCommand        { get; }
        public ICommand ClearSearchCommand   { get; }
        public ICommand ApplyFilterCommand   { get; }
        public ICommand ResetFilterCommand   { get; }
        public ICommand SortCommand          { get; }
        public ICommand ViewRecordCommand    { get; }
        public ICommand EditRecordCommand    { get; }
        public ICommand RenewRecordCommand   { get; }
        public ICommand DeleteRecordCommand  { get; }
        public ICommand ExportRecordCommand  { get; }
        public ICommand AddRecordCommand     { get; }

        // ── Constructor ──────────────────────────────────────────────────────
        public SoloParentRecordsViewModel()
            : this(SoloParentApiService.Instance, AuditLogService.Instance) { }

        public SoloParentRecordsViewModel(ISoloParentApiService api, AuditLogService auditLog)
        {
            _api      = api      ?? throw new ArgumentNullException(nameof(api));
            _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));

            LoadRecordsCommand  = new RelayCommand(_ => { var _ = LoadRecordsAsync(); });
            RefreshCommand      = new RelayCommand(_ => { var _ = LoadRecordsAsync(); });
            NextPageCommand     = new RelayCommand(_ => NextPage(),  _ => _currentPage < _totalPages);
            PrevPageCommand     = new RelayCommand(_ => PrevPage(),  _ => _currentPage > 1);
            SearchCommand       = new RelayCommand(_ => ApplyFiltersAndPage());
            ClearSearchCommand  = new RelayCommand(_ => ClearSearch());
            ApplyFilterCommand  = new RelayCommand(p  => ApplyFilter(p));
            ResetFilterCommand  = new RelayCommand(_ => ResetFilter());
            SortCommand         = new RelayCommand(p  => ApplySort(p != null ? p.ToString() : string.Empty));
            ViewRecordCommand   = new RelayCommand(p  => OnViewRecord(p as SoloParentRecordViewModel));
            EditRecordCommand   = new RelayCommand(p  => OnEditRecord(p != null ? p.ToString() : string.Empty));
            RenewRecordCommand  = new RelayCommand(p  => OnRenewRecord(p != null ? p.ToString() : string.Empty));
            DeleteRecordCommand = new RelayCommand(p  => { var _ = DeleteRecordAsync(p != null ? p.ToString() : string.Empty); });
            ExportRecordCommand = new RelayCommand(p  => OnExportRecord(p != null ? p.ToString() : string.Empty));
            AddRecordCommand    = new RelayCommand(_ => RequestAddRecord?.Invoke(this, EventArgs.Empty));

            ApplyRoleView();
        }

        // ── Public API for MainWindow ────────────────────────────────────────
        public SoloParentRecordViewModel FindRecord(string spId) =>
            _allRecords.Find(r => r.Id == spId);

        // ── Role-based visibility ────────────────────────────────────────────
        public void ApplyRoleView()
        {
            if (IsBasicUser)
            {
                AdminToolbarVisible  = Visibility.Collapsed;
                BasicToolbarVisible  = Visibility.Visible;
                EncoderPanelVisible  = Visibility.Collapsed;
                _barangayFilter      = CurrentUserBarangay;
            }
            else if (IsEncoder)
            {
                AdminToolbarVisible  = Visibility.Collapsed;
                BasicToolbarVisible  = Visibility.Collapsed;
                EncoderPanelVisible  = Visibility.Visible;
            }
            else
            {
                AdminToolbarVisible  = Visibility.Visible;
                BasicToolbarVisible  = Visibility.Collapsed;
                EncoderPanelVisible  = Visibility.Collapsed;
            }
        }

        // ── Data loading ──────────────────────────────────────────────────────
        public async Task LoadRecordsAsync()
        {
            if (_isLoading) return;
            IsLoading = true;

            try
            {
                string effectiveBarangay = IsBasicUser ? CurrentUserBarangay : _barangayFilter;
                string idQuery           = (_searchIdText ?? string.Empty).Trim();
                string nameQuery         = (_searchNameText ?? string.Empty).Trim();
                string barangayQuery     = (_searchBarangayText ?? string.Empty).Trim();

                if (IsBasicUser)
                {
                    nameQuery     = (_basicSearchNameText ?? string.Empty).Trim();
                    barangayQuery = string.Empty;
                }

                Guid? searchGuid = null;
                if (Guid.TryParse(idQuery, out var parsedGuid))
                    searchGuid = parsedGuid;

                bool? isActive = null;
                if (!IsBasicUser && _statusFilter != "All")
                    isActive = string.Equals(_statusFilter, "Valid", StringComparison.OrdinalIgnoreCase);

                string sex = (!IsBasicUser && _sexFilter != "All") ? _sexFilter : null;

                string barangay = null;
                if (!string.IsNullOrWhiteSpace(barangayQuery))
                    barangay = barangayQuery;
                else if (!string.IsNullOrEmpty(effectiveBarangay) && effectiveBarangay != "All")
                    barangay = effectiveBarangay;

                string sortBy    = "LastName";
                string sortOrder = "asc";
                switch (_sortOption)
                {
                    case "Name A-Z": sortBy = "LastName";    sortOrder = "asc";  break;
                    case "Name Z-A": sortBy = "LastName";    sortOrder = "desc"; break;
                    case "Newest":   sortBy = "datecreated"; sortOrder = "desc"; break;
                    case "Oldest":   sortBy = "datecreated"; sortOrder = "asc";  break;
                    case "Barangay": sortBy = "barangay";    sortOrder = "asc";  break;
                }

                var req = new GetSoloParentRequest
                {
                    Id        = searchGuid,
                    Fullname  = !string.IsNullOrWhiteSpace(nameQuery) ? nameQuery : null,
                    Barangay  = barangay,
                    Sex       = sex,
                    IsActive  = isActive,
                    SortBy    = sortBy,
                    SortOrder = sortOrder,
                    Page      = _currentPage,
                    PageSize  = PageSize
                };

                var resp = await _api.GetSoloParentsAsync(req);

                if (!resp.Succeeded || resp.Data == null)
                {
                    string err = resp.Errors != null && resp.Errors.Count > 0
                        ? resp.Errors[0] : "Unable to load records from backend.";
                    ToastNotification.Show("API Notice", err, ToastType.Warning);
                    _allRecords = new List<SoloParentRecordViewModel>();
                    App.Current.Dispatcher.Invoke(() => { Records.Clear(); HasNoResults = Visibility.Visible; });
                    TotalPages = 1;
                    UpdatePagingProperties();
                    return;
                }

                var paged = resp.Data;
                TotalPages = Math.Max(1, paged.TotalPages);

                var detailTasks     = paged.Items.Select(item => _api.GetSoloParentByIdAsync(item.Id)).ToList();
                var detailResponses = await Task.WhenAll(detailTasks);

                var newRecords = new List<SoloParentRecordViewModel>();
                for (int i = 0; i < paged.Items.Count; i++)
                {
                    var summary    = paged.Items[i];
                    var detailResp = detailResponses[i];

                    SoloParentRecord rec;
                    if (detailResp.Succeeded && detailResp.Data != null)
                        rec = _api.MapToRecord(detailResp.Data);
                    else
                    {
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
                    newRecords.Add(vm);
                }

                _allRecords = newRecords;
                App.Current.Dispatcher.Invoke(() =>
                {
                    Records.Clear();
                    foreach (var vm in _allRecords) Records.Add(vm);
                    HasNoResults = _allRecords.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                });

                UpdatePagingProperties();
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Error", ex.Message, ToastType.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ── CRUD operations ──────────────────────────────────────────────────
        public async Task SaveNewRecordAsync(SoloParentRecord record)
        {
            record.CreatedBy = CurrentUserName;
            record.Barangay  = string.IsNullOrEmpty(record.Barangay) ? CurrentUserBarangay : record.Barangay;

            var createReq  = _api.MapToCreateRequest(record);
            var createResp = await _api.CreateSoloParentAsync(createReq);

            if (!createResp.Succeeded)
            {
                string err = createResp.Errors != null && createResp.Errors.Count > 0
                    ? string.Join("\n", createResp.Errors) : "Failed to create record on the server.";
                MessageBox.Show(err, "Creation Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            record.Id = createResp.Data.ToString();
            ToastNotification.Show("Record Added", record.Name + " was added successfully.", ToastType.Success);
            _auditLog.LogCreate(record.Id, record.Name, GetCurrentUser(), CurrentUserRole);
            (System.Windows.Application.Current.MainWindow as MainWindow)?.ClearPageCache("SubsidyRecommendation");
            await LoadRecordsAsync();
        }

        public async Task SaveEditedRecordAsync(SoloParentRecord rawRecord, SoloParentRecord editedRecord, SoloParentDto existingDto)
        {
            if (!Guid.TryParse(rawRecord.Id, out var guid)) return;

            var updateReq  = _api.MapToUpdateRequest(guid, editedRecord, existingDto);
            var updateResp = await _api.UpdateSoloParentAsync(guid, updateReq);

            if (!updateResp.Succeeded)
            {
                string err = updateResp.Errors != null && updateResp.Errors.Count > 0
                    ? string.Join("\n", updateResp.Errors) : "Failed to update record on the server.";
                MessageBox.Show(err, "Update Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ToastNotification.Show("Record Updated", editedRecord.Name + " was updated.", ToastType.Info);
            
            // Track all field-level changes for detailed audit trail
            var changeService = new ChangeTrackingService();
            var changes = changeService.DetectChanges(rawRecord, editedRecord);
            
            _auditLog.LogUpdateWithChanges(editedRecord.Id, editedRecord.Name, GetCurrentUser(), CurrentUserRole, changes);
            await LoadRecordsAsync();
        }

        public async Task RenewRecordAsync(SoloParentRecord original, SoloParentRecord updated)
        {
            if (!Guid.TryParse(original.Id, out var guid))
            {
                MessageBox.Show("Cannot renew: invalid record ID.", "Renew", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var renewResp = await _api.RenewSoloParentRecordAsync(guid);
            if (!renewResp.Succeeded)
            {
                string err = renewResp.Errors != null && renewResp.Errors.Count > 0
                    ? string.Join("\n", renewResp.Errors) : "Failed to renew record.";
                MessageBox.Show(err, "Renew Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var updateReq  = _api.MapToUpdateRequest(guid, updated);
            var updateResp = await _api.UpdateSoloParentAsync(guid, updateReq);
            if (!updateResp.Succeeded)
            {
                string err = updateResp.Errors != null && updateResp.Errors.Count > 0
                    ? string.Join("\n", updateResp.Errors) : "Failed to save updated fields.";
                MessageBox.Show(err, "Update Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newExpiry = DateTime.Today.AddYears(1).ToString("MMMM d, yyyy");
            ToastNotification.Show("Record Renewed", original.Name + " — valid until " + newExpiry + ".", ToastType.Success);
            
            // Track changes made during renewal
            var changeService = new ChangeTrackingService();
            var changes = changeService.DetectChanges(original, updated);
            if (changes.Count > 0)
            {
                _auditLog.LogUpdateWithChanges(original.Id, original.Name, GetCurrentUser(), CurrentUserRole, changes);
            }
            else
            {
                _auditLog.LogUpdate(original.Id, original.Name, GetCurrentUser(), CurrentUserRole,
                    "Record renewed. Valid until " + newExpiry + ".");
            }
            
            await LoadRecordsAsync();
        }

        public async Task<(SoloParentRecord record, SoloParentDto dto)> FetchFullRecordAsync(string id)
        {
            if (Guid.TryParse(id, out var guid))
            {
                var resp = await _api.GetSoloParentByIdAsync(guid);
                if (resp.Succeeded && resp.Data != null)
                    return (_api.MapToRecord(resp.Data), resp.Data);
            }
            var existing = _allRecords.Find(r => r.Id == id);
            return (existing?.RawModel, null);
        }

        private async Task DeleteRecordAsync(string id)
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

            var vm = _allRecords.Find(r => r.Id == id);
            string recordName = vm != null ? vm.Name : id;

            var deleteResp = await _api.DeleteSoloParentAsync(guid);
            if (!deleteResp.Succeeded)
            {
                string err = deleteResp.Errors != null && deleteResp.Errors.Count > 0
                    ? string.Join("\n", deleteResp.Errors) : "Failed to delete record from server.";
                MessageBox.Show(err, "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ToastNotification.Show("Record Deleted", "The record was removed.", ToastType.Warning);
            _auditLog.LogDelete(id, recordName, GetCurrentUser(), CurrentUserRole);
            await LoadRecordsAsync();
        }

        // ── Audit helpers exposed to code-behind ──────────────────────────────
        public void LogView(string id, string name) =>
            _auditLog.LogView(id, name, GetCurrentUser(), CurrentUserRole);

        public bool CanEditRecord(SoloParentRecordViewModel vm) =>
            !(IsBasicUser && !string.Equals(vm.RawModel.CreatedBy ?? string.Empty,
                CurrentUserName, StringComparison.OrdinalIgnoreCase));

        // ── Filter / search helpers ───────────────────────────────────────────
        private void ApplyFiltersAndPage() { _currentPage = 1; var _ = LoadRecordsAsync(); }

        private void UpdateClearSearchVisibility()
        {
            bool hasText = !string.IsNullOrEmpty(_searchIdText)
                        || !string.IsNullOrEmpty(_searchNameText)
                        || !string.IsNullOrEmpty(_searchBarangayText);
            IsClearSearchVisible = hasText ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClearSearch()
        {
            _searchIdText       = string.Empty;
            _searchNameText     = string.Empty;
            _searchBarangayText = string.Empty;
            OnPropertyChanged(nameof(SearchIdText));
            OnPropertyChanged(nameof(SearchNameText));
            OnPropertyChanged(nameof(SearchBarangayText));
            IsClearSearchVisible = Visibility.Collapsed;
            ApplyFiltersAndPage();
        }

        private void ApplyFilter(object param)
        {
            if (param is FilterArgs args)
            {
                _sexFilter      = args.Sex;
                _barangayFilter = args.Barangay;
                _statusFilter   = args.Status;
            }
            ApplyFiltersAndPage();
        }

        private void ResetFilter()
        {
            _sexFilter      = "All";
            _barangayFilter = "All";
            _statusFilter   = "All";
            OnPropertyChanged(nameof(SexFilter));
            OnPropertyChanged(nameof(BarangayFilter));
            OnPropertyChanged(nameof(StatusFilter));
            ApplyFiltersAndPage();
        }

        private void ApplySort(string tag)
        {
            _sortOption = string.IsNullOrEmpty(tag) ? "Name A-Z" : tag;
            ApplyFiltersAndPage();
        }

        // ── Paging helpers ────────────────────────────────────────────────────
        private void NextPage() { if (_currentPage < _totalPages) { _currentPage++; var _ = LoadRecordsAsync(); } }
        private void PrevPage() { if (_currentPage > 1) { _currentPage--; var _ = LoadRecordsAsync(); } }

        private void UpdatePagingProperties()
        {
            PageInfoText  = "Page " + _currentPage + " of " + _totalPages;
            IsBackVisible = _currentPage > 1           ? Visibility.Visible : Visibility.Collapsed;
            IsNextVisible = _currentPage < _totalPages ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Event-raising helpers ─────────────────────────────────────────────
        private void OnViewRecord(SoloParentRecordViewModel vm)
        {
            if (vm == null) return;
            LogView(vm.RawModel.Id, vm.Name);
            RequestViewRecord?.Invoke(this, vm);
        }

        private void OnEditRecord(string id)
        {
            var vm = _allRecords.Find(r => r.Id == id);
            if (vm == null) return;
            if (IsBasicUser && !string.Equals(vm.RawModel.CreatedBy ?? string.Empty, CurrentUserName, StringComparison.OrdinalIgnoreCase))
            {
                ToastNotification.Show("Access Denied", "You can only edit records you created.", ToastType.Warning);
                return;
            }
            RequestEditRecord?.Invoke(this, vm.RawModel);
        }

        private void OnRenewRecord(string id)
        {
            if (!Guid.TryParse(id, out _))
            {
                MessageBox.Show("Cannot renew record without a valid backend ID.", "Renew", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var vm = _allRecords.Find(r => r.Id == id);
            RequestRenewRecord?.Invoke(this, new RenewRecordArgs { Id = id, RecordName = vm != null ? vm.Name : id, RawModel = vm != null ? vm.RawModel : null });
        }

        private void OnExportRecord(string id)
        {
            var vm = _allRecords.Find(r => r.Id == id);
            if (vm == null) return;
            RequestExportRecord?.Invoke(this, vm);
        }

        // ── Utilities ─────────────────────────────────────────────────────────
        private string GetCurrentUser() =>
            !string.IsNullOrEmpty(CurrentUserName) ? CurrentUserName : "Admin";
    }

    // ── Argument DTOs for events ─────────────────────────────────────────────
    public class RenewRecordArgs
    {
        public string           Id         { get; set; }
        public string           RecordName { get; set; }
        public SoloParentRecord RawModel   { get; set; }
    }

    public class FilterArgs
    {
        public string Sex      { get; set; } = "All";
        public string Barangay { get; set; } = "All";
        public string Status   { get; set; } = "All";
    }
}
