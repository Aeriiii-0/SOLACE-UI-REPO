using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Views.Dialogs;
using SOLUM_UI.Views.Pages;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI
{
    public partial class MainWindow : Window
    {
        private Button _activeButton;

        public static readonly DependencyProperty IsNavActiveProperty =
            DependencyProperty.RegisterAttached("IsNavActive", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public static void SetIsNavActive(DependencyObject obj, bool value) => obj.SetValue(IsNavActiveProperty, value);
        public static bool GetIsNavActive(DependencyObject obj) => (bool)obj.GetValue(IsNavActiveProperty);

        public static string CurrentUserName { get; set; } = "ADMIN USER";
        public static string CurrentUserId { get; set; } = string.Empty;
        public static string CurrentUserRole { get; set; } = "ADMINISTRATOR";
        public static string CurrentUserBarangay { get; set; } = string.Empty;

        public static string CurrentUserEmail { get; set; } = string.Empty;

        public static string FirstName { get; set; } = string.Empty;

        public static string LastName { get; set; } = string.Empty;

        public static string ContactNumber { get; set; } = string.Empty;

        public static string FullName => $"{FirstName} {LastName}".Trim().ToUpper();

        public static void SetCurrentUser(ApplicationUserDTO user)
        {
            if (user != null)
            {
                CurrentUserId = user.Id != Guid.Empty ? user.Id.ToString() : (AuthApiService.Instance.CurrentUserId ?? string.Empty);
                FirstName = user.FirstName?.ToUpper() ?? string.Empty;
                LastName = user.LastName?.ToUpper() ?? string.Empty;
                CurrentUserName = string.IsNullOrWhiteSpace(FullName) ? user.Email : FullName.ToUpper();
                // Barangay is set elsewhere (e.g., during login or profile setup) - default to "ALL" for safety
                if (string.IsNullOrWhiteSpace(CurrentUserBarangay))
                {
                    CurrentUserBarangay = "ALL";
                }
                else
                {
                    CurrentUserBarangay = CurrentUserBarangay.ToUpper();
                }
                CurrentUserEmail = user.Email;
                if (AuthApiService.Instance.IsSuperAdmin)
                {
                    CurrentUserRole = "SUPERADMIN";
                }
                else if (AuthApiService.Instance.IsAdmin)
                {
                    CurrentUserRole = "ADMINISTRATOR";
                }
                else
                {
                    CurrentUserRole = (!string.IsNullOrWhiteSpace(user.Role) ? user.Role : (CurrentUserRole ?? "ENCODER")).ToUpper();
                }
                ContactNumber = user.ContactNumber ?? string.Empty;
            }
            else
            {
                CurrentUserId = AuthApiService.Instance.CurrentUserId ?? string.Empty;
            }
        }
        /// <summary>
        /// Gets the current user's barangay with null safety. 
        /// Returns the barangay if set, otherwise returns "ALL" as safe default.
        /// </summary>
        public static string GetSafeBarangay()
        {
            return !string.IsNullOrWhiteSpace(CurrentUserBarangay) ? CurrentUserBarangay : "ALL";
        }

        private Dictionary<string, Button> _navButtons = new Dictionary<string, Button>();
        private Dictionary<string, Page> _pageCache = new Dictionary<string, Page>(); // NEW â€” one instance per page tag

        public MainWindow(ApplicationUserDTO user)
        {
            InitializeComponent();
            RegisterNavButtons();
            MainFrame.Navigated += MainFrame_Navigated;
            AuthApiService.Instance.OnSessionExpired += HandleSessionExpired;
            Closed += (s, e) => AuthApiService.Instance.OnSessionExpired -= HandleSessionExpired;
            Closing += MainWindow_Closing;
            Loaded += async (s, e) =>
            {
                TopBarControl.SetUser(CurrentUserName, CurrentUserRole);
                ApplyRoleNavVisibility();
                ActivateButton("Dashboard");
                NavigatePage(GetOrCreatePage("Dashboard", () => new DashboardPage())); // CHANGED
                // await CheckAdminReviewsOnStartupAsync(); // Disabled: review prompt on login
            };
            SetCurrentUser(user);
        }
        // NEW â€” returns the cached page instance for this tag, creating it once on first use
        private Page GetOrCreatePage(string tag, Func<Page> factory)
        {
            if (!_pageCache.TryGetValue(tag, out var page))
            {
                page = factory();
                _pageCache[tag] = page;
            }
            return page;
        }

        private async Task CheckAdminReviewsOnStartupAsync()
        {
            try
            {
                bool isAdmin = string.Equals(CurrentUserRole, "Administrator", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(CurrentUserRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(CurrentUserRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                               AuthApiService.Instance.IsAdmin || AuthApiService.Instance.IsSuperAdmin;
                if (!isAdmin) return;
                // Brief delay so main dashboard completes initial render
                await Task.Delay(800);
                var dashRes = await AnalyticsApiService.Instance.GetDashboardAnalyticsAsync();
                if (dashRes != null && dashRes.Succeeded && dashRes.Data != null)
                {
                    int expiring = dashRes.Data.RenewalsDueThisMonth;
                    int inactive = dashRes.Data.InactiveRecords;
                    int pending = dashRes.Data.PendingReviewCount;
                    if (expiring > 0 || inactive > 0 || pending > 0)
                    {
                        ReviewAlertPopoutDialog.ShowIfReviewsNeeded(this, expiring, inactive, pending);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] CheckAdminReviewsOnStartup error: {ex.Message}");
            }
        }

        public void ClearPageCache(string tag)
        {
            if (_pageCache.ContainsKey(tag)) _pageCache.Remove(tag);
        }

        private void ApplyRoleNavVisibility()
        {
            bool isAdmin = string.Equals(CurrentUserRole, "Administrator", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(CurrentUserRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(CurrentUserRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                           SOLUM_UI.Services.Api.AuthApiService.Instance.IsAdmin;
            if (_navButtons.ContainsKey("UserAdministration"))
                _navButtons["UserAdministration"].Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            if (_navButtons.ContainsKey("SubsidyRecommendation"))
                _navButtons["SubsidyRecommendation"].Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            while (MainFrame.CanGoBack)
                MainFrame.RemoveBackEntry();
            while (MainFrame.CanGoForward)
                MainFrame.RemoveBackEntry();
        }
        // CHANGED â€” simplified now that caching handles "don't rebuild the same page" for us.
        // This just avoids re-navigating the Frame to the exact same instance it's already showing.
        private void NavigatePage(Page page)
        {
            if (MainFrame.Content == page)
                return;
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

        public void ActivateButton(string tag)
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

        private bool _isExiting = false;

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isExiting) return;
            e.Cancel = true;
            await PromptExitAndLogoutAsync();
        }

        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            await PromptExitAndLogoutAsync();
        }

        private async Task PromptExitAndLogoutAsync()
        {
            if (_isExiting) return;
            const string logoutIcon = "M17,7L15.59,8.41L18.17,11H8V13H18.17L15.59,15.58L17,17L22,12M4,5H12V3H4A2,2 0 0,0 2,5V19A2,2 0 0,0 4,21H12V19H4V5Z";

            bool isConfirmed = ActionConfirmDialog.Show(
                title: "Exit Application",
                message: "Are you sure you want to sign out and exit the application?",
                confirmText: "Exit & Sign Out",
                cancelText: "Cancel",
                theme: ConfirmThemeType.Primary,
                owner: this,
                customIconData: logoutIcon);
            if (!isConfirmed) return;
            _isExiting = true;
            if (LogoutLoadingOverlay != null)
            {
                LogoutLoadingOverlay.Visibility = Visibility.Visible;
            }
            try
            {
                await AuthApiService.Instance.LogoutAsync();
            }
            catch { }
            try
            {
                AuditLogService.Instance.LogLogout(CurrentUserName, CurrentUserRole);
            }
            catch { }
            AuthApiService.Instance.OnSessionExpired -= HandleSessionExpired;
            Application.Current.Shutdown();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        public void NavigateTo(string tag)
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
                    NavigatePage(GetOrCreatePage("Dashboard", () => new DashboardPage())); // CHANGED
                    break;
                case "SoloParentRecords":
                    SoloParentRecordsPage.CurrentUserName = CurrentUserName;
                    SoloParentRecordsPage.CurrentUserRole = CurrentUserRole;
                    SoloParentRecordsPage.CurrentUserBarangay = CurrentUserBarangay;
                    NavigatePage(GetOrCreatePage("SoloParentRecords", () => new SoloParentRecordsPage())); // CHANGED
                    break;
                case "UserAdministration":
                    NavigatePage(GetOrCreatePage("UserAdministration", () => new UserAdministrationPage())); // CHANGED
                    break;
                case "Analytics":
                    NavigatePage(GetOrCreatePage("Analytics", () => new AnalyticsPage())); // CHANGED
                    break;
                case "SubsidyRecommendation":
                    bool isSubsidyAdmin = string.Equals(CurrentUserRole, "Administrator", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(CurrentUserRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(CurrentUserRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                                         SOLUM_UI.Services.Api.AuthApiService.Instance.IsAdmin;
                    if (!isSubsidyAdmin)
                    {
                        ToastNotification.Show("Access Denied", "Only administrators can access Subsidy Recommendations.", ToastType.Error);
                        ActivateButton("Dashboard");
                        NavigateTo("Dashboard");
                        return;
                    }
                    var subPage = (SubsidyRecommendationPage)GetOrCreatePage("SubsidyRecommendation", () => new SubsidyRecommendationPage());
                    if (subPage.DataContext is SubsidyRecommendationViewModel subVm)
                    {
                        if (!subVm.IsDataFresh)
                        {
                            _ = subVm.RefreshDataAsync();
                        }
                    }
                    NavigatePage(subPage);
                    break;
                case "UserProfile":
                    NavigatePage(GetOrCreatePage("UserProfile", () => new UserProfilePage())); // CHANGED
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
            var page = (UserProfilePage)GetOrCreatePage("UserProfile", () => new UserProfilePage());
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

        private bool _isSessionExpiredRedirecting = false;

        private void HandleSessionExpired(string message)
        {
            Dispatcher.Invoke(() =>
            {
                if (_isSessionExpiredRedirecting) return;
                _isSessionExpiredRedirecting = true;
                AuthApiService.Instance.OnSessionExpired -= HandleSessionExpired;
                var login = new LoginPage(message);
                login.Show();
                _isExiting = true;
                this.Close();
            });
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
            try
            {
                await AuthApiService.Instance.LogoutAsync();
            }
            catch { }
            AuthApiService.Instance.OnSessionExpired -= HandleSessionExpired;
            var login = new LoginPage();
            login.Show();
            AuditLogService.Instance.LogLogout(CurrentUserName, CurrentUserRole);
            ToastNotification.Show("Logged Out", "You have been logged out.", ToastType.Info);
            _isExiting = true;
            this.Close();
        }

        public void NavigateToRecord(string spId)
        {
            ActivateButton("SoloParentRecords");
            SoloParentRecordsPage.CurrentUserName = CurrentUserName;
            SoloParentRecordsPage.CurrentUserRole = CurrentUserRole;
            SoloParentRecordsPage.CurrentUserBarangay = CurrentUserBarangay;
            TopBarControl.PageTitle = "Solo Parent Records";
            var page = (SoloParentRecordsPage)GetOrCreatePage("SoloParentRecords", () => new SoloParentRecordsPage()); // CHANGED
            page.HighlightRecord(spId);
            NavigatePage(page);
        }

        public void NavigateToSoloParentRecordsWithFilter(string statusFilter)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"NavigateToSoloParentRecordsWithFilter START - statusFilter: {statusFilter}");
                ActivateButton("SoloParentRecords");
                SoloParentRecordsPage.CurrentUserName = CurrentUserName;
                SoloParentRecordsPage.CurrentUserRole = CurrentUserRole;
                SoloParentRecordsPage.CurrentUserBarangay = CurrentUserBarangay;
                TopBarControl.PageTitle = "Solo Parent Records";
                // Set the filter FIRST
                SoloParentRecordsPage.RequestedStatusFilter = statusFilter;
                System.Diagnostics.Debug.WriteLine($"Set RequestedStatusFilter to: {statusFilter}");
                ClearPageCache("SoloParentRecords"); // Clear cache to ensure fresh page with new filter
                System.Diagnostics.Debug.WriteLine("Cleared page cache");
                var page = new SoloParentRecordsPage();
                System.Diagnostics.Debug.WriteLine($"Created new SoloParentRecordsPage instance. RequestedStatusFilter now: {SoloParentRecordsPage.RequestedStatusFilter}");
                NavigatePage(page);
                System.Diagnostics.Debug.WriteLine("NavigatePage called successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NavigateToSoloParentRecordsWithFilter ERROR: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}

