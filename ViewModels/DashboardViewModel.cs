using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using SOLUM_UI.Models;
using SOLUM_UI.Services;

namespace SOLUM_UI.ViewModels
{
    public class MonthBar : INotifyPropertyChanged
    {
        public string Month    { get; set; }
        public int    Value    { get; set; }
        public int    MaxValue { get; set; }

        /// <summary>Pixel height relative to max, capped at 200px.</summary>
        public double BarHeight => MaxValue > 0 ? (Value / (double)MaxValue) * 200.0 : 0;

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        // --- stat data source (swap with API response when ready) ---
        private static readonly int[] _monthlyValues = { 330, 295, 245, 325, 295, 235, 330 };
        private static readonly string[] _monthLabels = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul" };

        private const int TotalRegistered     = 1348;
        private const int TotalActive         = 847;
        private const int TotalRenewalsThisMo = 112;

        public int    RegisteredSoloParents  => TotalRegistered;
        public int    ActiveSoloParents      => TotalActive;
        public int    RenewalsDueThisMonth   => TotalRenewalsThisMo;

        public string RegisteredDelta => "↑ " + _monthlyValues[_monthlyValues.Length - 1] + " this month";
        public string ActiveDelta     => "↑ " + (TotalActive - _monthlyValues[_monthlyValues.Length - 2]) + " from last month";
        public string RenewalsDelta   => "↑ 3 completed today";

        public ObservableCollection<MonthBar>  MonthlyBars { get; } = new ObservableCollection<MonthBar>();
        public ObservableCollection<AuditLog>  RecentLogs  { get; } = new ObservableCollection<AuditLog>();

        public DashboardViewModel()
        {
            LoadChart();
            LoadRecentLogs();
            AuditLogService.Instance.Logs.CollectionChanged += OnLogsChanged;
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
