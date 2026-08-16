using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using SOLUM_UI.Services;

namespace SOLUM_UI
{
    public partial class MainWindow : Window
    {
        private Button _activeButton;

        public static readonly DependencyProperty IsNavActiveProperty =
            DependencyProperty.RegisterAttached("IsNavActive", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public static void SetIsNavActive(DependencyObject obj, bool value) => obj.SetValue(IsNavActiveProperty, value);
        public static bool GetIsNavActive(DependencyObject obj) => (bool)obj.GetValue(IsNavActiveProperty);

        public static string CurrentUserName { get; set; } = "Admin User";
        public static string CurrentUserRole { get; set; } = "Administrator";
        public static string CurrentUserBarangay { get; set; } = string.Empty;

        private Dictionary<string, Button> _navButtons = new Dictionary<string, Button>();

        public MainWindow()
        {
            InitializeComponent();
            RegisterNavButtons();
            MainFrame.Navigated += MainFrame_Navigated;
            Loaded += (s, e) =>
            {
                TopBarControl.SetUser(CurrentUserName, CurrentUserRole);
                ApplyRoleNavVisibility();
                ActivateButton("Dashboard");
                NavigatePage(new DashboardPage());
            };
        }

        private void ApplyRoleNavVisibility()
        {
            bool isAdmin = string.Equals(CurrentUserRole, "Administrator", StringComparison.OrdinalIgnoreCase);
            if (_navButtons.ContainsKey("UserAdministration"))
                _navButtons["UserAdministration"].Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (_navButtons.ContainsKey("SubsidyRecommendation"))
                _navButtons["SubsidyRecommendation"].Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (_navButtons.ContainsKey("Analytics"))
                _navButtons["Analytics"].Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            while (MainFrame.CanGoBack)
                MainFrame.RemoveBackEntry();
            while (MainFrame.CanGoForward)
                MainFrame.RemoveBackEntry();
        }

        private void NavigatePage(Page page)
        {
            if (MainFrame.Content is Page current)
            {
                if (current.GetType() == page.GetType() && !(page is SoloParentRecordsPage))
                    return;
            }
            MainFrame.Navigate(page);
        }

        private void RegisterNavButtons()
        {
            foreach (Button btn in FindAllButtons(this))
            {
                if (btn.Tag != null)
                {
                    string tag = btn.Tag.ToString();
                    if (!_navButtons.ContainsKey(tag))
                        _navButtons.Add(tag, btn);
                }
            }
        }

        private List<Button> FindAllButtons(DependencyObject parent)
        {
            List<Button> result = new List<Button>();
            if (parent == null) return result;
            foreach (object child in LogicalTreeHelper.GetChildren(parent))
            {
                DependencyObject dep = child as DependencyObject;
                if (dep == null) continue;
                Button btn = dep as Button;
                if (btn != null) result.Add(btn);
                result.AddRange(FindAllButtons(dep));
            }
            return result;
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;
            string tag = btn.Tag != null ? btn.Tag.ToString() : string.Empty;
            ActivateButton(tag);
            NavigateTo(tag);
        }

        private void ActivateButton(string tag)
        {
            foreach (Button b in _navButtons.Values)
                SetIsNavActive(b, false);

            if (_navButtons.ContainsKey(tag))
            {
                SetIsNavActive(_navButtons[tag], true);
                _activeButton = _navButtons[tag];
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void NavigateTo(string tag)
        {
            switch (tag)
            {
                case "SoloParentRecords":
                    TopBarControl.PageTitle = "Solo Parent Records";
                    break;
                case "UserAdministration":
                    TopBarControl.PageTitle = "User Administration";
                    break;
                case "Analytics":
                    TopBarControl.PageTitle = "Analytics";
                    break;
                case "SubsidyRecommendation":
                    TopBarControl.PageTitle = "Subsidy Recommendation";
                    break;
                case "UserProfile":
                    TopBarControl.PageTitle = "User Profile";
                    break;
                default:
                    TopBarControl.PageTitle = tag;
                    break;
            }

            switch (tag)
            {
                case "Dashboard":
                    NavigatePage(new DashboardPage());
                    break;
                case "SoloParentRecords":
                    SoloParentRecordsPage.CurrentUserName     = CurrentUserName;
                    SoloParentRecordsPage.CurrentUserRole     = CurrentUserRole;
                    SoloParentRecordsPage.CurrentUserBarangay = CurrentUserBarangay;
                    NavigatePage(new SoloParentRecordsPage());
                    break;
                case "UserAdministration":
                    NavigatePage(new UserAdministrationPage());
                    break;
                case "Analytics":
                    NavigatePage(new AnalyticsPage());
                    break;
                case "SubsidyRecommendation":
                    NavigatePage(new SubsidyRecommendationPage());
                    break;
                case "UserProfile":
                    NavigatePage(new UserProfilePage());
                    break;
                case "Settings":
                    break;
                case "Logout":
                    HandleLogout();
                    break;
            }
        }

        private void TopBarControl_ProfileClicked(object sender, RoutedEventArgs e)
        {
            ActivateButton("UserProfile");
            NavigateTo("UserProfile");
        }

        private void TopBarControl_ChangePasswordRequested(object sender, RoutedEventArgs e)
        {
            ActivateButton("UserProfile");
            TopBarControl.PageTitle = "User Profile";
            var page = new UserProfilePage();
            page.ScrollToChangePassword();
            NavigatePage(page);
        }

        private void TopBarControl_LogoutRequested(object sender, RoutedEventArgs e)
        {
            HandleLogout();
        }

        private void HandleLogout()
        {
            LogoutOverlay.Visibility = Visibility.Visible;
        }

        private void LogoutCancel_Click(object sender, RoutedEventArgs e)
        {
            LogoutOverlay.Visibility = Visibility.Collapsed;
            ActivateButton(_activeButton?.Tag?.ToString() ?? "Dashboard");
        }

        private async void LogoutConfirm_Click(object sender, RoutedEventArgs e)
        {
            LogoutOverlay.Visibility = Visibility.Collapsed;
            LogoutLoadingOverlay.Visibility = Visibility.Visible;

            await System.Threading.Tasks.Task.Delay(1200);

            var login = new LoginPage();
            login.Show();
            AuditLogService.Instance.LogLogout(CurrentUserName, CurrentUserRole);
            ToastNotification.Show("Logged Out", "You have been logged out.", ToastType.Info);
            this.Close();
        }

        public void NavigateToRecord(string spId)
        {
            ActivateButton("SoloParentRecords");
            SoloParentRecordsPage.CurrentUserName     = CurrentUserName;
            SoloParentRecordsPage.CurrentUserRole     = CurrentUserRole;
            SoloParentRecordsPage.CurrentUserBarangay = CurrentUserBarangay;
            TopBarControl.PageTitle = "Solo Parent Records";
            var page = new SoloParentRecordsPage();
            page.HighlightRecord(spId);
            NavigatePage(page);
        }
    }
}
