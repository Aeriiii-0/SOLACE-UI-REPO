using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services;
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
        // --- chart data source (still hardcoded; swap with MonthlyAnalyticsDto later) ---
        private static readonly int[] _monthlyValues = { 330, 295, 245, 325, 295, 235, 330 };
        private static readonly string[] _monthLabels = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul" };

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
        public ObservableCollection<AuditLog> RecentLogs { get; } = new ObservableCollection<AuditLog>();

        public DashboardViewModel()
        {
            LoadChart();
            LoadRecentLogs();
            AuditLogService.Instance.Logs.CollectionChanged += OnLogsChanged;
        }

        /// <summary>Fetches the dashboard stat cards from the API.</summary>
        public async Task LoadAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            ErrorMessage = null;

            try
            {
                var response = await AnalyticsApiService.Instance.GetDashboardAnalyticsAsync();

                if (response == null || !response.Succeeded || response.Data == null)
                {
                    ErrorMessage = response?.Errors != null && response.Errors.Count > 0
                        ? string.Join(Environment.NewLine, response.Errors)
                        : "Failed to load dashboard data.";
                    return;
                }

                Apply(response.Data);
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
            ActiveSoloParents = d.ActiveRecords;
            RenewalsDueThisMonth = d.RenewalsDueThisMonth;

            RegisteredDelta = $"↑ {d.RegisteredThisMonth:N0} this month";

            IsActiveChangeNegative = d.ActiveChangeFromLastMonth < 0;
            ActiveDelta = d.ActiveChangeFromLastMonth > 0 ? $"↑ {d.ActiveChangeFromLastMonth:N0} from last month"
                        : d.ActiveChangeFromLastMonth < 0 ? $"↓ {Math.Abs(d.ActiveChangeFromLastMonth):N0} from last month"
                        : "No change from last month";

            RenewalsDelta = $"↑ {d.RenewalsCompletedToday:N0} completed today";
        }

        private void OnLogsChanged(object sender, NotifyCollectionChangedEventArgs e)
            => LoadRecentLogs();

        private void LoadChart()
        {
            int max = _monthlyValues.Max();
            MonthlyBars.Clear();
            for (int i = 0; i < _monthLabels.Length; i++)
                MonthlyBars.Add(new MonthBar { Month = _monthLabels[i], Value = _monthlyValues[i], MaxValue = max });
        }

        /// <summary>Pulls the 5 most recent logs from the singleton service.</summary>
        private void LoadRecentLogs()
        {
            RecentLogs.Clear();
            foreach (var log in AuditLogService.Instance.GetRecent(5))
                RecentLogs.Add(log);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}