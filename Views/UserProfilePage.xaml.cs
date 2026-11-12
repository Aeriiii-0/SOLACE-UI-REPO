using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;

namespace SOLUM_UI
{
    public partial class UserProfilePage : Page
    {
        private static readonly DateTime _sessionStart = DateTime.Now;

        private double _normalAccountDetailsHeight = 0;
        private Models.Api.ApplicationUserDTO _currentProfileDto;

        public UserProfilePage()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                PopulateProfile();
                UpdateCardClips();
                SyncAccountDetailsHeight();
                await LoadCurrentUserProfileAsync();
            };
        }

        private async Task LoadCurrentUserProfileAsync()
        {
            try
            {
                var resp = await UserApiService.Instance.GetCurrentUserProfileAsync();
                if (resp != null && resp.Succeeded && resp.Data != null)
                {
                    _currentProfileDto = resp.Data;
                    MainWindow.SetCurrentUser(resp.Data);
                    PopulateProfile();
                    (Application.Current.MainWindow as MainWindow)?.RefreshUserHeader();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserProfilePage] Failed to load profile: {ex.Message}");
            }
        }

        /// <summary>
        /// Clips a card to its own rounded corners so the filled decorative circles
        /// don't spill past the card edges. Hooked to SizeChanged on HeroCardBg / PasswordCardBg.
        /// </summary>
        private void RoundedClip_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Border b && e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                double r = b.CornerRadius.TopLeft > 0 ? b.CornerRadius.TopLeft : 22.0;
                b.Clip = new RectangleGeometry(new Rect(0, 0, e.NewSize.Width, e.NewSize.Height), r, r);
            }
            else if (sender is FrameworkElement fe && e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                fe.Clip = new RectangleGeometry(new Rect(0, 0, e.NewSize.Width, e.NewSize.Height), 22.0, 22.0);
            }
        }

        private void UpdateCardClips()
        {
            ApplyClip(HeroCardBg, 22.0);
            ApplyClip(PasswordCard, 22.0);
            ApplyClip(PasswordCardBg, 20.5);
        }

        private void ApplyClip(FrameworkElement fe, double radius)
        {
            if (fe != null && fe.ActualWidth > 0 && fe.ActualHeight > 0)
            {
                fe.Clip = new RectangleGeometry(new Rect(0, 0, fe.ActualWidth, fe.ActualHeight), radius, radius);
            }
        }

        private void SessionInfoCard_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SyncAccountDetailsHeight();
        }

        private void PasswordCard_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RoundedClip_SizeChanged(sender, e);
            SyncAccountDetailsHeight();
        }

        private void SyncAccountDetailsHeight()
        {
            if (ChangePasswordFormPanel != null && ChangePasswordFormPanel.Visibility == Visibility.Visible)
                return; // Do not expand left column when change password form expands

            if (PasswordCard != null && SessionInfoCard != null &&
                PasswordCard.ActualHeight > 0 && SessionInfoCard.ActualHeight > 0)
            {
                double targetHeight = PasswordCard.ActualHeight + 20.0 + SessionInfoCard.ActualHeight;
                if (targetHeight > 250)
                {
                    _normalAccountDetailsHeight = targetHeight;
                    AccountDetailsCard.Height = targetHeight;
                }
            }
        }

        /// <summary>Populate all read-only fields from current session.</summary>
        private void PopulateProfile()
        {
            string name = MainWindow.FullName;
            string role = MainWindow.CurrentUserRole;
            string barangay = MainWindow.CurrentUserBarangay;
            string email = MainWindow.CurrentUserEmail;
            string contactNumber = MainWindow.ContactNumber;

            bool isAllBarangays = string.IsNullOrWhiteSpace(barangay) ||
                                  barangay.Equals("ALL", StringComparison.OrdinalIgnoreCase);

            // Hero card
            TxtDisplayName.Text = (name ?? string.Empty).ToUpper();
            TxtDisplayRole.Text = (role ?? string.Empty).ToUpper();
            TxtDisplayEmail.Text = email;
            TxtInitial.Text = !string.IsNullOrEmpty(name) ? name[0].ToString().ToUpper() : "U";
            TxtScope.Text = isAllBarangays ? "All barangays" : barangay;

            // Account details
            TxtFullName.Text = (name ?? string.Empty).ToUpper();
            TxtRole.Text = (role ?? string.Empty).ToUpper();
            TxtContactNumber.Text = string.IsNullOrWhiteSpace(contactNumber) ? "—" : contactNumber;
            TxtBarangay.Text = isAllBarangays ? "ALL BARANGAYS" : barangay.ToUpper();

            // Session info
            TxtSessionStart.Text = _sessionStart.ToString("MMM d, yyyy  h:mm tt");
        }

        /// <summary>Show the change password form when button is clicked.</summary>
        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (AccountDetailsCard.ActualHeight > 200)
            {
                _normalAccountDetailsHeight = AccountDetailsCard.ActualHeight;
                AccountDetailsCard.Height = _normalAccountDetailsHeight;
            }
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
            if (_normalAccountDetailsHeight > 200)
            {
                AccountDetailsCard.Height = _normalAccountDetailsHeight;
            }
            Dispatcher.BeginInvoke(new Action(SyncAccountDetailsHeight), System.Windows.Threading.DispatcherPriority.Loaded);
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
                    if (_normalAccountDetailsHeight > 200)
                    {
                        AccountDetailsCard.Height = _normalAccountDetailsHeight;
                    }
                    Dispatcher.BeginInvoke(new Action(SyncAccountDetailsHeight), System.Windows.Threading.DispatcherPriority.Loaded);
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
    
        private async void BtnEditProfile_Click(object sender, RoutedEventArgs e)
        {
            string currentFirstName = _currentProfileDto?.FirstName ?? MainWindow.FirstName ?? string.Empty;
            string currentLastName = _currentProfileDto?.LastName ?? MainWindow.LastName ?? string.Empty;
            string currentEmail = _currentProfileDto?.Email ?? MainWindow.CurrentUserEmail ?? string.Empty;
            string currentContactNumber = _currentProfileDto?.ContactNumber ?? MainWindow.ContactNumber ?? string.Empty;
            
            var dialog = new Views.Dialogs.EditProfileDialog(currentFirstName, currentLastName, currentEmail, currentContactNumber)
            {
                Owner = Window.GetWindow(this)
            };
            
            if (dialog.ShowDialog() == true && dialog.Confirmed)
            {
                // Update local DTO and MainWindow static session properties
                if (_currentProfileDto != null)
                {
                    _currentProfileDto.FirstName = dialog.UpdatedFirstName;
                    _currentProfileDto.LastName = dialog.UpdatedLastName;
                    _currentProfileDto.ContactNumber = dialog.UpdatedContactNumber;
                }

                MainWindow.FirstName = (dialog.UpdatedFirstName ?? string.Empty).ToUpper();
                MainWindow.LastName = (dialog.UpdatedLastName ?? string.Empty).ToUpper();
                MainWindow.CurrentUserName = $"{MainWindow.FirstName} {MainWindow.LastName}".Trim();
                MainWindow.ContactNumber = dialog.UpdatedContactNumber ?? string.Empty;

                PopulateProfile();
                (Application.Current.MainWindow as MainWindow)?.RefreshUserHeader();

                // Re-sync with backend
                await LoadCurrentUserProfileAsync();
            }
        }
    }
}