using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace SOLUM_UI
{
    public partial class UserProfilePage : Page
    {
        private static readonly DateTime _sessionStart = DateTime.Now;

        public UserProfilePage()
        {
            InitializeComponent();
            Loaded += (s, e) => PopulateProfile();
        }

        /// <summary>Populate all read-only fields from current session.</summary>
        private void PopulateProfile()
        {
            string name     = MainWindow.CurrentUserName;
            string role     = MainWindow.CurrentUserRole;
            string barangay = MainWindow.CurrentUserBarangay;
            string email    = name.Contains("@") ? name : name.ToLower().Replace(" ", ".") + "@solace.gov.ph";

            TxtDisplayName.Text  = name;
            TxtDisplayRole.Text  = role;
            TxtDisplayEmail.Text = email;
            TxtFullName.Text     = name;
            TxtRole.Text         = role;
            TxtEmail.Text        = email;
            TxtBarangay.Text     = string.IsNullOrWhiteSpace(barangay) ? "All Barangays" : barangay;
            TxtInitial.Text      = !string.IsNullOrEmpty(name) ? name[0].ToString().ToUpper() : "U";

            string[] parts = name.Split(' ');
            TxtPwdFirstName.Text = parts.Length > 0 ? parts[0] : name;
            TxtPwdLastName.Text  = parts.Length > 1 ? parts[parts.Length - 1] : "";
            TxtPwdRole.Text      = role;

            TxtSessionStart.Text = _sessionStart.ToString("MMM d, yyyy  h:mm tt");
        }

        /// <summary>Scrolls the page to the Change Password section.</summary>
        public void ScrollToChangePassword()
        {
            Loaded += (s, e) =>
            {
                ChangePasswordSection.BringIntoView();
                TxtPwdEmail.Focus();
            };
        }

        private void TxtPwdEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            ErrPwdEmail.Visibility  = Visibility.Collapsed;
            ResetSentBanner.Visibility = Visibility.Collapsed;
        }

        /// <summary>Validates email and simulates sending a reset link.</summary>
        private void SendReset_Click(object sender, RoutedEventArgs e)
        {
            ErrPwdEmail.Visibility     = Visibility.Collapsed;
            ResetSentBanner.Visibility = Visibility.Collapsed;

            string email = TxtPwdEmail.Text.Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                ErrPwdEmail.Text       = "Email address is required.";
                ErrPwdEmail.Visibility = Visibility.Visible;
                return;
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrPwdEmail.Text       = "Please enter a valid email address.";
                ErrPwdEmail.Visibility = Visibility.Visible;
                return;
            }

            string expectedEmail = TxtEmail.Text.Trim().ToLower();
            if (!string.Equals(email, expectedEmail, StringComparison.OrdinalIgnoreCase))
            {
                ErrPwdEmail.Text       = "This email does not match the registered email for your account.";
                ErrPwdEmail.Visibility = Visibility.Visible;
                return;
            }

            TxtResetSent.Text          = "Reset link sent to " + email + ". Please check your inbox.";
            ResetSentBanner.Visibility = Visibility.Visible;
            TxtPwdEmail.IsReadOnly     = true;
            BtnSendReset.IsEnabled     = false;

            ToastNotification.Show("Reset Link Sent",
                "A password reset link has been sent to " + email + ".",
                ToastType.Success);

            Services.AuditLogService.Instance.LogSystem(
                "Password reset requested by " + MainWindow.CurrentUserName + " (" + email + ")");
        }

        /// <summary>Clears the email field and resets the form state.</summary>
        private void CancelReset_Click(object sender, RoutedEventArgs e)
        {
            TxtPwdEmail.Text           = string.Empty;
            TxtPwdEmail.IsReadOnly     = false;
            BtnSendReset.IsEnabled     = true;
            ErrPwdEmail.Visibility     = Visibility.Collapsed;
            ResetSentBanner.Visibility = Visibility.Collapsed;
        }
    }
}
