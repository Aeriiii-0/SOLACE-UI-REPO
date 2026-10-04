using System.Windows;
using System.Windows.Controls;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI
{
    public partial class ViewAllLogsOverlay : UserControl
    {
        public static readonly RoutedEvent CloseRequestedEvent =
            EventManager.RegisterRoutedEvent(
                "CloseRequested",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ViewAllLogsOverlay));

        public event RoutedEventHandler CloseRequested
        {
            add    => AddHandler(CloseRequestedEvent, value);
            remove => RemoveHandler(CloseRequestedEvent, value);
        }

        public ViewAllLogsOverlay()
        {
            InitializeComponent();

            // Only load if the DashboardPage hasn't already pre-fetched.
            Loaded += async (s, e) =>
            {
                if (DataContext is AuditLogViewModel vm && !vm.IsInitialized)
                    await vm.LoadAsync();
            };
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CloseRequestedEvent));
        }
    }
}
