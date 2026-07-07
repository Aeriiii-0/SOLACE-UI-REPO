using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        private void BtnLogin_Click(object sender, RoutedEventArgs e) => AttemptLogin();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AttemptLogin();
        }

        private async void AttemptLogin()
        {
            string email = TxtEmail.Text.Trim();
            string password = _isPasswordVisible
                ? TxtPasswordVisible.Text
                : TxtPassword.Password;

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Please enter your email address.");
                return;
            }

            if (!email.EndsWith("@gmail.com", System.StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Invalid email or password. Please check your credentials and try again.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please enter your password.");
                return;
            }

            BtnLogin.IsEnabled = false;
            LoadingOverlay.Visibility = Visibility.Visible;

            await Task.Delay(1500);

            MainWindow.CurrentUserName = email;
            MainWindow.CurrentUserRole = "Administrator";

            var mainWindow = new MainWindow();
            mainWindow.Show();
            ToastNotification.Show("Welcome", "Logged in as " + email + ".", ToastType.Success);
            this.Close();
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
