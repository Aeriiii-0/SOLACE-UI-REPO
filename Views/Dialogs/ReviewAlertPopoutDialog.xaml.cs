using System;
using System.Windows;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class ReviewAlertPopoutDialog : Window
    {
        private MainWindow _mainWindow;

        public ReviewAlertPopoutDialog(int pendingCount = 0, MainWindow mainWindow = null)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            TxtPendingCount.Text = pendingCount.ToString("N0");
        }

        // Backwards compatibility constructor when callers pass (expiring, inactive, pending, mainWin)
        public ReviewAlertPopoutDialog(int expiringCount, int inactiveCount, int pendingCount = 0, MainWindow mainWindow = null)
            : this(pendingCount > 0 ? pendingCount : (expiringCount > 0 ? expiringCount : inactiveCount), mainWindow)
        {
        }

        private MainWindow GetMainWindow()
        {
            if (_mainWindow != null) return _mainWindow;
            if (Owner is MainWindow mw) return mw;
            if (Application.Current?.MainWindow is MainWindow appMw) return appMw;
            if (Application.Current != null)
            {
                foreach (Window win in Application.Current.Windows)
                {
                    if (win is MainWindow m)
                    {
                        _mainWindow = m;
                        return m;
                    }
                }
            }
            return null;
        }

        private void ReviewSubsidy_Click(object sender, RoutedEventArgs e)
        {
            var main = GetMainWindow();
            Close();
            if (main != null)
            {
                main.ActivateButton("SubsidyRecommendation");
                main.NavigateTo("SubsidyRecommendation");
            }
        }

        private void Dismiss_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static void ShowIfReviewsNeeded(Window owner, int pendingCount)
        {
            if (pendingCount <= 0) return;
            var mainWin = owner as MainWindow;
            var dlg = new ReviewAlertPopoutDialog(pendingCount, mainWin)
            {
                Owner = owner
            };
            dlg.ShowDialog();
        }

        public static void ShowIfReviewsNeeded(Window owner, int expiringCount, int inactiveCount, int pendingCount = 0)
        {
            int count = pendingCount > 0 ? pendingCount : (expiringCount > 0 ? expiringCount : inactiveCount);
            if (count <= 0) return;
            ShowIfReviewsNeeded(owner, count);
        }
    }
}
