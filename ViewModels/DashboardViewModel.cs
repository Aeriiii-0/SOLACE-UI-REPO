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
        public string Month { get; set; }
        public int Value { get; set; }
        public int MaxValue { get; set; }

        
        public double BarHeight
        {
            get
            {
                if (MaxValue > 0)
                    return (Value / (double)MaxValue) * 200.0;
                return 0;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        //  Stat cards 
        public int RegisteredSoloParents { get { return 1348; } }
        public int ActiveSoloParents { get { return 847; } }
        public int RenewalsDueThisMonth { get { return 112; } }

        public string RegisteredDelta { get { return "↑ 28 this month"; } }
        public string ActiveDelta { get { return "↑ 72 from last month"; } }
        public string RenewalsDelta { get { return "↑ 3 Completed today"; } }

        //  bar chart data 
        public ObservableCollection<MonthBar> MonthlyBars { get; }
            = new ObservableCollection<MonthBar>();

        // logs (5 pre-showed)
        public ObservableCollection<AuditLog> RecentLogs { get; }
            = new ObservableCollection<AuditLog>();

        public DashboardViewModel()
        {
            LoadChart();
            LoadRecentLogs();

            // re-syncing
            AuditLogService.Instance.Logs.CollectionChanged += OnLogsChanged;
        }

        private void OnLogsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            LoadRecentLogs();
        }

        private void LoadChart()
        {
            string[] months = new string[] { "Jan", "Feb", "March", "April", "May", "June", "July" };
            int[] values = new int[] { 330, 295, 245, 325, 295, 235, 330 };

            int max = values.Max();

            MonthlyBars.Clear();
            for (int i = 0; i < months.Length; i++)
            {
                MonthlyBars.Add(new MonthBar
                {
                    Month = months[i],
                    Value = values[i],
                    MaxValue = max
                });
            }
        }

        private void LoadRecentLogs()
        {
            RecentLogs.Clear();
            foreach (AuditLog log in AuditLogService.Instance.GetRecent(5))
                RecentLogs.Add(log);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }
    }
}
