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

        /// <summary>Pixel height relative to max, capped at 200px.</summary>
        public double BarHeight => MaxValue > 0 ? (Value / (double)MaxValue) * 200.0 : 0;

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        private string _chartSubtitle = "New solo parents registered";
        public string ChartSubtitle
        {
            get => _chartSubtitle;
            private set { _chartSubtitle = value; OnPropertyChanged(); }
        }

        private int _yAxisMax = 100;
        public int YAxisMax
        {
            get => _yAxisMax;
            private set { _yAxisMax = value; OnPropertyChanged(); }
        }

        private int _yAxisMidHigh = 75;
        public int YAxisMidHigh
        {
            get => _yAxisMidHigh;
            private set { _yAxisMidHigh = value; OnPropertyChanged(); }
        }

        private int _yAxisMidLow = 50;
        public int YAxisMidLow
        {
            get => _yAxisMidLow;
            private set { _yAxisMidLow = value; OnPropertyChanged(); }
        }

        private int _yAxisMin = 0;
        public int YAxisMin
        {
            get => _yAxisMin;
            private set { _yAxisMin = value; OnPropertyChanged(); }
        }

        // --- stats from API (same property names as before, so existing XAML bindings keep working) ---
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

        private int _inactiveSoloParents;
        public int InactiveSoloParents
        {
            get => _inactiveSoloParents;
            private set { _inactiveSoloParents = value; OnPropertyChanged(); }
        }

        private int _renewalsDueThisMonth;
        public int RenewalsDueThisMonth
        {
            get => _renewalsDueThisMonth;
            private set { _renewalsDueThisMonth = value; OnPropertyChanged(); }
        }

        private int _renewalsThisMonth;
        public int RenewalsThisMonth
        {
            get => _renewalsThisMonth;
            private set { _renewalsThisMonth = value; OnPropertyChanged(); }
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

        // For red/green styling of the Active delta (can be negative)
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
            private set { _isLoading = value; OnPropertyChanged(); }
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
        public ObservableCollection<BarangayBreakdownItemDto> TopBarangays { get; } = new ObservableCollection<BarangayBreakdownItemDto>();

        private string _quarterPeriodLabel = "";
        public string QuarterPeriodLabel
        {
            get => _quarterPeriodLabel;
            private set { _quarterPeriodLabel = value; OnPropertyChanged(); }
        }

        private int _quarterGrandTotal;
        public int QuarterGrandTotal
        {
            get => _quarterGrandTotal;
            private set { _quarterGrandTotal = value; OnPropertyChanged(); }
        }

        // ── Log detail panel ──────────────────────────────────────────────────
        private AuditLog _selectedLog;
        public AuditLog SelectedLog
        {
            get => _selectedLog;
            set { _selectedLog = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsLogDetailVisible)); }
        }
        public bool IsLogDetailVisible => _selectedLog != null;

        public ICommand SelectLogCommand    { get; }
        public ICommand CloseLogDetailCommand { get; }

        public DashboardViewModel()
        {
            LoadChart();

            SelectLogCommand     = new RelayCommand(param => SelectedLog = param as AuditLog);
            CloseLogDetailCommand = new RelayCommand(_ => SelectedLog = null);
        }

        /// <summary>Fetches dashboard stats, quarterly breakdown, and recent audit logs from the API.</summary>
        public async Task LoadAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            ErrorMessage = null;

            try
            {
                var (isEncoder, currentUserId) = GetEncoderContext();
                string effectiveUserId = isEncoder && !string.IsNullOrWhiteSpace(currentUserId) ? currentUserId : null;

                int currentQuarter = (DateTime.Today.Month - 1) / 3 + 1;

                // Run stats, breakdown, and recent logs in parallel
                var statsTask     = AnalyticsApiService.Instance.GetDashboardAnalyticsAsync();
                var logsTask      = AuditLogApiService.Instance.GetLogsAsync(pageSize: 10, userId: effectiveUserId);
                var breakdownTask = AnalyticsApiService.Instance.GetBarangayBreakdownAsync(quarter: currentQuarter, year: DateTime.Today.Year);

                await System.Threading.Tasks.Task.WhenAll(statsTask, logsTask, breakdownTask);

                // Apply stats
                var statsResponse = statsTask.Result;
                if (statsResponse != null && statsResponse.Succeeded && statsResponse.Data != null)
                    Apply(statsResponse.Data);
                else
                    ErrorMessage = statsResponse?.Errors?.Count > 0
                        ? string.Join(Environment.NewLine, statsResponse.Errors)
                        : "Failed to load dashboard data.";

                // Apply quarterly barangay breakdown
                var breakdownResponse = breakdownTask.Result;
                if (breakdownResponse != null && breakdownResponse.Succeeded && breakdownResponse.Data != null)
                {
                    QuarterPeriodLabel = !string.IsNullOrWhiteSpace(breakdownResponse.Data.PeriodLabel)
                        ? breakdownResponse.Data.PeriodLabel
                        : $"Q{currentQuarter} {DateTime.Today.Year}";
                    QuarterGrandTotal = breakdownResponse.Data.GrandTotalSoloParents;

                    TopBarangays.Clear();
                    if (breakdownResponse.Data.Barangays != null)
                    {
                        foreach (var b in breakdownResponse.Data.Barangays.OrderByDescending(x => x.TotalSoloParents).Take(5))
                        {
                            TopBarangays.Add(b);
                        }
                    }
                }

                // Apply recent logs
                var logsResponse = logsTask.Result;
                RecentLogs.Clear();
                if (logsResponse != null && logsResponse.Succeeded && logsResponse.Data?.Items != null)
                {
                    foreach (var dto in logsResponse.Data.Items)
                        RecentLogs.Add(MapDtoToLog(dto));
                }

                // Load accurate monthly registration chart from API
                try
                {
                    var monthlyCounts = new System.Collections.Generic.Dictionary<string, int>();
                    var currentDt = DateTime.Today;
                    var monthTasks = new System.Collections.Generic.List<(string Key, Task<BaseResponse<MonthlyAnalyticsDto>> Task)>();

                    for (int i = 5; i >= 0; i--)
                    {
                        var target = currentDt.AddMonths(-i);
                        string key = target.ToString("yyyy-MM");
                        var t = AnalyticsApiService.Instance.GetMonthlyAnalyticsAsync(target.Year, target.Month);
                        monthTasks.Add((key, t));
                    }

                    await Task.WhenAll(monthTasks.Select(x => x.Task));

                    foreach (var item in monthTasks)
                    {
                        var res = item.Task.Result;
                        if (res?.Succeeded == true && res.Data != null)
                        {
                            monthlyCounts[item.Key] = res.Data.TotalSoloParents;
                        }
                    }

                    LoadChart(monthlyCounts);
                }
                catch
                {
                    // Fallback to initial chart if monthly analytics query fails
                    LoadChart();
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
            InactiveSoloParents   = d.InactiveRecords > 0 ? d.InactiveRecords : (d.TotalRegistered - d.ActiveRecords);
            RenewalsDueThisMonth  = d.RenewalsDueThisMonth;
            RenewalsThisMonth     = d.RenewalsThisMonth;

            RegisteredDelta = $"↑ {d.RegisteredThisMonth:N0} this month";

            IsActiveChangeNegative = d.ActiveChangeFromLastMonth < 0;
            ActiveDelta = d.ActiveChangeFromLastMonth > 0
                ? $"↑ {d.ActiveChangeFromLastMonth:N0} from last month"
                : d.ActiveChangeFromLastMonth < 0
                    ? $"↓ {Math.Abs(d.ActiveChangeFromLastMonth):N0} from last month"
                    : "No change from last month";

            RenewalsDelta = d.RenewalsThisMonth > 0
                ? $"↑ {d.RenewalsThisMonth:N0} this month"
                : $"↑ {d.RenewalsCompletedToday:N0} completed today";
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

        private void LoadChart(System.Collections.Generic.Dictionary<string, int> monthlyCounts = null)
        {
            var bars = new System.Collections.Generic.List<(string Label, int Count)>();
            int currentYear = DateTime.Today.Year;
            int currentMonth = DateTime.Today.Month;

            // Generate the last 6 months dynamically up to the current month
            for (int i = 5; i >= 0; i--)
            {
                var dt = DateTime.Today.AddMonths(-i);
                string label = dt.ToString("MMM");
                string key = dt.ToString("yyyy-MM");
                int val = (monthlyCounts != null && monthlyCounts.TryGetValue(key, out var c)) ? c : 0;
                bars.Add((label, val));
            }

            // Determine max value for chart scaling with clean rounding
            int maxVal = bars.Max(b => b.Count);
            if (maxVal <= 0) maxVal = 10;
            else if (maxVal <= 50) maxVal = ((maxVal + 9) / 10) * 10;
            else maxVal = ((maxVal + 49) / 50) * 50;

            YAxisMax = maxVal;
            YAxisMidHigh = (int)(maxVal * 0.75);
            YAxisMidLow = (int)(maxVal * 0.50);
            YAxisMin = 0;

            var firstMonth = DateTime.Today.AddMonths(-5);
            ChartSubtitle = $"New solo parents registered — {firstMonth:MMM yyyy} to {DateTime.Today:MMM yyyy}";

            MonthlyBars.Clear();
            foreach (var b in bars)
            {
                MonthlyBars.Add(new MonthBar { Month = b.Label, Value = b.Count, MaxValue = maxVal });
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
