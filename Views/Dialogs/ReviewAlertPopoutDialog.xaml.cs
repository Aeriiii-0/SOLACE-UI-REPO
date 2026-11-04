using System;
using System.Windows;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class ReviewAlertPopoutDialog : Window
    {
        private MainWindow _mainWindow;

        public ReviewAlertPopoutDialog(int expiringCount, int inactiveCount, int pendingCount = 0, MainWindow mainWindow = null)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            TxtExpiringCount.Text = expiringCount.ToString("N0");
            TxtInactiveCount.Text = inactiveCount.ToString("N0");
            TxtPendingCount.Text = pendingCount.ToString("N0");

            if (expiringCount <= 0 && CardExpiring != null)
            {
                CardExpiring.Visibility = Visibility.Collapsed;
            }

            if (inactiveCount <= 0 && CardInactive != null)
            {
                CardInactive.Visibility = Visibility.Collapsed;
            }

            if (pendingCount <= 0 && CardPendingReview != null)
            {
                CardPendingReview.Visibility = Visibility.Collapsed;
            }
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

        private void ReviewExpiring_Click(object sender, RoutedEventArgs e)
        {
            var main = GetMainWindow();
            Close();
            if (main != null)
            {
                main.NavigateToSoloParentRecordsWithFilter("ExpiringSoon");
            }
        }

        private void ReviewInactive_Click(object sender, RoutedEventArgs e)
        {
            var main = GetMainWindow();
            Close();
            if (main != null)
            {
                main.NavigateToSoloParentRecordsWithFilter("Expired");
            }
        }

        private void Dismiss_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static void ShowIfReviewsNeeded(Window owner, int expiringCount, int inactiveCount, int pendingCount = 0)
        {
            if (expiringCount <= 0 && inactiveCount <= 0 && pendingCount <= 0) return;

            var mainWin = owner as MainWindow;
            var dlg = new ReviewAlertPopoutDialog(expiringCount, inactiveCount, pendingCount, mainWin)
            {
                Owner = owner
            };
            dlg.ShowDialog();
        }
    }
}
