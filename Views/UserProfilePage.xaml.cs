using System;
using System.Linq;
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
            string name     = MainWindow.FullName;
            string role     = MainWindow.CurrentUserRole;
            string barangay = MainWindow.CurrentUserBarangay;
            string firstName = MainWindow.FirstName;
            string lastName = MainWindow.LastName;
            string email    = MainWindow.CurrentUserEmail;
            string contactNumber = MainWindow.ContactNumber;

            TxtDisplayName.Text  = name;
            TxtDisplayRole.Text  = role;
            TxtDisplayEmail.Text = email;
            TxtFullName.Text     = name;
            TxtRole.Text         = role;
            TxtContactNumber.Text = string.IsNullOrWhiteSpace(contactNumber) ? "—" : contactNumber;
            TxtBarangay.Text     = string.IsNullOrWhiteSpace(barangay) ? "All Barangays" : barangay;
            TxtInitial.Text      = !string.IsNullOrEmpty(name) ? name[0].ToString().ToUpper() : "U";

            string[] parts = name.Split(' ');

            TxtSessionStart.Text = _sessionStart.ToString("MMM d, yyyy  h:mm tt");
        }

        /// <summary>Show the change password form when button is clicked.</summary>
        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ChangePasswordButtonPanel.Visibility = Visibility.Collapsed;
            ChangePasswordFormPanel.Visibility = Visibility.Visible;
            PwdCurrentPassword.Focus();
        }

        /// <summary>Cancel change password and hide the form.</summary>
        private void BtnCancelChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ClearChangePasswordForm();
            ChangePasswordFormPanel.Visibility = Visibility.Collapsed;
            ChangePasswordButtonPanel.Visibility = Visibility.Visible;
        }

        /// <summary>Validate and save the new password.</summary>
        private void BtnSaveChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ClearErrorMessages();

            string currentPassword = PwdCurrentPassword.Password;
            string newPassword = PwdNewPassword.Password;
            string confirmPassword = PwdConfirmPassword.Password;

            bool hasErrors = false;

            // Validate current password
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                ErrCurrentPassword.Text = "Current password is required.";
                ErrCurrentPassword.Visibility = Visibility.Visible;
                hasErrors = true;
            }

            // Validate new password
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ErrNewPassword.Text = "New password is required.";
                ErrNewPassword.Visibility = Visibility.Visible;
                hasErrors = true;
            }
            else if (newPassword.Length < 6)
            {
                ErrNewPassword.Text = "New password must be at least 6 characters.";
                ErrNewPassword.Visibility = Visibility.Visible;
                hasErrors = true;
            }

            // Validate confirm password
            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                ErrConfirmPassword.Text = "Confirm password is required.";
                ErrConfirmPassword.Visibility = Visibility.Visible;
                hasErrors = true;
            }
            else if (newPassword != confirmPassword)
            {
                ErrConfirmPassword.Text = "Passwords do not match.";
                ErrConfirmPassword.Visibility = Visibility.Visible;
                hasErrors = true;
            }

            if (hasErrors)
                return;

            // TODO: Add actual password change logic via API call
            // For now, simulate success
            ChangePasswordSuccessBanner.Visibility = Visibility.Visible;

            ToastNotification.Show("Success",
                "Your password has been changed successfully!",
                ToastType.Success);

            Services.AuditLogService.Instance.LogSystem(
                "Password changed by " + MainWindow.CurrentUserName);

            // Hide form after success
            System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    ClearChangePasswordForm();
                    ChangePasswordFormPanel.Visibility = Visibility.Collapsed;
                    ChangePasswordButtonPanel.Visibility = Visibility.Visible;
                });
            });
        }

        /// <summary>Clear all error messages.</summary>
        private void ClearErrorMessages()
        {
            ErrCurrentPassword.Visibility = Visibility.Collapsed;
            ErrNewPassword.Visibility = Visibility.Collapsed;
            ErrConfirmPassword.Visibility = Visibility.Collapsed;
            ChangePasswordSuccessBanner.Visibility = Visibility.Collapsed;
        }

        /// <summary>Clear all password fields.</summary>
        private void ClearChangePasswordForm()
        {
            PwdCurrentPassword.Clear();
            PwdNewPassword.Clear();
            PwdConfirmPassword.Clear();
            ClearErrorMessages();
        }
    }
}
