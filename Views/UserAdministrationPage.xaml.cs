using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;

namespace SOLUM_UI
{
    public class AppUser
    {
        public Guid Id { get; set; }   // NEW — required for update/delete calls
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; } = "Encoder"; // CHANGED — fixed value, page only manages Encoders
        public string ContactNumber { get; set; } = string.Empty; // NOT PERSISTED — no backend field
        public string Password { get; set; }
        public string DateAdded { get; set; } = "—"; // NOT AVAILABLE — no backend field

        public string FullName => FirstName + " " + LastName;

        public string Initials
        {
            get
            {
                string f = string.IsNullOrEmpty(FirstName) ? "" : FirstName[0].ToString().ToUpper();
                string l = string.IsNullOrEmpty(LastName) ? "" : LastName[0].ToString().ToUpper();
                return f + l;
            }
        }

        public SolidColorBrush RoleBadgeColor => new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9)); // always Encoder styling now
        public SolidColorBrush RoleTextColor => new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));
    }

    public partial class UserAdministrationPage : Page
    {
        private List<AppUser> _allUsers = new List<AppUser>(); 
        private List<AppUser> _filtered = new List<AppUser>();
        private AppUser _editTarget = null;

        private const int PageSize = 10;         
        private int _currentPage = 1;                
        private int _totalPages = 1;               

        public UserAdministrationPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadUsersAsync(); 
        }

        private async Task LoadUsersAsync()
        {
            var request = new GetApplicationUserRequest
            {
                Role = "Encoder",
                SearchTerm = SearchBox?.Text?.Trim(),
                Page = _currentPage,        // CHANGED — was hardcoded to 1
                PageSize = PageSize,
                IsActive = true
            };

            var response = await UserApiService.Instance.GetApplicationUsersAsync(request);

            if (!response.Succeeded || response.Data == null)
            {
                string err = response.Errors != null && response.Errors.Count > 0
                    ? string.Join("\n", response.Errors)
                    : "Unable to load users.";
                ToastNotification.Show("Load Failed", err, ToastType.Warning);
                _allUsers = new List<AppUser>();
                _totalPages = 1;
            }
            else
            {
                _allUsers = response.Data.Items.Select(dto => new AppUser
                {
                    Id = dto.Id,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    Role = "Encoder"
                }).ToList();

                // Assumes PagedResult<T>.TotalCount — adjust if your actual property name differs
                int totalCount = response.Data.TotalCount;
                _totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));

                // Guard: if we're sitting on a page beyond the new total (e.g. after a delete
                // shrinks the count), step back and reload instead of showing an empty page.
                if (_currentPage > _totalPages)
                {
                    _currentPage = _totalPages;
                    await LoadUsersAsync();
                    return;
                }
            }

            _filtered = new List<AppUser>(_allUsers);
            RefreshList();
            UpdatePagingControls(); // NEW
        }

        private void UpdatePagingControls()
        {
            TxtPageInfo.Text = $"Page {_currentPage} of {_totalPages}";
            BtnPrevPage.IsEnabled = _currentPage > 1;
            BtnNextPage.IsEnabled = _currentPage < _totalPages;
        }

        // NEW
        private async void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            await LoadUsersAsync();
        }

        // NEW
        private async void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage >= _totalPages) return;
            _currentPage++;
            await LoadUsersAsync();
        }


        private void RefreshList()
        {
            UserList.ItemsSource = null;
            UserList.ItemsSource = _filtered;
            TxtUserCount.Text = " " + _filtered.Count;
        }

        
        private async void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentPage = 1;
            await LoadUsersAsync();
        }

        private void OpenAddPanel_Click(object sender, RoutedEventArgs e)
        {
            _editTarget = null;
            ResetForm();
            SetPanelMode(false);
            ColPanel.Width = new GridLength(324);
        }

        // CHANGED — looks up by Id now, not Email. Update your XAML so each row's edit button
        // sets Tag="{Binding Id}" instead of Tag="{Binding Email}".
        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            string tagStr = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            if (!Guid.TryParse(tagStr, out var id)) return;

            AppUser user = _allUsers.Find(u => u.Id == id);
            if (user == null) return;

            _editTarget = user;
            TxtFirstName.Text = user.FirstName;
            TxtLastName.Text = user.LastName;
            TxtEmail.Text = user.Email;
            TxtEmail.IsReadOnly = true;   // NEW — email can't be changed via UpdateApplicationUserRequest
            TxtContactNumber.Text = string.Empty; // not available from backend
            PwdPassword.Password = string.Empty;
            PwdConfirm.Password = string.Empty;

            SetPanelMode(true);
            ClearErrors(null, null);
            ColPanel.Width = new GridLength(324);
        }

        // CHANGED — Id-based lookup, calls real delete endpoint
        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            string tagStr = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            if (!Guid.TryParse(tagStr, out var id)) return;

            AppUser user = _allUsers.Find(u => u.Id == id);
            if (user == null) return;

            var result = MessageBox.Show("Delete " + user.FullName + "?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            var response = await UserApiService.Instance.DeleteApplicationUserAsync(id);

            if (!response.Succeeded)
            {
                string err = response.Errors != null && response.Errors.Count > 0
                    ? string.Join("\n", response.Errors)
                    : "Unable to delete user.";
                ToastNotification.Show("Delete Failed", err, ToastType.Warning);
                return;
            }

            if (_editTarget == user) ClosePanel_Click(null, null);
            ToastNotification.Show("User Deleted", user.FullName + " was removed.", ToastType.Warning);
            AuditLogService.Instance.LogUserAdmin("deleted", user.FullName + " (" + user.Email + ")",
                MainWindow.CurrentUserName, MainWindow.CurrentUserRole);

            await LoadUsersAsync();
        }

        private void ClosePanel_Click(object sender, RoutedEventArgs e)
        {
            ColPanel.Width = new GridLength(0);
            _editTarget = null;
            ResetForm();
        }

        // CHANGED — now async, branches into Register (create) or Update against real endpoints
        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            ClearErrors(null, null);

            string firstName = TxtFirstName.Text.Trim();
            string lastName = TxtLastName.Text.Trim();
            string email = TxtEmail.Text.Trim();
            string password = PwdPassword.Password;
            string confirm = PwdConfirm.Password;
            bool ok = true;

            if (string.IsNullOrWhiteSpace(firstName))
            { ErrFirstName.Visibility = Visibility.Visible; ok = false; }

            if (string.IsNullOrWhiteSpace(lastName))
            { ErrLastName.Visibility = Visibility.Visible; ok = false; }

            // Email validation only matters for CREATE — it's locked/read-only in edit mode
            if (_editTarget == null)
            {
                if (string.IsNullOrWhiteSpace(email))
                { ErrEmail.Text = "Required."; ErrEmail.Visibility = Visibility.Visible; ok = false; }
                else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                { ErrEmail.Text = "Enter a valid email."; ErrEmail.Visibility = Visibility.Visible; ok = false; }
                else if (_allUsers.Exists(u => u.Email.ToLower() == email.ToLower()))
                { ErrEmail.Text = "Email already registered."; ErrEmail.Visibility = Visibility.Visible; ok = false; }

                // Password only required on create — there's no update-password path on this endpoint
                if (string.IsNullOrWhiteSpace(password))
                { ErrPassword.Text = "Required."; ErrPassword.Visibility = Visibility.Visible; ok = false; }
                else if (password.Length < 6)
                { ErrPassword.Text = "Min. 6 characters."; ErrPassword.Visibility = Visibility.Visible; ok = false; }

                if (string.IsNullOrWhiteSpace(confirm))
                { ErrConfirm.Text = "Required."; ErrConfirm.Visibility = Visibility.Visible; ok = false; }
                else if (password != confirm)
                { ErrConfirm.Text = "Passwords do not match."; ErrConfirm.Visibility = Visibility.Visible; ok = false; }
            }

            if (!ok)
            {
                TxtFormError.Text = "Please fix the errors above.";
                FormErrorBanner.Visibility = Visibility.Visible;
                ToastNotification.Show("Incomplete Form", "Fill in all required fields.", ToastType.Warning);
                return;
            }

            if (_editTarget != null)
            {
                var updateReq = new UpdateApplicationUserRequest
                {
                    Id = _editTarget.Id,
                    FirstName = firstName,
                    LastName = lastName
                };

                var response = await UserApiService.Instance.UpdateApplicationUserAsync(updateReq);

                if (!response.Succeeded)
                {
                    string err = response.Errors != null && response.Errors.Count > 0
                        ? string.Join("\n", response.Errors)
                        : "Unable to update user.";
                    TxtFormError.Text = err;
                    FormErrorBanner.Visibility = Visibility.Visible;
                    return;
                }

                ToastNotification.Show("User Updated", firstName + " " + lastName + " was updated.", ToastType.Info);
                AuditLogService.Instance.LogUserAdmin("updated",
                    firstName + " " + lastName + " (" + _editTarget.Email + ")",
                    MainWindow.CurrentUserName, MainWindow.CurrentUserRole);
                ClosePanel_Click(null, null);
                await LoadUsersAsync();
            }
            else
            {
                var response = await AuthApiService.Instance.RegisterAsync(email, password, firstName, lastName);

                if (!response.Succeeded)
                {
                    string err = response.Errors != null && response.Errors.Count > 0
                        ? string.Join("\n", response.Errors)
                        : "Unable to create user.";
                    TxtFormError.Text = err;
                    FormErrorBanner.Visibility = Visibility.Visible;
                    return;
                }

                ToastNotification.Show("User Created", firstName + " " + lastName + " was added.", ToastType.Success);
                AuditLogService.Instance.LogUserAdmin("created",
                    firstName + " " + lastName + " (" + email + ")",
                    MainWindow.CurrentUserName, MainWindow.CurrentUserRole);
                ClosePanel_Click(null, null);
                await LoadUsersAsync();
            }
        }

        private void SetPanelMode(bool editMode)
        {
            TxtPanelTitle.Text = editMode ? "Edit User" : "Add New User";
            TxtPanelSub.Text = editMode ? "Update account details." : "Create a new user account.";
            TxtSubmitBtn.Text = editMode ? "Save Changes" : "Create Account";
        }

        private void ResetForm()
        {
            TxtFirstName.Text = string.Empty;
            TxtLastName.Text = string.Empty;
            TxtEmail.Text = string.Empty;
            TxtEmail.IsReadOnly = false; // NEW — re-enable for the next "Add" flow
            TxtContactNumber.Text = string.Empty;
            PwdPassword.Password = string.Empty;
            PwdConfirm.Password = string.Empty;
            FormErrorBanner.Visibility = Visibility.Collapsed;
            SetPanelMode(false);
        }

        private void ClearErrors(object sender, object e)
        {
            ErrFirstName.Visibility = Visibility.Collapsed;
            ErrLastName.Visibility = Visibility.Collapsed;
            ErrEmail.Visibility = Visibility.Collapsed;
            ErrContactNumber.Visibility = Visibility.Collapsed;
            ErrPassword.Visibility = Visibility.Collapsed;
            ErrConfirm.Visibility = Visibility.Collapsed;
            FormErrorBanner.Visibility = Visibility.Collapsed;
        }
    }
}