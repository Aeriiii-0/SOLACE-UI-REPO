using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;

namespace SOLUM_UI
{
    public partial class LoginPage : Window
    {
        private bool _isPasswordVisible = false;

        public LoginPage()
        {
            InitializeComponent();
        }

        private void TxtEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderEmail.Visibility = string.IsNullOrEmpty(TxtEmail.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PlaceholderPassword.Visibility = string.IsNullOrEmpty(TxtPassword.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void TxtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderPassword.Visibility = string.IsNullOrEmpty(TxtPasswordVisible.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
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

            string email = TxtEmail.Text.Trim();
            string password = _isPasswordVisible
                ? TxtPasswordVisible.Text
                : TxtPassword.Password;

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Please enter your email address.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please enter your password.");
                return;
            }

            _isLoggingIn = true;               // NEW
            BtnLogin.IsEnabled = false;
            LoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                var loginResponse = await AuthApiService.Instance.LoginAsync(email, password);

                if (!loginResponse.Succeeded || loginResponse.Data == null || string.IsNullOrWhiteSpace(loginResponse.Data.Token))
                {
                    string err = loginResponse.Errors != null && loginResponse.Errors.Count > 0
                        ? string.Join("\n", loginResponse.Errors)
                        : "Invalid email or password. Please check your credentials and try again.";
                    ShowError(err);
                    return;
                }

                MainWindow.CurrentUserName = !string.IsNullOrWhiteSpace(loginResponse.Data.Email)
                    ? loginResponse.Data.Email
                    : email;

                MainWindow.CurrentUserRole = AuthApiService.Instance.IsAdmin ? "Administrator" : "Encoder";

                var mainWindow = new MainWindow();
                mainWindow.Show();
                AuditLogService.Instance.LogLogin(MainWindow.CurrentUserName, MainWindow.CurrentUserRole);
                ToastNotification.Show("Welcome", "Logged in as " + MainWindow.CurrentUserName + ".", ToastType.Success);
                this.Close();
            }
            finally
            {
                _isLoggingIn = false;          // NEW
                BtnLogin.IsEnabled = true;
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowError(string message)
        {
            TxtErrorModal.Text = message;
            ErrorModalOverlay.Visibility = Visibility.Visible;
        }

        private void BtnErrorClose_Click(object sender, RoutedEventArgs e)
        {
            ErrorModalOverlay.Visibility = Visibility.Collapsed;
            TxtEmail.Text = string.Empty;
            TxtPassword.Password = string.Empty;
            TxtPasswordVisible.Text = string.Empty;
            if (_isPasswordVisible)
            {
                TxtPasswordVisible.Visibility = Visibility.Collapsed;
                TxtPassword.Visibility = Visibility.Visible;
                TxtToggleIcon.Text = "\uED1A";
                _isPasswordVisible = false;
            }
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
    }
}
