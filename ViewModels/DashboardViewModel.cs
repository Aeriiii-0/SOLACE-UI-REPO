using System;
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
    public class MonthBar : INotifyPropertyChanged
    {
        public string Month { get; set; }
        public int Value { get; set; }
        public int MaxValue { get; set; }

        public double BarHeight => MaxValue > 0 ? (Value / (double)MaxValue) * 200.0 : 0;

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        private static readonly int[] _monthlyValues = { 330, 295, 245, 325, 295, 235, 330 };
        private static readonly string[] _monthLabels = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul" };

        private int _registeredSoloParents;
        public int RegisteredSoloParents
        {
            get => _registeredSoloParents;
            private set { _registeredSoloParents = value; OnPropertyChanged(); }
        }

        private int _activeSoloParents;
        public int ActiveSoloParents
        {
            get => _activeSoloParents;
            private set { _activeSoloParents = value; OnPropertyChanged(); }
        }

        private int _renewalsDueThisMonth;
        public int RenewalsDueThisMonth
        {
            get => _renewalsDueThisMonth;
            private set { _renewalsDueThisMonth = value; OnPropertyChanged(); }
        }

        private string _registeredDelta = "";
        public string RegisteredDelta
        {
            get => _registeredDelta;
            private set { _registeredDelta = value; OnPropertyChanged(); }
        }

        private string _activeDelta = "";
        public string ActiveDelta
        {
            get => _activeDelta;
            private set { _activeDelta = value; OnPropertyChanged(); }
        }

        private string _renewalsDelta = "";
        public string RenewalsDelta
        {
            get => _renewalsDelta;
            private set { _renewalsDelta = value; OnPropertyChanged(); }
        }

        private bool _isActiveChangeNegative;
        public bool IsActiveChangeNegative
        {
            get => _isActiveChangeNegative;
            private set { _isActiveChangeNegative = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                _isLoading = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            private set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public ObservableCollection<MonthBar> MonthlyBars { get; } = new ObservableCollection<MonthBar>();
        public ObservableCollection<AuditLog> RecentLogs  { get; } = new ObservableCollection<AuditLog>();

        private AuditLog _selectedLog;
        public AuditLog SelectedLog
        {
            get => _selectedLog;
            set { _selectedLog = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsLogDetailVisible)); }
        }
        public bool IsLogDetailVisible => _selectedLog != null;

        public ICommand SelectLogCommand     { get; }
        public ICommand CloseLogDetailCommand { get; }
        public ICommand RefreshCommand        { get; }

        public DashboardViewModel()
        {
            SelectLogCommand      = new RelayCommand(param => SelectedLog = param as AuditLog);
            CloseLogDetailCommand = new RelayCommand(_ => SelectedLog = null);
            RefreshCommand        = new RelayCommand(
                async _ => await LoadAsync(),
                _         => !IsLoading);

            LoadChart();
        }

        public async Task LoadAsync()
        {
            if (IsLoading) return;

            IsLoading    = true;
            ErrorMessage = null;

            try
            {
                var (isEncoder, currentUserId) = GetEncoderContext();
                string effectiveUserId = isEncoder && !string.IsNullOrWhiteSpace(currentUserId) ? currentUserId : null;

                var statsTask = AnalyticsApiService.Instance.GetDashboardAnalyticsAsync();
                var logsTask  = AuditLogApiService.Instance.GetLogsAsync(pageSize: 10, userId: effectiveUserId);

                await Task.WhenAll(statsTask, logsTask);

                var statsResponse = statsTask.Result;
                if (statsResponse != null && statsResponse.Succeeded && statsResponse.Data != null)
                    Apply(statsResponse.Data);
                else
                    ErrorMessage = statsResponse?.Errors?.Count > 0
                        ? string.Join(Environment.NewLine, statsResponse.Errors)
                        : "Failed to load dashboard data.";

                var logsResponse = logsTask.Result;
                RecentLogs.Clear();
                if (logsResponse != null && logsResponse.Succeeded && logsResponse.Data?.Items != null)
                {
                    foreach (var dto in logsResponse.Data.Items)
                        RecentLogs.Add(MapDtoToLog(dto));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unable to reach the server: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Apply(DashboardAnalyticsDto d)
        {
            RegisteredSoloParents = d.TotalRegistered;
            ActiveSoloParents     = d.ActiveRecords;
            RenewalsDueThisMonth  = d.RenewalsDueThisMonth;

            RegisteredDelta = $"↑ {d.RegisteredThisMonth:N0} this month";

            IsActiveChangeNegative = d.ActiveChangeFromLastMonth < 0;
            ActiveDelta = d.ActiveChangeFromLastMonth > 0
                ? $"↑ {d.ActiveChangeFromLastMonth:N0} from last month"
                : d.ActiveChangeFromLastMonth < 0
                    ? $"↓ {Math.Abs(d.ActiveChangeFromLastMonth):N0} from last month"
                    : "No change from last month";

            RenewalsDelta = $"↑ {d.RenewalsCompletedToday:N0} completed today";
        }

        private static AuditLog MapDtoToLog(SOLUM_UI.Models.Api.AuditLogDto dto)
        {
            string rawType    = dto.ActionType  ?? string.Empty;
            string userId     = dto.PerformedBy?.UserId ?? string.Empty;
            string userRole   = dto.PerformedBy?.Role   ?? string.Empty;
            string targetType = dto.Target?.TargetType  ?? string.Empty;
            string targetName = dto.Target?.TargetName  ?? string.Empty;
            string status     = dto.Metadata?.Status    ?? string.Empty;
            string recordName = !string.IsNullOrWhiteSpace(targetName) ? targetName : targetType;

            return new AuditLog
            {
                RawActionType   = rawType,
                Action          = AuditAction.System,
                Description     = BuildLogDescription(rawType, userId, recordName, status),
                PerformedBy     = userId,
                PerformedByRole = userRole,
                Timestamp       = dto.Timestamp.LocalDateTime,
                RecordName      = recordName,
                IpAddress       = dto.Metadata?.IpAddress ?? string.Empty,
                Status          = status,
                TargetType      = targetType,
            };
        }

        private static string BuildLogDescription(string rawType, string actor, string target, string status)
        {
            string who    = !string.IsNullOrWhiteSpace(actor)  ? actor  : "Unknown";
            string suffix = !string.IsNullOrWhiteSpace(status) ? $" · {status}" : string.Empty;
            string tgt    = !string.IsNullOrWhiteSpace(target) ? $" · {target}" : string.Empty;

            switch (rawType.ToUpperInvariant())
            {
                case "LOGIN":               return $"{who} logged in{suffix}";
                case "LOGOUT":              return $"{who} logged out{suffix}";
                case "REFRESH_TOKEN":       return $"{who} refreshed session token";
                case "CHANGE_PASSWORD":     return $"{who} changed password{suffix}";
                case "REGISTER_USER":       return $"New user registered{tgt}";
                case "REGISTER_ADMIN":      return $"New admin registered{tgt}";
                case "UPDATE_USER":         return $"User updated{tgt}";
                case "DISABLE_USER":        return $"User disabled{tgt}";
                case "CREATE_SOLO_PARENT":  return $"Record created{tgt}";
                case "UPDATE_SOLO_PARENT":  return $"Record updated{tgt}";
                case "DELETE_SOLO_PARENT":  return $"Record deleted{tgt}";
                case "RENEW_SOLO_PARENT":   return $"Record renewed{tgt}";
                default:
                    return !string.IsNullOrWhiteSpace(rawType)
                        ? $"{rawType.Replace('_', ' ')}{tgt}{suffix}"
                        : $"Action by {who}";
            }
        }

        private void LoadChart()
        {
            int max = _monthlyValues.Max();
            MonthlyBars.Clear();
            for (int i = 0; i < _monthLabels.Length; i++)
                MonthlyBars.Add(new MonthBar { Month = _monthLabels[i], Value = _monthlyValues[i], MaxValue = max });
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
