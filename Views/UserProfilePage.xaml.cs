using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;

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
        private async void BtnSaveChangePassword_Click(object sender, RoutedEventArgs e)
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
            else if (currentPassword == newPassword)
            {
                ErrNewPassword.Text = "New password must be different from current password.";
                ErrNewPassword.Visibility = Visibility.Visible;
                hasErrors = true;
            }

            if (hasErrors)
                return;

            BtnSaveChangePassword.IsEnabled = false;

            try
            {
                var resp = await AuthApiService.Instance.ChangePasswordAsync(currentPassword, newPassword);

                if (resp != null && resp.Succeeded)
                {
                    ChangePasswordSuccessBanner.Visibility = Visibility.Visible;

                    ToastNotification.Show("Success",
                        "Your password has been changed successfully!",
                        ToastType.Success);

                    Services.AuditLogService.Instance.LogSystem(
                        "Password changed by " + MainWindow.CurrentUserName);

                    // Clear fields and hide form after 2 seconds
                    await Task.Delay(2000);
                    ClearChangePasswordForm();
                    ChangePasswordFormPanel.Visibility = Visibility.Collapsed;
                    ChangePasswordButtonPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    string errorMsg = (resp?.Errors != null && resp.Errors.Count > 0)
                        ? string.Join("\n", resp.Errors)
                        : "Failed to change password. Please check your current password.";

                    // If error mentions current or old password, display under current password
                    if (errorMsg.IndexOf("current", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        errorMsg.IndexOf("incorrect", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        ErrCurrentPassword.Text = errorMsg;
                        ErrCurrentPassword.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ErrNewPassword.Text = errorMsg;
                        ErrNewPassword.Visibility = Visibility.Visible;
                    }

                    ToastNotification.Show("Password Change Failed", errorMsg, ToastType.Error);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Error", "An unexpected error occurred: " + ex.Message, ToastType.Error);
            }
            finally
            {
                BtnSaveChangePassword.IsEnabled = true;
            }
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
