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
        private static readonly Regex NameAllowedRegex = new Regex(@"^[\p{L} \-]+$", RegexOptions.Compiled);
        private static readonly Regex NameDisallowedCharsRegex = new Regex(@"[^\p{L} \-]", RegexOptions.Compiled);
        
        public bool Confirmed { get; private set; }
        public string UpdatedFirstName { get; private set; }
        public string UpdatedLastName { get; private set; }
        public string UpdatedFullName => $"{UpdatedFirstName} {UpdatedLastName}".Trim();
        public string UpdatedContactNumber { get; private set; }

        private readonly string _currentFirstName;
        private readonly string _currentLastName;
        private readonly string _currentEmail;
        private readonly string _currentContactNumber;

        public EditProfileDialog(string currentFirstName, string currentLastName, string currentEmail, string currentContactNumber)
        {
            InitializeComponent();
            
            _currentFirstName = currentFirstName ?? string.Empty;
            _currentLastName = currentLastName ?? string.Empty;
            _currentEmail = currentEmail ?? string.Empty;
            _currentContactNumber = currentContactNumber ?? string.Empty;
            
            // Populate current values
            TxtFirstName.Text = _currentFirstName;
            TxtLastName.Text = _currentLastName;
            TxtEmail.Text = _currentEmail;
            TxtContactNumber.Text = _currentContactNumber;
            
            // Focus on first name field
            Loaded += (s, e) => TxtFirstName.Focus();
            
            // Show dim overlay
            Loaded += (s, e) => MainWindow.ShowDimOverlay(fullWindow: true);
            Closed += (s, e) =>
            {
                MainWindow.HideDimOverlay();
                try { Owner?.Activate(); } catch { }
            };
            
            // Real-time validation & character filtering
            TxtFirstName.PreviewTextInput += NameInput_PreviewTextInput;
            TxtLastName.PreviewTextInput += NameInput_PreviewTextInput;
            DataObject.AddPastingHandler(TxtFirstName, NameInput_Pasting);
            DataObject.AddPastingHandler(TxtLastName, NameInput_Pasting);

            TxtFirstName.TextChanged += (s, e) => { HideBannerError(); ValidateFirstName(); };
            TxtLastName.TextChanged += (s, e) => { HideBannerError(); ValidateLastName(); };
            TxtContactNumber.TextChanged += (s, e) => { HideBannerError(); ValidateContactNumber(); };
            
            // Escape key to cancel
            KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape && BtnCancel.IsEnabled)
                    BtnCancel_Click(s, e);
            };
        }

        private void ShowBannerError(string message)
        {
            TxtBannerError.Text = message;
            ErrBanner.Visibility = Visibility.Visible;
        }

        private void HideBannerError()
        {
            if (ErrBanner.Visibility == Visibility.Visible)
                ErrBanner.Visibility = Visibility.Collapsed;
        }

        private void SetLoading(bool isLoading)
        {
            SavingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            TxtSaveBtn.Text = isLoading ? "Saving..." : "Save Changes";
            BtnSave.IsEnabled = !isLoading;
            BtnCancel.IsEnabled = !isLoading;
            TxtFirstName.IsEnabled = !isLoading;
            TxtLastName.IsEnabled = !isLoading;
            TxtContactNumber.IsEnabled = !isLoading;
        }

        private static void NameInput_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text) || NameDisallowedCharsRegex.IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }

        private static void NameInput_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }
            string pasted = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            if (NameDisallowedCharsRegex.IsMatch(pasted))
            {
                e.CancelCommand();
            }
        }

        private bool ValidateFirstName() => ValidateNameField(TxtFirstName, ErrFirstName, "First name");

        private bool ValidateLastName() => ValidateNameField(TxtLastName, ErrLastName, "Last name");

        private static bool ValidateNameField(System.Windows.Controls.TextBox box, System.Windows.Controls.TextBlock errText, string label)
        {
            string name = box.Text?.Trim();
            var errorBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE5, 0x39, 0x35));
            var normalBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD4, 0xA0, 0xB0));

            if (string.IsNullOrWhiteSpace(name))
            {
                errText.Text = $"{label} is required";
                errText.Visibility = Visibility.Visible;
                box.BorderBrush = errorBrush;
                return false;
            }

            if (name.Length < 2)
            {
                errText.Text = $"{label} must be at least 2 characters";
                errText.Visibility = Visibility.Visible;
                box.BorderBrush = errorBrush;
                return false;
            }

            if (!NameAllowedRegex.IsMatch(name))
            {
                errText.Text = $"{label} can only contain letters, spaces, and hyphens (-)";
                errText.Visibility = Visibility.Visible;
                box.BorderBrush = errorBrush;
                return false;
            }

            errText.Visibility = Visibility.Collapsed;
            box.BorderBrush = normalBrush;
            return true;
        }

        private bool ValidateContactNumber()
        {
            string rawContact = TxtContactNumber.Text?.Trim() ?? string.Empty;
            string digitsOnly = Regex.Replace(rawContact, @"\D", "");
            
            // Contact number is optional
            if (string.IsNullOrWhiteSpace(digitsOnly))
            {
                ErrContactNumber.Visibility = Visibility.Collapsed;
                TxtContactNumber.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xD4, 0xA0, 0xB0));
                return true;
            }
            
            // If provided, validate format (09XXXXXXXXX - 11 digits starting with 09, ignoring formatting dashes)
            if (!PhoneRegex.IsMatch(digitsOnly))
            {
                ErrContactNumber.Text = "Must be 11 digits starting with 09 (e.g., 0912-345-6789)";
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

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
            Close();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate all editable fields
            bool isValid = ValidateFirstName() & ValidateLastName() & ValidateContactNumber();
            
            if (!isValid)
                return;
            
            string newFirstName = TxtFirstName.Text?.Trim();
            string newLastName = TxtLastName.Text?.Trim();
            string newContactNumber = Regex.Replace(TxtContactNumber.Text?.Trim() ?? string.Empty, @"\D", "");
            string currentContactDigits = Regex.Replace(_currentContactNumber ?? string.Empty, @"\D", "");
            
            // Check if anything changed
            bool firstNameChanged = !string.Equals(newFirstName, _currentFirstName, StringComparison.Ordinal);
            bool lastNameChanged = !string.Equals(newLastName, _currentLastName, StringComparison.Ordinal);
            bool contactChanged = !string.Equals(newContactNumber, currentContactDigits, StringComparison.Ordinal);
            
            if (!firstNameChanged && !lastNameChanged && !contactChanged)
            {
                ActionConfirmDialog.Info(
                    "No Changes",
                    "You haven't made any changes to your profile.",
                    this);
                return;
            }
            
            HideBannerError();
            SetLoading(true);
            
            try
            {
                // Call dedicated self-service profile endpoint (PUT /api/user/profile)
                var request = new UpdateUserProfileRequest
                {
                    FirstName = newFirstName,
                    LastName = newLastName,
                    ContactNumber = newContactNumber
                };
                
                var response = await UserApiService.Instance.UpdateCurrentUserProfileAsync(request);
                
                if (response != null && response.Succeeded)
                {
                    UpdatedFirstName = newFirstName;
                    UpdatedLastName = newLastName;
                    UpdatedContactNumber = newContactNumber;
                    Confirmed = true;

                    SetLoading(false);
                    
                    // Show success message
                    ActionConfirmDialog.Success(
                        "Profile Updated",
                        "Your profile has been updated successfully.",
                        this);
                    
                    DialogResult = true;
                    Close();
                }
                else
                {
                    string errorMsg = response?.Errors != null && response.Errors.Count > 0
                        ? string.Join("\n", response.Errors)
                        : "Failed to update profile. Please try again.";
                    
                    ShowBannerError(errorMsg);
                    SetLoading(false);

                    ActionConfirmDialog.Error(
                        "Update Failed",
                        errorMsg,
                        this);
                }
            }
            catch (Exception ex)
            {
                string err = $"An error occurred while updating your profile: {ex.Message}";
                ShowBannerError(err);
                SetLoading(false);

                ActionConfirmDialog.Error(
                    "Error",
                    err,
                    this);
            }
            finally
            {
                if (IsLoaded)
                    SetLoading(false);
            }
        }
    }
}
