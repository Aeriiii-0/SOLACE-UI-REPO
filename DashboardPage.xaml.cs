using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SOLUM_UI.Services;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
        }

        private void ViewAllLogs_Click(object sender, RoutedEventArgs e)
        {
            var allLogs = AuditLogService.Instance.Logs.ToList();
            var vm = new AuditLogViewModel(allLogs);
            LogsOverlay.DataContext = vm;

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
