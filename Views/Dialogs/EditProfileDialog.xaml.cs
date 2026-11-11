using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services.Api;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class EditProfileDialog : Window
    {
        private static readonly Regex PhoneRegex = new Regex(@"^09\d{9}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);
        
        public bool Confirmed { get; private set; }
        public string UpdatedFullName { get; private set; }
        public string UpdatedEmail { get; private set; }
        public string UpdatedContactNumber { get; private set; }

        private readonly string _currentFullName;
        private readonly string _currentEmail;
        private readonly string _currentContactNumber;

        public EditProfileDialog(string currentFullName, string currentEmail, string currentContactNumber)
        {
            InitializeComponent();
            
            _currentFullName = currentFullName;
            _currentEmail = currentEmail;
            _currentContactNumber = currentContactNumber;
            
            // Populate current values
            TxtFullName.Text = currentFullName ?? string.Empty;
            TxtEmail.Text = currentEmail ?? string.Empty;
            TxtContactNumber.Text = currentContactNumber ?? string.Empty;
            
            // Focus on full name field
            Loaded += (s, e) => TxtFullName.Focus();
            
            // Show dim overlay
            Loaded += (s, e) => MainWindow.ShowDimOverlay(fullWindow: true);
            Closed += (s, e) =>
            {
                MainWindow.HideDimOverlay();
                try { Owner?.Activate(); } catch { }
            };

            // Guard against custom-chrome window losing focus when child dialogs close
            Deactivated += (s, e) =>
            {
                if (IsLoaded && IsVisible)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (IsVisible && !IsActive)
                            Activate();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            };
            
            // Real-time validation
            TxtFullName.TextChanged += (s, e) => ValidateFullName();
            TxtEmail.TextChanged += (s, e) => ValidateEmail();
            TxtContactNumber.TextChanged += (s, e) => ValidateContactNumber();
            
            // Escape key to cancel
            KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                    BtnCancel_Click(s, e);
            };
        }

        private bool ValidateFullName()
        {
            string name = TxtFullName.Text?.Trim();
            
            if (string.IsNullOrWhiteSpace(name))
            {
                ErrFullName.Text = "Full name is required";
                ErrFullName.Visibility = Visibility.Visible;
                TxtFullName.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xE5, 0x39, 0x35));
                return false;
            }
            
            if (name.Length < 2)
            {
                ErrFullName.Text = "Name must be at least 2 characters";
                ErrFullName.Visibility = Visibility.Visible;
                TxtFullName.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xE5, 0x39, 0x35));
                return false;
            }
            
            ErrFullName.Visibility = Visibility.Collapsed;
            TxtFullName.BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xD4, 0xA0, 0xB0));
            return true;
        }

        private bool ValidateContactNumber()
        {
            string contact = TxtContactNumber.Text?.Trim();
            
            // Contact number is optional
            if (string.IsNullOrWhiteSpace(contact))
            {
                ErrContactNumber.Visibility = Visibility.Collapsed;
                TxtContactNumber.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xD4, 0xA0, 0xB0));
                return true;
            }
            
            // If provided, validate format (09XXXXXXXXX - 11 digits starting with 09)
            if (!PhoneRegex.IsMatch(contact))
            {
                ErrContactNumber.Text = "Must be 11 digits starting with 09 (e.g., 09123456789)";
                ErrContactNumber.Visibility = Visibility.Visible;
                TxtContactNumber.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xE5, 0x39, 0x35));
                return false;
            }
            
            ErrContactNumber.Visibility = Visibility.Collapsed;
            TxtContactNumber.BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xD4, 0xA0, 0xB0));
            return true;
        }

        private bool ValidateEmail()
        {
            string email = TxtEmail.Text?.Trim();
            
            if (string.IsNullOrWhiteSpace(email))
            {
                ErrEmail.Text = "Email is required";
                ErrEmail.Visibility = Visibility.Visible;
                TxtEmail.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xE5, 0x39, 0x35));
                return false;
            }
            
            if (!EmailRegex.IsMatch(email))
            {
                ErrEmail.Text = "Invalid email format";
                ErrEmail.Visibility = Visibility.Visible;
                TxtEmail.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xE5, 0x39, 0x35));
                return false;
            }
            
            ErrEmail.Visibility = Visibility.Collapsed;
            TxtEmail.BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xD4, 0xA0, 0xB0));
            return true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            Close();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate all fields
            bool isValid = ValidateFullName() & ValidateEmail() & ValidateContactNumber();
            
            if (!isValid)
                return;
            
            string newFullName = TxtFullName.Text?.Trim();
            string newEmail = TxtEmail.Text?.Trim();
            string newContactNumber = TxtContactNumber.Text?.Trim();
            
            // Check if anything changed
            bool nameChanged = !string.Equals(newFullName, _currentFullName, StringComparison.Ordinal);
            bool emailChanged = !string.Equals(newEmail, _currentEmail, StringComparison.OrdinalIgnoreCase);
            bool contactChanged = !string.Equals(newContactNumber, _currentContactNumber, StringComparison.Ordinal);
            
            if (!nameChanged && !emailChanged && !contactChanged)
            {
                ActionConfirmDialog.Info(
                    "No Changes",
                    "You haven't made any changes to your profile.",
                    this);
                return;
            }
            
            // Disable save button during API call
            BtnSave.IsEnabled = false;
            
            try
            {
                // Call API to update profile
                var request = new UpdateProfileRequest
                {
                    FullName = newFullName,
                    Email = newEmail,
                    ContactNumber = string.IsNullOrWhiteSpace(newContactNumber) ? null : newContactNumber
                };
                
                var response = await AuthApiService.Instance.UpdateProfileAsync(request);
                
                if (response.Succeeded)
                {
                    UpdatedFullName = newFullName;
                    UpdatedEmail = newEmail;
                    UpdatedContactNumber = newContactNumber;
                    Confirmed = true;
                    
                    // Show success message
                    ActionConfirmDialog.Success(
                        "Profile Updated",
                        "Your profile has been updated successfully.",
                        this);
                    
                    Close();
                }
                else
                {
                    string errorMsg = response.Errors != null && response.Errors.Count > 0
                        ? string.Join("\n", response.Errors)
                        : "Failed to update profile. Please try again.";
                    
                    ActionConfirmDialog.Error(
                        "Update Failed",
                        errorMsg,
                        this);
                }
            }
            catch (Exception ex)
            {
                ActionConfirmDialog.Error(
                    "Error",
                    $"An error occurred while updating your profile: {ex.Message}",
                    this);
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }
    }
}
