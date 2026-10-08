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
        private string _month;
        private int _value;
        private int _maxValue;
        private string _tooltipText;

        public string Month
        {
            get => _month;
            set { _month = value; OnPropertyChanged(); }
        }

        public int Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); OnPropertyChanged(nameof(BarHeight)); }
        }

        public int MaxValue
        {
            get => _maxValue;
            set { _maxValue = value; OnPropertyChanged(); OnPropertyChanged(nameof(BarHeight)); }
        }

        public string TooltipText
        {
            get => _tooltipText;
            set { _tooltipText = value; OnPropertyChanged(); }
        }

        public double BarHeight => MaxValue > 0 ? (Value / (double)MaxValue) * 200.0 : 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        private static readonly string[] _monthLabels = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

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

        private string _chartSubtitle = $"New solo parents registered — Jan to Dec {DateTime.Now.Year}";
        public string ChartSubtitle
        {
            get => _chartSubtitle;
            set { _chartSubtitle = value; OnPropertyChanged(); }
        }

        private string _chartYMax = "400";
        public string ChartYMax
        {
            get => _chartYMax;
            set { _chartYMax = value; OnPropertyChanged(); }
        }

        private string _chartYMid = "200";
        public string ChartYMid
        {
            get => _chartYMid;
            set { _chartYMid = value; OnPropertyChanged(); }
        }

        private string _chartYLow = "100";
        public string ChartYLow
        {
            get => _chartYLow;
            set { _chartYLow = value; OnPropertyChanged(); }
        }

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

                int currentYear = DateTime.Now.Year;
                ChartSubtitle = $"New solo parents registered — Jan to Dec {currentYear}";

                var statsTask = AnalyticsApiService.Instance.GetDashboardAnalyticsAsync();
                var logsTask  = AuditLogApiService.Instance.GetLogsAsync(pageSize: 10, userId: effectiveUserId);
                var monthlyTasks = Enumerable.Range(1, 12)
                    .Select(m => AnalyticsApiService.Instance.GetMonthlyAnalyticsAsync(currentYear, m))
                    .ToArray();

                await Task.WhenAll(new Task[] { statsTask, logsTask }.Concat(monthlyTasks));

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

                // Populate 12-month chart from API responses
                var monthlyValues = new int[12];
                for (int i = 0; i < 12; i++)
                {
                    var res = monthlyTasks[i].Result;
                    if (res != null && res.Succeeded && res.Data != null)
                    {
                        monthlyValues[i] = res.Data.NewRegistrations;
                    }
                }

                int maxVal = Math.Max(10, monthlyValues.Max());
                ChartYMax = maxVal.ToString();
                ChartYMid = (maxVal / 2).ToString();
                ChartYLow = (maxVal / 4).ToString();

                MonthlyBars.Clear();
                for (int i = 0; i < 12; i++)
                {
                    int val = monthlyValues[i];
                    MonthlyBars.Add(new MonthBar
                    {
                        Month = _monthLabels[i],
                        Value = val,
                        MaxValue = maxVal,
                        TooltipText = $"{_monthLabels[i]} {currentYear}: {val:N0} new registrations"
                    });
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
            string targetId   = dto.Target?.TargetId    ?? string.Empty;
            string targetType = dto.Target?.TargetType  ?? string.Empty;
            string targetName = dto.Target?.TargetName  ?? string.Empty;
            string status     = dto.Metadata?.Status    ?? string.Empty;
            string recordName = !string.IsNullOrWhiteSpace(targetName) ? targetName : (!string.IsNullOrWhiteSpace(targetId) ? targetId : targetType);

            return new AuditLog
            {
                RawActionType   = rawType,
                Action          = MapAction(rawType),
                Description     = BuildLogDescription(rawType, userId, recordName, status),
                PerformedBy     = userId,
                PerformedByRole = userRole,
                Timestamp       = dto.Timestamp.LocalDateTime,
                RecordId        = targetId,
                RecordName      = recordName,
                IpAddress       = dto.Metadata?.IpAddress ?? string.Empty,
                Status          = status,
                TargetType      = targetType,
                FieldChanged    = dto.Metadata?.FieldChanged ?? string.Empty,
                OldValue        = dto.Metadata?.OldValue     ?? string.Empty,
                NewValue        = dto.Metadata?.NewValue     ?? string.Empty,
            };
        }

        private static AuditAction MapAction(string actionType)
        {
            if (string.IsNullOrWhiteSpace(actionType)) return AuditAction.System;
            switch (actionType.Trim().ToUpperInvariant())
            {
                case "CREATE_SOLO_PARENT":
                case "REGISTER_USER":
                case "REGISTER_ADMIN":   return AuditAction.Create;
                case "UPDATE_SOLO_PARENT":
                case "UPDATE_USER":
                case "CHANGE_PASSWORD":
                case "RENEW_SOLO_PARENT":
                case "REFRESH_TOKEN":    return AuditAction.Update;
                case "DELETE_SOLO_PARENT":
                case "DISABLE_USER":     return AuditAction.Delete;
                case "LOGIN":            return AuditAction.Login;
                case "LOGOUT":           return AuditAction.Logout;
                default:                 return AuditAction.System;
            }
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
            MonthlyBars.Clear();
            int currentYear = DateTime.Now.Year;
            for (int i = 0; i < _monthLabels.Length; i++)
            {
                MonthlyBars.Add(new MonthBar
                {
                    Month = _monthLabels[i],
                    Value = 0,
                    MaxValue = 100,
                    TooltipText = $"{_monthLabels[i]} {currentYear}: 0 new registrations"
                });
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
