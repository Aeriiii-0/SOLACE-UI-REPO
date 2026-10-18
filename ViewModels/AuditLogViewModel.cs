using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services.Api;

namespace SOLUM_UI.ViewModels
{
    public class AuditLogViewModel : INotifyPropertyChanged
    {
        // ── Constants ──────────────────────────────────────────────────────────
        private const int PageSize = 8;

        // ── Observable collection shown in the list ────────────────────────────
        public ObservableCollection<AuditLog> PagedLogs { get; } = new ObservableCollection<AuditLog>();

        // ── Tracks whether the first load has completed ────────────────────────
        public bool IsInitialized { get; private set; }

        // ── In-memory page cache ───────────────────────────────────────────────
        // Stores fetched pages so Back/Forward is instant without re-fetching.
        // Cleared when filters change or Refresh is pressed.
        private struct PageCacheEntry
        {
            public List<AuditLog> Items;
            public bool HasMore;
        }
        private readonly Dictionary<int, PageCacheEntry> _pageCache
            = new Dictionary<int, PageCacheEntry>();

        // ── User name cache (UserId -> FullName) ──────────────────────────────
        private static readonly Dictionary<string, string> _userNamesCache =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static bool _userCacheLoaded = false;
        private static readonly System.Threading.SemaphoreSlim _userCacheLock =
            new System.Threading.SemaphoreSlim(1, 1);

        // ── Loading / error state ──────────────────────────────────────────────
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotLoading)); OnPropertyChanged(nameof(HasNoLogs)); }
        }
        public bool IsNotLoading => !_isLoading;

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            private set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoLogs)); }
        }
        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        public bool HasNoLogs => IsNotLoading && !HasError && PagedLogs.Count == 0;

        // ── Role awareness ─────────────────────────────────────────────────────
        public bool IsEncoder => GetEncoderContext().isEncoder;
        public string HeaderTitle => IsEncoder ? "My Audit Logs" : "All Audit Logs";
        public string HeaderSubtitle => IsEncoder
            ? "Double-click any entry to view change details."
            : "Double-click any entry to view full change details.";
        public string SearchPlaceholder => IsEncoder ? "Viewing your activity logs (read-only)" : "Search by user ID…";

        // ── Filter properties ──────────────────────────────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }


        private string _selectedFilter = "All";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set { _selectedFilter = value; OnPropertyChanged(); }
        }

        private DateTime? _startDate;
        public DateTime? StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(); }
        }

        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get => _endDate;
            set { _endDate = value; OnPropertyChanged(); }
        }

        public List<string> ActionFilters { get; } = new List<string>
        {
            "All",
            // Solo Parent
            "CREATE_SOLO_PARENT", "UPDATE_SOLO_PARENT", "DELETE_SOLO_PARENT",
            "RENEW_SOLO_PARENT_RECORD", "REVOKE_SOLO_PARENT_RECORD",
            // User Management
            "ENABLE_USER", "DISABLE_USER", "UPDATE_USER", "DELETE_USER", "ADMIN_RESET_PASSWORD",
            // Authentication
            "LOGIN", "LOGOUT", "REGISTER_USER", "REGISTER_ADMIN", "CHANGE_PASSWORD",
            "REFRESH_TOKEN", "REFRESH_TOKEN_FAILED",
            // Subsidy
            "CREATE_SUBSIDY_CYCLE", "GENERATE_SUBSIDY_RECOMMENDATIONS", "SHORTLIST_SUBSIDY_CANDIDATES",
            "REJECT_SUBSIDY_OUTLIER", "CONFIRM_SUBSIDY_GRANTEES", "REVOKE_SUBSIDY_GRANT"
        };

        // ── Cursor-based pagination ────────────────────────────────────────────
        private readonly List<(DateTimeOffset? CursorTs, string CursorId)> _pageStack
            = new List<(DateTimeOffset?, string)>();
        private int _currentPageIndex;
        private bool _hasMore;

        public int CurrentPage => _currentPageIndex + 1;

        private string _pageInfo = "Loading…";
        public string PageInfo
        {
            get => _pageInfo;
            private set { _pageInfo = value; OnPropertyChanged(); }
        }

        public bool CanGoPrev => _currentPageIndex > 0 && IsNotLoading;
        public bool CanGoNext => _hasMore && IsNotLoading;

        // ── Commands ───────────────────────────────────────────────────────────
        public ICommand PrevPageCommand     { get; }
        public ICommand NextPageCommand     { get; }
        public ICommand SearchCommand       { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand RefreshCommand      { get; }
        public ICommand OpenDetailCommand   { get; }
        public ICommand CloseDetailCommand  { get; }

        // ── Detail panel ───────────────────────────────────────────────────────
        private AuditLog _selectedLog;
        public AuditLog SelectedLog
        {
            get => _selectedLog;
            set { _selectedLog = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsDetailVisible)); }
        }
        public bool IsDetailVisible => SelectedLog != null;

        // ── Constructor ────────────────────────────────────────────────────────
        public AuditLogViewModel()
        {
            _pageStack.Add((null, null));

            PrevPageCommand     = new RelayCommand(_ => FireAndForget(GoToPrevPageAsync),        _ => CanGoPrev);
            NextPageCommand     = new RelayCommand(_ => FireAndForget(GoToNextPageAsync),        _ => CanGoNext);
            SearchCommand       = new RelayCommand(_ => FireAndForget(ResetAndLoadAsync),        _ => IsNotLoading);
            ClearFiltersCommand = new RelayCommand(_ => FireAndForget(ClearFiltersAndLoadAsync), _ => IsNotLoading);
            RefreshCommand      = new RelayCommand(_ => FireAndForget(ForceRefreshAsync),        _ => IsNotLoading);
            OpenDetailCommand   = new RelayCommand(param => SelectedLog = param as AuditLog);
            CloseDetailCommand  = new RelayCommand(_ => SelectedLog = null);
        }

        // ── Public entry point ─────────────────────────────────────────────────
        public async Task LoadAsync()
        {
            await FetchPageAsync(_pageStack[_currentPageIndex]);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static void FireAndForget(Func<Task> asyncAction) => asyncAction();

        // ── Navigation ─────────────────────────────────────────────────────────
        private async Task GoToPrevPageAsync()
        {
            if (_currentPageIndex <= 0) return;
            _currentPageIndex--;
            await FetchPageAsync(_pageStack[_currentPageIndex]);
        }

        private async Task GoToNextPageAsync()
        {
            if (!_hasMore) return;
            int next = _currentPageIndex + 1;
            if (next < _pageStack.Count)
                _currentPageIndex = next;
            await FetchPageAsync(_pageStack[_currentPageIndex]);
        }

        private async Task ResetAndLoadAsync()
        {
            _pageStack.Clear();
            _pageStack.Add((null, null));
            _currentPageIndex = 0;
            _hasMore = false;
            _pageCache.Clear();
            await FetchPageAsync(_pageStack[0]);
        }

        private async Task ForceRefreshAsync()
        {
            // Same as reset but keeps filters intact
            _pageStack.Clear();
            _pageStack.Add((null, null));
            _currentPageIndex = 0;
            _hasMore = false;
            _pageCache.Clear();
            await FetchPageAsync(_pageStack[0]);
        }

        private async Task ClearFiltersAndLoadAsync()
        {
            _searchText     = string.Empty;
            _selectedFilter = "All";
            _startDate      = null;
            _endDate        = null;
            OnPropertyChanged(nameof(SearchText));
            OnPropertyChanged(nameof(SelectedFilter));
            OnPropertyChanged(nameof(StartDate));
            OnPropertyChanged(nameof(EndDate));
            await ResetAndLoadAsync();
        }

        // ── Core fetch ─────────────────────────────────────────────────────────
        private async Task FetchPageAsync((DateTimeOffset? CursorTs, string CursorId) cursor)
        {
            // ── Serve from cache if available (instant, no network call) ──────
            if (_pageCache.TryGetValue(_currentPageIndex, out var cached))
            {
                PagedLogs.Clear();
                foreach (var log in cached.Items)
                    PagedLogs.Add(log);
                _hasMore = cached.HasMore;
                UpdatePageInfo();
                RaiseNavChanged();
                return;
            }

            // ── Network fetch ─────────────────────────────────────────────────
            IsLoading    = true;
            ErrorMessage = null;
            RaiseNavChanged();

            try
            {
                string actionType = _selectedFilter == "All" ? null : _selectedFilter;

                DateTimeOffset? startDto = _startDate.HasValue
                    ? (DateTimeOffset?)new DateTimeOffset(_startDate.Value,
                        TimeZoneInfo.Local.GetUtcOffset(_startDate.Value))
                    : null;

                DateTimeOffset? endDto = _endDate.HasValue
                    ? (DateTimeOffset?)new DateTimeOffset(
                        _endDate.Value.Date.AddDays(1).AddTicks(-1),
                        TimeZoneInfo.Local.GetUtcOffset(_endDate.Value))
                    : null;

                var (isEncoder, currentUserId) = GetEncoderContext();
                string userId = isEncoder
                    ? (!string.IsNullOrWhiteSpace(currentUserId) ? currentUserId : null)
                    : (string.IsNullOrWhiteSpace(_searchText) ? null : _searchText.Trim());

                var result = await AuditLogApiService.Instance.GetLogsAsync(
                    pageSize:        PageSize,
                    userId:          userId,
                    actionType:      actionType,
                    startDate:       startDto,
                    endDate:         endDto,
                    cursorTimestamp: cursor.CursorTs,
                    cursorId:        cursor.CursorId);

                if (!result.Succeeded || result.Data == null)
                {
                    ErrorMessage = (result.Errors != null && result.Errors.Count > 0)
                        ? string.Join(Environment.NewLine, result.Errors)
                        : "Failed to load audit logs.";
                    PagedLogs.Clear();
                    return;
                }

                var page = result.Data;
                _hasMore = page.HasMore;

                // Push next cursor if at the frontier
                if (_currentPageIndex == _pageStack.Count - 1 && _hasMore)
                    _pageStack.Add((page.NextCursorTimestamp, page.NextCursorId));

                // Ensure user names cache is loaded in background/parallel
                await EnsureUserCacheLoadedAsync();

                // Populate list
                PagedLogs.Clear();
                foreach (var dto in page.Items)
                    PagedLogs.Add(MapToAuditLog(dto));

                // ── Store in cache ─────────────────────────────────────────────
                _pageCache[_currentPageIndex] = new PageCacheEntry
                {
                    Items   = new List<AuditLog>(PagedLogs),
                    HasMore = _hasMore
                };

                UpdatePageInfo();
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                RaiseNavChanged();
            }
        }

        public static async Task EnsureUserCacheLoadedAsync()
        {
            // Seed current logged-in user immediately
            string curId = MainWindow.CurrentUserId;
            string curName = MainWindow.FullName;
            if (!string.IsNullOrWhiteSpace(curId) && !string.IsNullOrWhiteSpace(curName))
            {
                lock (_userNamesCache)
                {
                    _userNamesCache[curId] = curName;
                }
            }

            if (_userCacheLoaded) return;

            // Only Admins have permission to query /api/user. Encoders manage solo parents only.
            if (!AuthApiService.Instance.IsAdmin)
            {
                _userCacheLoaded = true;
                return;
            }

            if (!await _userCacheLock.WaitAsync(0)) return;

            try
            {
                if (_userCacheLoaded) return;

                // Fetch up to 50 encoders
                var encoderReq = new GetApplicationUserRequest
                {
                    Role = "Encoder",
                    Page = 1,
                    PageSize = 50
                };
                var encoderTask = UserApiService.Instance.GetApplicationUsersAsync(encoderReq);

                // Fetch up to 50 admins
                var adminReq = new GetApplicationUserRequest
                {
                    Role = "Admin",
                    Page = 1,
                    PageSize = 50
                };
                var adminTask = UserApiService.Instance.GetApplicationUsersAsync(adminReq);

                await Task.WhenAll(encoderTask, adminTask);

                lock (_userNamesCache)
                {
                    if (encoderTask.Result?.Succeeded == true && encoderTask.Result.Data?.Items != null)
                    {
                        foreach (var u in encoderTask.Result.Data.Items)
                        {
                            string fullName = $"{u.FirstName} {u.LastName}".Trim();
                            if (!string.IsNullOrWhiteSpace(fullName))
                            {
                                _userNamesCache[u.Id.ToString()] = fullName;
                                if (!string.IsNullOrWhiteSpace(u.Email))
                                    _userNamesCache[u.Email] = fullName;
                            }
                        }
                    }

                    if (adminTask.Result?.Succeeded == true && adminTask.Result.Data?.Items != null)
                    {
                        foreach (var u in adminTask.Result.Data.Items)
                        {
                            string fullName = $"{u.FirstName} {u.LastName}".Trim();
                            if (!string.IsNullOrWhiteSpace(fullName))
                            {
                                _userNamesCache[u.Id.ToString()] = fullName;
                                if (!string.IsNullOrWhiteSpace(u.Email))
                                    _userNamesCache[u.Email] = fullName;
                            }
                        }
                    }
                }

                _userCacheLoaded = true;
            }
            catch
            {
                // Soft-fail: fallback to IDs if user list couldn't be loaded
            }
            finally
            {
                _userCacheLock.Release();
            }
        }

        public static string ResolveUserName(string rawUserId)
        {
            if (string.IsNullOrWhiteSpace(rawUserId))
                return string.Empty;

            // Direct check in cache
            lock (_userNamesCache)
            {
                if (_userNamesCache.TryGetValue(rawUserId.Trim(), out var name) && !string.IsNullOrWhiteSpace(name))
                    return name;
            }

            // Check if matches current user id or email
            if (!string.IsNullOrWhiteSpace(MainWindow.CurrentUserId) &&
                string.Equals(rawUserId.Trim(), MainWindow.CurrentUserId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return MainWindow.FullName;
            }
            if (!string.IsNullOrWhiteSpace(MainWindow.CurrentUserEmail) &&
                string.Equals(rawUserId.Trim(), MainWindow.CurrentUserEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return MainWindow.FullName;
            }

            return string.Empty;
        }

        private void UpdatePageInfo()
        {
            int shown = PagedLogs.Count;
            PageInfo = shown == 0
                ? "No results"
                : $"Page {CurrentPage}  ·  {shown} record{(shown == 1 ? "" : "s")}";
        }

        private void RaiseNavChanged()
        {
            OnPropertyChanged(nameof(CanGoPrev));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(CurrentPage));
            OnPropertyChanged(nameof(HasNoLogs));
        }

        // ── DTO → model ────────────────────────────────────────────────────────
        private static AuditLog MapToAuditLog(AuditLogDto dto)
        {
            string logId      = !string.IsNullOrWhiteSpace(dto.Id) ? dto.Id : string.Empty;
            string userId     = !string.IsNullOrWhiteSpace(dto.UserId) ? dto.UserId : (dto.PerformedBy?.UserId ?? string.Empty);
            string userRole   = !string.IsNullOrWhiteSpace(dto.Role) ? dto.Role : (dto.PerformedBy?.Role ?? string.Empty);
            string targetId   = !string.IsNullOrWhiteSpace(dto.TargetId) ? dto.TargetId : (dto.Target?.TargetId ?? string.Empty);
            string targetType = !string.IsNullOrWhiteSpace(dto.TargetType) ? dto.TargetType : (dto.Target?.TargetType ?? string.Empty);
            string targetName = !string.IsNullOrWhiteSpace(dto.TargetName) ? dto.TargetName : (dto.Target?.TargetName ?? string.Empty);
            string ipAddress  = !string.IsNullOrWhiteSpace(dto.IpAddress) ? dto.IpAddress : (dto.Metadata?.IpAddress ?? string.Empty);
            string status     = !string.IsNullOrWhiteSpace(dto.Status) ? dto.Status : (dto.Metadata?.Status ?? string.Empty);
            string rawType    = dto.ActionType ?? string.Empty;

            string resolvedName = ResolveUserName(userId);
            string displayName = !string.IsNullOrWhiteSpace(resolvedName) ? resolvedName : userId;

            string recordName = !string.IsNullOrWhiteSpace(targetName) ? targetName : (!string.IsNullOrWhiteSpace(targetId) ? targetId : targetType);
            string description = BuildDescription(rawType, displayName, recordName, status);

            var changesSource = (dto.Changes != null && dto.Changes.Count > 0)
                ? dto.Changes
                : (dto.Metadata?.Changes ?? new List<ChangeItemDto>());

            var changesList = changesSource
                .Select(c => new ChangeDetail(
                    c.FieldName ?? c.Field ?? string.Empty,
                    c.OldValue,
                    c.NewValue))
                .ToList();

            return new AuditLog
            {
                Id                = 0,
                AuditLogId        = logId,
                RawActionType     = rawType,
                Action            = MapAction(rawType),
                Description       = description,
                PerformedBy       = displayName,
                PerformedByName   = resolvedName,
                PerformedByUserId = userId,
                PerformedByRole   = userRole,
                Timestamp         = dto.Timestamp.LocalDateTime,
                RecordId          = targetId,
                RecordName        = recordName,
                IpAddress         = ipAddress,
                Status            = status,
                TargetType        = targetType,
                FieldChanged      = dto.Metadata?.FieldChanged ?? string.Empty,
                OldValue          = dto.Metadata?.OldValue     ?? string.Empty,
                NewValue          = dto.Metadata?.NewValue     ?? string.Empty,
                Changes           = changesList,
            };
        }

        private static string BuildDescription(string rawActionType, string actor, string target, string status)
        {
            string who    = !string.IsNullOrWhiteSpace(actor)  ? actor  : "Unknown";
            string suffix = !string.IsNullOrWhiteSpace(status) ? $" · {status}" : string.Empty;
            string tgt    = !string.IsNullOrWhiteSpace(target) ? $" · {target}" : string.Empty;

            switch (rawActionType.ToUpperInvariant())
            {
                case "LOGIN":                           return $"{who} logged in{suffix}";
                case "LOGOUT":                          return $"{who} logged out{suffix}";
                case "REFRESH_TOKEN":                   return $"{who} refreshed session token";
                case "REFRESH_TOKEN_FAILED":            return $"Session token refresh failed for {who}{suffix}";
                case "CHANGE_PASSWORD":                 return $"{who} changed their password{suffix}";
                case "REGISTER_USER":                   return $"New user registered{tgt}";
                case "REGISTER_ADMIN":                  return $"New admin registered{tgt}";
                case "UPDATE_USER":                     return $"User account updated{tgt}";
                case "ENABLE_USER":                     return $"User account enabled{tgt}";
                case "DISABLE_USER":                    return $"User account disabled{tgt}";
                case "DELETE_USER":                     return $"User account deleted{tgt}";
                case "ADMIN_RESET_PASSWORD":            return $"Admin reset user password{tgt}";
                case "CREATE_SOLO_PARENT":              return $"Solo parent record created{tgt}";
                case "UPDATE_SOLO_PARENT":              return $"Solo parent record updated{tgt}";
                case "DELETE_SOLO_PARENT":              return $"Solo parent record deleted{tgt}";
                case "RENEW_SOLO_PARENT":
                case "RENEW_SOLO_PARENT_RECORD":        return $"Solo parent record renewed{tgt}";
                case "REVOKE_SOLO_PARENT_RECORD":       return $"Solo parent record revoked{tgt}";
                case "CREATE_SUBSIDY_CYCLE":            return $"Subsidy cycle created{tgt}";
                case "GENERATE_SUBSIDY_RECOMMENDATIONS":return $"Subsidy recommendations generated{tgt}";
                case "SHORTLIST_SUBSIDY_CANDIDATES":    return $"Subsidy candidates shortlisted{tgt}";
                case "REJECT_SUBSIDY_OUTLIER":          return $"Subsidy candidate rejected{tgt}";
                case "CONFIRM_SUBSIDY_GRANTEES":        return $"Subsidy grantees confirmed{tgt}";
                case "REVOKE_SUBSIDY_GRANT":            return $"Subsidy grant revoked{tgt}";
                default:
                    return !string.IsNullOrWhiteSpace(rawActionType)
                        ? $"{rawActionType.Replace('_', ' ')}{tgt}{suffix}"
                        : $"Action performed by {who}";
            }
        }

        private static AuditAction MapAction(string actionType)
        {
            if (string.IsNullOrWhiteSpace(actionType)) return AuditAction.System;
            switch (actionType.Trim().ToUpperInvariant())
            {
                case "CREATE_SOLO_PARENT":
                case "REGISTER_USER":
                case "REGISTER_ADMIN":
                case "CREATE_SUBSIDY_CYCLE":
                    return AuditAction.Create;

                case "UPDATE_SOLO_PARENT":
                case "UPDATE_USER":
                case "ENABLE_USER":
                case "CHANGE_PASSWORD":
                case "ADMIN_RESET_PASSWORD":
                case "RENEW_SOLO_PARENT":
                case "RENEW_SOLO_PARENT_RECORD":
                case "REFRESH_TOKEN":
                case "SHORTLIST_SUBSIDY_CANDIDATES":
                case "CONFIRM_SUBSIDY_GRANTEES":
                    return AuditAction.Update;

                case "DELETE_SOLO_PARENT":
                case "REVOKE_SOLO_PARENT_RECORD":
                case "DISABLE_USER":
                case "DELETE_USER":
                case "REJECT_SUBSIDY_OUTLIER":
                case "REVOKE_SUBSIDY_GRANT":
                    return AuditAction.Delete;

                case "LOGIN":
                    return AuditAction.Login;

                case "LOGOUT":
                    return AuditAction.Logout;

                case "REFRESH_TOKEN_FAILED":
                case "GENERATE_SUBSIDY_RECOMMENDATIONS":
                default:
                    return AuditAction.System;
            }
        }

        private static (bool isEncoder, string userId) GetEncoderContext()
        {
            bool isEncoder = AuthApiService.Instance.IsEncoder ||
                             string.Equals(MainWindow.CurrentUserRole, "Encoder", StringComparison.OrdinalIgnoreCase);
            string userId = !string.IsNullOrWhiteSpace(AuthApiService.Instance.CurrentUserId)
                ? AuthApiService.Instance.CurrentUserId
                : MainWindow.CurrentUserId;
            return (isEncoder, userId);
        }

        // ── INotifyPropertyChanged ─────────────────────────────────────────────
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
