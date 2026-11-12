using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SOLUM_UI.Services.Api;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class ForceChangePasswordDialog : Window
    {
        private readonly string _email;
        private bool _isCurrentVisible = false;
        private bool _isNewVisible = false;
        private bool _isConfirmVisible = false;

        public ForceChangePasswordDialog(string email, string currentPassword = null)
        {
            InitializeComponent();
            _email = email;
            if (!string.IsNullOrEmpty(currentPassword))
            {
                PwdCurrent.Password = currentPassword;
                TxtCurrentVisible.Text = currentPassword;
            }
        }

        private string GetCurrentPassword() => _isCurrentVisible ? TxtCurrentVisible.Text : PwdCurrent.Password;
        private string GetNewPassword() => _isNewVisible ? TxtNewVisible.Text : PwdNew.Password;
        private string GetConfirmPassword() => _isConfirmVisible ? TxtConfirmVisible.Text : PwdConfirm.Password;

        private void Fields_Changed(object sender, RoutedEventArgs e)
        {
            BorderError.Visibility = Visibility.Collapsed;
        }

        private void TxtCurrentVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isCurrentVisible && PwdCurrent.Password != TxtCurrentVisible.Text)
                PwdCurrent.Password = TxtCurrentVisible.Text;
            BorderError.Visibility = Visibility.Collapsed;
        }

        private void TxtNewVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isNewVisible && PwdNew.Password != TxtNewVisible.Text)
                PwdNew.Password = TxtNewVisible.Text;
            BorderError.Visibility = Visibility.Collapsed;
        }

        private void TxtConfirmVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isConfirmVisible && PwdConfirm.Password != TxtConfirmVisible.Text)
                PwdConfirm.Password = TxtConfirmVisible.Text;
            BorderError.Visibility = Visibility.Collapsed;
        }

        private void ToggleCurrentVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isCurrentVisible = !_isCurrentVisible;
            if (_isCurrentVisible)
            {
                TxtCurrentVisible.Text = PwdCurrent.Password;
                PwdCurrent.Visibility = Visibility.Collapsed;
                TxtCurrentVisible.Visibility = Visibility.Visible;
                TxtToggleCurrent.Text = "🔒";
            }
            else
            {
                PwdCurrent.Password = TxtCurrentVisible.Text;
                TxtCurrentVisible.Visibility = Visibility.Collapsed;
                PwdCurrent.Visibility = Visibility.Visible;
                TxtToggleCurrent.Text = "👁";
            }
        }

        private void ToggleNewVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isNewVisible = !_isNewVisible;
            if (_isNewVisible)
            {
                TxtNewVisible.Text = PwdNew.Password;
                PwdNew.Visibility = Visibility.Collapsed;
                TxtNewVisible.Visibility = Visibility.Visible;
                TxtToggleNew.Text = "🔒";
            }
            else
            {
                PwdNew.Password = TxtNewVisible.Text;
                TxtNewVisible.Visibility = Visibility.Collapsed;
                PwdNew.Visibility = Visibility.Visible;
                TxtToggleNew.Text = "👁";
            }
        }

        private void ToggleConfirmVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isConfirmVisible = !_isConfirmVisible;
            if (_isConfirmVisible)
            {
                TxtConfirmVisible.Text = PwdConfirm.Password;
                PwdConfirm.Visibility = Visibility.Collapsed;
                TxtConfirmVisible.Visibility = Visibility.Visible;
                TxtToggleConfirm.Text = "🔒";
            }
            else
            {
                PwdConfirm.Password = TxtConfirmVisible.Text;
                TxtConfirmVisible.Visibility = Visibility.Collapsed;
                PwdConfirm.Visibility = Visibility.Visible;
                TxtToggleConfirm.Text = "👁";
            }
        }

        private void ShowError(string msg)
        {
            TxtError.Text = msg;
            BorderError.Visibility = Visibility.Visible;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            string current = GetCurrentPassword();
            string newPwd = GetNewPassword();
            string confirm = GetConfirmPassword();
            if (string.IsNullOrWhiteSpace(current))
            {
                ShowError("Please enter your current temporary password.");
                return;
            }
            if (string.IsNullOrWhiteSpace(newPwd))
            {
                ShowError("Please enter your new password.");
                return;
            }
            if (newPwd.Length < 8 ||
                !System.Linq.Enumerable.Any(newPwd, char.IsUpper) ||
                !System.Linq.Enumerable.Any(newPwd, char.IsLower) ||
                !System.Linq.Enumerable.Any(newPwd, char.IsDigit) ||
                !System.Linq.Enumerable.Any(newPwd, c => !char.IsLetterOrDigit(c)))
            {
                ShowError("New password must be at least 8 characters and include uppercase, lowercase, a number, and a special character.");
                return;
            }
            if (string.Equals(current, newPwd, StringComparison.Ordinal))
            {
                ShowError("New password must be different from your current temporary password.");
                return;
            }
            if (!string.Equals(newPwd, confirm, StringComparison.Ordinal))
            {
                ShowError("New password and confirmation do not match.");
                return;
            }
            BtnSave.IsEnabled = false;
            try
            {
                var resp = await AuthApiService.Instance.ChangePasswordAsync(current, newPwd);
                if (resp != null && resp.Succeeded)
                {
                    ToastNotification.Show("Password Changed", "Your password has been successfully updated. Welcome!", ToastType.Success);
                    Services.AuditLogService.Instance.LogSystem($"Temporary password updated on initial login by {_email}");
                    SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, true);
                }
                else
                {
                    string err = (resp?.Errors != null && resp.Errors.Count > 0)
                        ? string.Join("\n", resp.Errors)
                        : "Failed to update password. Please check your credentials.";
                    ShowError(err);
                }
            }
            catch (Exception ex)
            {
                ShowError("An unexpected error occurred: " + ex.Message);
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, false);
        }
    }
}