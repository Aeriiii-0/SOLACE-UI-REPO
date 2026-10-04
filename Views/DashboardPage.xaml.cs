using System.Windows;
using System.Windows.Controls;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI
{
    public partial class DashboardPage : Page
    {
        // VM lives for the lifetime of the page so the cache persists between opens.
        private readonly AuditLogViewModel _logsVm = new AuditLogViewModel();

        public DashboardPage()
        {
            InitializeComponent();

            if (!(DataContext is DashboardViewModel))
                DataContext = new DashboardViewModel();

            // Assign the log VM to the overlay immediately so it is ready.
            LogsOverlay.DataContext = _logsVm;

            Loaded += async (s, e) =>
            {
                // Load dashboard data
                if (DataContext is DashboardViewModel vm)
                    await vm.LoadAsync();

                // Pre-fetch first page of audit logs in the background
                if (!_logsVm.IsInitialized)
                    await _logsVm.LoadAsync();
            };
        }

        private void ViewAllLogs_Click(object sender, RoutedEventArgs e)
        {
            // VM is already loaded — just reveal the overlay.
            DashboardScroll.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 6 };
            OverlayRoot.Visibility = Visibility.Visible;
        }

        private void LogsOverlay_CloseRequested(object sender, RoutedEventArgs e)
        {
            OverlayRoot.Visibility = Visibility.Collapsed;
            DashboardScroll.Effect = null;
        }
    }
}
