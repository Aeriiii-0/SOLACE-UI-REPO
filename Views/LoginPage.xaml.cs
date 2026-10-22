using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Views.Dialogs;

namespace SOLUM_UI
{
    public partial class LoginPage : Window
    {
        private bool _isPasswordVisible = false;

        public LoginPage()
        {
            InitializeComponent();
        }

        public LoginPage(string sessionNotice) : this()
        {
            if (!string.IsNullOrWhiteSpace(sessionNotice))
            {
                Loaded += (s, e) => ShowNotice(sessionNotice, "Session Expired");
            }
        }

        private static readonly SolidColorBrush NormalBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC"));
        private static readonly SolidColorBrush ErrorBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545"));

        private void TxtEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderEmail.Visibility = string.IsNullOrEmpty(TxtEmail.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
            ClearEmailError();
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PlaceholderPassword.Visibility = string.IsNullOrEmpty(TxtPassword.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
            ClearPasswordError();
        }

        private void TxtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderPassword.Visibility = string.IsNullOrEmpty(TxtPasswordVisible.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
            ClearPasswordError();
        }

        private void ClearEmailError()
        {
            if (ErrEmail != null)
            {
                ErrEmail.Text = string.Empty;
                ErrEmail.Visibility = Visibility.Collapsed;
            }
            if (BorderEmail != null)
            {
                BorderEmail.BorderBrush = NormalBorderBrush;
                BorderEmail.BorderThickness = new Thickness(0, 0, 0, 1);
            }
        }

        private void SetEmailError(string message)
        {
            if (ErrEmail != null)
            {
                ErrEmail.Text = message;
                ErrEmail.Visibility = Visibility.Visible;
            }
            if (BorderEmail != null)
            {
                BorderEmail.BorderBrush = ErrorBorderBrush;
                BorderEmail.BorderThickness = new Thickness(0, 0, 0, 1.5);
            }
        }

        private void ClearPasswordError()
        {
            if (ErrPassword != null)
            {
                ErrPassword.Text = string.Empty;
                ErrPassword.Visibility = Visibility.Collapsed;
            }
            if (BorderPassword != null)
            {
                BorderPassword.BorderBrush = NormalBorderBrush;
                BorderPassword.BorderThickness = new Thickness(0, 0, 0, 1);
            }
        }

        private void SetPasswordError(string message)
        {
            if (ErrPassword != null)
            {
                ErrPassword.Text = message;
                ErrPassword.Visibility = Visibility.Visible;
            }
            if (BorderPassword != null)
            {
                BorderPassword.BorderBrush = ErrorBorderBrush;
                BorderPassword.BorderThickness = new Thickness(0, 0, 0, 1.5);
            }
        }

        private bool _isLoggingIn = false;   // NEW

        private void BtnLogin_Click(object sender, RoutedEventArgs e) => AttemptLogin();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !_isLoggingIn)   // CHANGED — was unguarded
                AttemptLogin();
        }

        private async void AttemptLogin()
        {
            if (_isLoggingIn) return;   // NEW — guards any other re-entry path too

            ClearEmailError();
            ClearPasswordError();

            string email = TxtEmail.Text?.Trim() ?? string.Empty;
            string password = _isPasswordVisible
                ? TxtPasswordVisible.Text
                : TxtPassword.Password;

            bool hasValidationError = false;
            Control controlToFocus = null;

            // 1. Email field validation
            if (string.IsNullOrWhiteSpace(email))
            {
                SetEmailError("Please enter your email address.");
                hasValidationError = true;
                if (controlToFocus == null) controlToFocus = TxtEmail;
            }
            else if (email.Contains(" "))
            {
                SetEmailError("Email address cannot contain spaces.");
                hasValidationError = true;
                if (controlToFocus == null) controlToFocus = TxtEmail;
            }
            else if (!email.Contains("@"))
            {
                SetEmailError("Please include an '@' in the email address.");
                hasValidationError = true;
                if (controlToFocus == null) controlToFocus = TxtEmail;
            }
            else if (!Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                SetEmailError("Please enter a valid email address (e.g., name@example.com).");
                hasValidationError = true;
                if (controlToFocus == null) controlToFocus = TxtEmail;
            }

            // 2. Password field validation
            if (string.IsNullOrWhiteSpace(password))
            {
                SetPasswordError("Please enter your password.");
                hasValidationError = true;
                if (controlToFocus == null) controlToFocus = _isPasswordVisible ? (Control)TxtPasswordVisible : TxtPassword;
            }
            else if (password.Length < 6)
            {
                SetPasswordError("Password must be at least 6 characters.");
                hasValidationError = true;
                if (controlToFocus == null) controlToFocus = _isPasswordVisible ? (Control)TxtPasswordVisible : TxtPassword;
            }

            if (hasValidationError)
            {
                controlToFocus?.Focus();
                return;
            }

            _isLoggingIn = true;               // NEW
            BtnLogin.IsEnabled = false;
            LoadingOverlay.Visibility = Visibility.Visible;
            
            // Show splash screen
            SplashScreenWindow splash = null;
            
            // Start progress animation
            var progressPercentage = FindName("ProgressPercentage") as TextBlock;
            var progressBarFill = FindName("ProgressBarFill") as Border;
            
            var progressTimer = new System.Timers.Timer(100);
            int progress = 0;
            
            try
            {
                splash = new SplashScreenWindow();
                splash.Show();

                if (progressPercentage != null && progressBarFill != null)
                {
                    progressTimer.Elapsed += (s, e) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            progress = Math.Min(progress + 5, 95);  // Cap at 95% until complete
                            progressPercentage.Text = $"{progress}%";
                            progressBarFill.Width = (progress / 100.0) * 240;
                        });
                    };
                    progressTimer.Start();
                }

                var loginResponse = await AuthApiService.Instance.LoginAsync(email, password);

                if (!loginResponse.Succeeded || loginResponse.Data == null || string.IsNullOrWhiteSpace(loginResponse.Data.Token))
                {
                    splash?.Close();
                    splash = null;

                    string rawErr = loginResponse.Errors != null && loginResponse.Errors.Count > 0
                        ? string.Join(" ", loginResponse.Errors)
                        : "Invalid credentials";
                    string friendlyErr = FormatHumanReadableAuthError(rawErr);
                    ShowError(friendlyErr);
                    return;
                }

                MainWindow.CurrentUserName = !string.IsNullOrWhiteSpace(loginResponse.Data.Email)
                    ? loginResponse.Data.Email
                    : email;

                Guid id = AuthApiService.ExtractUserIdFromToken(loginResponse.Data.Token) ?? Guid.Empty;
                string assignedRole = AuthApiService.Instance.IsSuperAdmin ? "SuperAdmin" : (AuthApiService.Instance.IsAdmin ? "Administrator" : "Encoder");
                MainWindow.CurrentUserRole = assignedRole;

                ApplicationUserDTO loggedInUser = null;
                // Only Admins have permission to query /api/user. Encoders manage solo parents only.
                if (AuthApiService.Instance.IsAdmin)
                {
                    try
                    {
                        var userDTO = await UserApiService.Instance.GetApplicationUsersAsync(
                            new GetApplicationUserRequest { Id = id, Role = string.Empty, Page = 1, PageSize = 1 }
                        );
                        loggedInUser = userDTO?.Data?.Items?.FirstOrDefault();
                    }
                    catch { }
                }

                if (loggedInUser == null)
                {
                    loggedInUser = new ApplicationUserDTO
                    {
                        Id = id,
                        Email = MainWindow.CurrentUserName,
                        Role = assignedRole
                    };
                }

                if (string.IsNullOrWhiteSpace(loggedInUser.Role) || AuthApiService.Instance.IsAdmin)
                {
                    loggedInUser.Role = assignedRole;
                }

                // Complete the progress bar
                if (progressPercentage != null && progressBarFill != null)
                {
                    progressPercentage.Text = "100%";
                    progressBarFill.Width = 240;
                }

                var mainWindow = new MainWindow(loggedInUser);
                mainWindow.Show();
                AuditLogService.Instance.LogLogin(MainWindow.CurrentUserName, MainWindow.CurrentUserRole);
                ToastNotification.Show("Welcome", "Logged in as " + MainWindow.CurrentUserName + ".", ToastType.Success);
                
                splash?.Close();
                splash = null;
                this.Close();
            }
            catch (Exception ex)
            {
                splash?.Close();
                splash = null;
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}\n{ex.StackTrace}");
                string friendlyErr = FormatHumanReadableAuthError(ex.Message);
                ShowError(friendlyErr);
            }
            finally
            {
                if (splash != null)
                {
                    try { splash.Close(); } catch { }
                    splash = null;
                }
                progressTimer.Stop();
                progressTimer.Dispose();
                _isLoggingIn = false;          // NEW
                BtnLogin.IsEnabled = true;
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        public void ShowNotice(string message, string title = "Session Expired")
        {
            if (TxtErrorModalTitle != null) TxtErrorModalTitle.Text = title;
            TxtErrorModal.Text = FormatHumanReadableAuthError(message);
            ErrorModalOverlay.Visibility = Visibility.Visible;
        }

        private void ShowError(string message)
        {
            if (TxtErrorModalTitle != null) TxtErrorModalTitle.Text = "Sign In Failed";
            TxtErrorModal.Text = message;
            ErrorModalOverlay.Visibility = Visibility.Visible;
        }

        private void BtnErrorClose_Click(object sender, RoutedEventArgs e)
        {
            ErrorModalOverlay.Visibility = Visibility.Collapsed;
            // Clear only password so the user does not have to retype their email address
            TxtPassword.Password = string.Empty;
            TxtPasswordVisible.Text = string.Empty;
            if (_isPasswordVisible)
            {
                TxtPasswordVisible.Focus();
            }
            else
            {
                TxtPassword.Focus();
            }
        }

        private string FormatHumanReadableAuthError(string rawError)
        {
            if (string.IsNullOrWhiteSpace(rawError))
                return "Unable to sign in. Please verify your credentials and try again.";

            string lower = rawError.ToLowerInvariant();

            // 1. Incorrect credentials / Not found / Unauthorized
            if (lower.Contains("invalid credential") ||
                lower.Contains("invalid email") ||
                lower.Contains("invalid password") ||
                lower.Contains("bad credential") ||
                lower.Contains("incorrect username or password") ||
                lower.Contains("incorrect email or password") ||
                lower.Contains("user not found") ||
                lower.Contains("unauthorized") ||
                lower.Contains("invalid login attempt") ||
                lower.Contains("wrong password"))
            {
                return "The email address or password you entered is incorrect. Please verify your credentials and try again.";
            }

            // 2. Account lockout
            if (lower.Contains("lockout") || lower.Contains("locked out") || lower.Contains("too many failed"))
            {
                return "Your account has been temporarily locked due to multiple failed login attempts. Please contact your system administrator or try again later.";
            }

            // 3. Deactivated / Inactive account
            if (lower.Contains("inactive") || lower.Contains("deactivated") || lower.Contains("disabled") || lower.Contains("not active"))
            {
                return "Your account has been deactivated. Please contact your CSWDO administrator for assistance.";
            }

            // 4. Email confirmation required
            if (lower.Contains("not confirmed") || lower.Contains("unconfirmed") || lower.Contains("email confirmation"))
            {
                return "Your email address has not been confirmed yet. Please verify your account before signing in.";
            }

            // 5. Connection refused / Offline / Network issues
            if (lower.Contains("unable to connect") ||
                lower.Contains("connection refused") ||
                lower.Contains("no connection could be made") ||
                lower.Contains("network") ||
                lower.Contains("socketexception") ||
                lower.Contains("actively refused") ||
                lower.Contains("no such host") ||
                lower.Contains("name resolution") ||
                lower.Contains("remote server"))
            {
                return "Unable to connect to the SOLACE server. Please check your network connection and ensure the server is online.";
            }

            // 6. Request timeout
            if (lower.Contains("timed out") || lower.Contains("timeout") || lower.Contains("taskcanceledexception"))
            {
                return "The server took too long to respond. Please check your network connection and try again.";
            }

            // 7. Server internal error (500, 502, 503)
            if (lower.Contains("500") || lower.Contains("502") || lower.Contains("503") ||
                lower.Contains("internal server error") || lower.Contains("bad gateway") || lower.Contains("service unavailable"))
            {
                return "The server encountered a temporary error. Please try again shortly or contact your system administrator.";
            }

            // 8. Session expired
            if (lower.Contains("session expired") || lower.Contains("session has expired") || lower.Contains("token expired"))
            {
                return "Your session has expired. Please log in again to continue.";
            }

            // 9. Technical stack trace or raw JSON
            if (rawError.Contains("System.") || rawError.Contains("Exception:") || rawError.Contains("{") || rawError.Contains("}"))
            {
                return "An unexpected system error occurred while signing in. Please try again or contact your administrator.";
            }

            // 10. Clean up raw string if it has clean words
            string clean = rawError.Trim().TrimEnd('.', '!', '?') + ".";
            return clean;
        }

        private void LinkTerms_Click(object sender, RoutedEventArgs e)
        {
            TermsOverlay.Visibility = Visibility.Visible;
        }

        private void BtnTermsClose_Click(object sender, RoutedEventArgs e)
        {
            TermsOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                TxtPasswordVisible.Text = TxtPassword.Password;
                TxtPassword.Visibility = Visibility.Collapsed;
                TxtPasswordVisible.Visibility = Visibility.Visible;
                TxtToggleIcon.Text = "\uE7B3";
                TxtPasswordVisible.Focus();
                TxtPasswordVisible.CaretIndex = TxtPasswordVisible.Text.Length;
            }
            else
            {
                TxtPassword.Password = TxtPasswordVisible.Text;
                TxtPasswordVisible.Visibility = Visibility.Collapsed;
                TxtPassword.Visibility = Visibility.Visible;
                TxtToggleIcon.Text = "\uED1A";
                TxtPassword.Focus();
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LinkForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            ActionConfirmDialog.Show(
                title: "Password Recovery",
                message: "For internal security, passwords in SOLACE cannot be recovered online.\n\nPlease contact your System Administrator (CSWDO / IT Head) to issue a temporary password reset.",
                confirmText: "Understood",
                cancelText: "Close",
                theme: ConfirmThemeType.Primary,
                owner: this);
        }
    }
}
