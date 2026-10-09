using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Views.Dialogs;

namespace SOLUM_UI
{
    public class AppUser
    {
        public Guid Id { get; set; }   // NEW — required for update/delete calls
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; } = "Encoder";
        public string ContactNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string Password { get; set; }
        public string DateAdded { get; set; } = "—";

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

        public SolidColorBrush RoleBadgeColor => new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9));
        public SolidColorBrush RoleTextColor => new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));

        public string StatusText => IsActive ? "Active" : "Disabled";
        public SolidColorBrush StatusBadgeColor => IsActive
            ? new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9))
            : new SolidColorBrush(Color.FromRgb(0xFE, 0xEB, 0xEE)); // Soft red/pink for disabled
        public SolidColorBrush StatusBadgeBorder => IsActive
            ? new SolidColorBrush(Color.FromRgb(0xA5, 0xD6, 0xA7))
            : new SolidColorBrush(Color.FromRgb(0xEF, 0x9A, 0x9A));
        public SolidColorBrush StatusTextColor => IsActive
            ? new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x20))
            : new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
        public SolidColorBrush StatusDotColor => IsActive
            ? new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32))
            : new SolidColorBrush(Color.FromRgb(0xD3, 0x2F, 0x2F));

        public string ToggleActionToolTip => IsActive ? "Disable User Account" : "Re-enable User Account";
        public string ToggleButtonText => IsActive ? "Disable" : "Re-enable";
        public SolidColorBrush ToggleButtonBorder => IsActive
            ? new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0))
            : new SolidColorBrush(Color.FromRgb(0xA5, 0xD6, 0xA7));
        public SolidColorBrush ToggleButtonBg => IsActive
            ? new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA))
            : new SolidColorBrush(Color.FromRgb(0xEB, 0xF8, 0xEE));
        public SolidColorBrush ToggleButtonTextColor => IsActive
            ? new SolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61))
            : new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x20));
        public string ToggleIconData => IsActive
            ? "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12C20,13.85 19.37,15.55 18.31,16.9L7.1,5.69C8.45,4.63 10.15,4 12,4M4,12C4,10.15 4.63,8.45 5.69,7.1L16.9,18.31C15.55,19.37 13.85,20 12,20A8,8 0 0,1 4,12Z" // slashed/prohibition circle
            : "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M10,17L15,12L10,7V17Z"; // check or activate/play arrow
        public SolidColorBrush ToggleIconBrush => IsActive
            ? new SolidColorBrush(Color.FromRgb(0x75, 0x75, 0x75))
            : new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
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

            DataObject.AddPastingHandler(TxtFirstName, NamePasteHandler);
            DataObject.AddPastingHandler(TxtLastName, NamePasteHandler);
            DataObject.AddPastingHandler(TxtContactNumber, ContactPasteHandler);
        }

        private async Task LoadUsersAsync()
        {
            if (UsersLoadingOverlay  != null) UsersLoadingOverlay.IsLoading  = true;
            if (BtnRefreshUsers != null) BtnRefreshUsers.IsEnabled = false;
            try
            {
                string statusTag = (CmbUserStatus?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ALL";
                string searchTerm = SearchBox?.Text?.Trim();

                if (statusTag == "ACTIVE")
                {
                    var request = new GetApplicationUserRequest
                    {
                        Role = "Encoder",
                        SearchTerm = searchTerm,
                        Page = _currentPage,
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
                        PopulateUsersFromResponse(response.Data.Items, response.Data.TotalCount, defaultIsActive: true);
                    }
                }
                else if (statusTag == "DISABLED")
                {
                    var request = new GetApplicationUserRequest
                    {
                        Role = "Encoder",
                        SearchTerm = searchTerm,
                        Page = _currentPage,
                        PageSize = PageSize,
                        IsActive = false
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
                        PopulateUsersFromResponse(response.Data.Items, response.Data.TotalCount, defaultIsActive: false);
                    }
                }
                else
                {
                    // "ALL" Status: fetch active and disabled users and combine
                    var activeTask = UserApiService.Instance.GetApplicationUsersAsync(new GetApplicationUserRequest
                    {
                        Role = "Encoder",
                        SearchTerm = searchTerm,
                        Page = 1,
                        PageSize = 100,
                        IsActive = true
                    });
                    var disabledTask = UserApiService.Instance.GetApplicationUsersAsync(new GetApplicationUserRequest
                    {
                        Role = "Encoder",
                        SearchTerm = searchTerm,
                        Page = 1,
                        PageSize = 100,
                        IsActive = false
                    });

                    await Task.WhenAll(activeTask, disabledTask);
                    var activeRes = activeTask.Result;
                    var disabledRes = disabledTask.Result;

                    var userMap = new Dictionary<Guid, (ApplicationUserDTO dto, bool isActive)>();

                    if (activeRes?.Succeeded == true && activeRes.Data?.Items != null)
                    {
                        foreach (var dto in activeRes.Data.Items)
                        {
                            bool active = dto.IsActive ?? true;
                            userMap[dto.Id] = (dto, active);
                        }
                    }

                    if (disabledRes?.Succeeded == true && disabledRes.Data?.Items != null)
                    {
                        foreach (var dto in disabledRes.Data.Items)
                        {
                            // A user returned in disabled query is disabled
                            userMap[dto.Id] = (dto, false);
                        }
                    }

                    var allCombined = userMap.Values.Select(pair => new AppUser
                    {
                        Id = pair.dto.Id,
                        FirstName = pair.dto.FirstName,
                        LastName = pair.dto.LastName,
                        Email = pair.dto.Email,
                        ContactNumber = pair.dto.ContactNumber ?? string.Empty,
                        IsActive = pair.isActive,
                        Role = string.IsNullOrWhiteSpace(pair.dto.Role) ? "Encoder" : pair.dto.Role
                    }).ToList();

                    int totalCount = allCombined.Count;
                    _totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
                    if (_currentPage > _totalPages) _currentPage = _totalPages;

                    _allUsers = allCombined
                        .Skip((_currentPage - 1) * PageSize)
                        .Take(PageSize)
                        .ToList();
                }

                _filtered = new List<AppUser>(_allUsers);
                RefreshList();
                UpdatePagingControls();
            }
            finally
            {
                if (UsersLoadingOverlay != null) UsersLoadingOverlay.IsLoading = false;
                if (BtnRefreshUsers != null) BtnRefreshUsers.IsEnabled = true;
            }
        }

        private void PopulateUsersFromResponse(IEnumerable<ApplicationUserDTO> items, int totalCount, bool defaultIsActive = true)
        {
            _allUsers = items.Select(dto => new AppUser
            {
                Id = dto.Id,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                ContactNumber = dto.ContactNumber ?? string.Empty,
                IsActive = dto.IsActive ?? defaultIsActive,
                Role = string.IsNullOrWhiteSpace(dto.Role) ? "Encoder" : dto.Role
            }).ToList();

            _totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        }

        private async void CmbUserStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentPage = 1;
            await LoadUsersAsync();
        }

        private async void BtnRefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            await LoadUsersAsync();
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
            TxtEmail.IsReadOnly = true;
            TxtContactNumber.Text = user.ContactNumber ?? string.Empty;
            PwdPassword.Password = string.Empty;
            PwdConfirm.Password = string.Empty;

            if (CmbEditStatus != null)
            {
                CmbEditStatus.SelectedIndex = user.IsActive ? 0 : 1;
            }

            SetPanelMode(true);
            ClearErrors(null, null);
            ColPanel.Width = new GridLength(324);
        }

        private async void ToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            string tagStr = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            if (!Guid.TryParse(tagStr, out var id)) return;

            AppUser user = _allUsers.Find(u => u.Id == id);
            if (user == null) return;

            bool newStatus = !user.IsActive;
            string actionVerb = newStatus ? "re-enable" : "disable";
            string actionTitle = newStatus ? "Re-enable User" : "Disable User";
            string confirmText = newStatus ? "Re-enable" : "Disable";
            var theme = newStatus ? ConfirmThemeType.Success : ConfirmThemeType.Warning;

            bool isConfirmed = ActionConfirmDialog.Show(
                title: actionTitle,
                message: $"Are you sure you want to {actionVerb} this user account ({user.FullName})?",
                confirmText: confirmText,
                cancelText: "Cancel",
                theme: theme,
                owner: Window.GetWindow(this));

            if (!isConfirmed) return;

            if (UsersLoadingOverlay != null) UsersLoadingOverlay.IsLoading = true;
            try
            {
                var response = await UserApiService.Instance.UpdateUserStatusAsync(id, newStatus);

                if (!response.Succeeded)
                {
                    string err = response.Errors != null && response.Errors.Count > 0
                        ? string.Join("\n", response.Errors)
                        : $"Unable to {actionVerb} user.";
                    ToastNotification.Show("Status Update Failed", err, ToastType.Warning);
                    return;
                }

                user.IsActive = newStatus;
                RefreshList();

                ToastNotification.Show("Status Updated", $"{user.FullName} was {(newStatus ? "re-enabled" : "disabled")}.", ToastType.Success);
                AuditLogService.Instance.LogUserAdmin($"status changed to {(newStatus ? "Active" : "Disabled")}",
                    $"{user.FullName} ({user.Email})",
                    MainWindow.CurrentUserName, MainWindow.CurrentUserRole,
                    user.Id.ToString());

                await LoadUsersAsync();
            }
            finally
            {
                if (UsersLoadingOverlay != null) UsersLoadingOverlay.IsLoading = false;
            }
        }

        // CHANGED — Id-based lookup, calls real delete endpoint
        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            string tagStr = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            if (!Guid.TryParse(tagStr, out var id)) return;

            AppUser user = _allUsers.Find(u => u.Id == id);
            if (user == null) return;

            bool isConfirmed = ActionConfirmDialog.Show(
                title: "Delete User",
                message: $"Are you sure you want to permanently delete {user.FullName}? This action cannot be undone.",
                confirmText: "Delete User",
                cancelText: "Cancel",
                theme: ConfirmThemeType.Danger,
                owner: Window.GetWindow(this));

            if (!isConfirmed) return;

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
                MainWindow.CurrentUserName, MainWindow.CurrentUserRole,
                user.Id.ToString());

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
            string contactNumber = TxtContactNumber.Text.Trim();
            string password = PwdPassword.Password;
            string confirm = PwdConfirm.Password;
            bool ok = true;

            if (string.IsNullOrWhiteSpace(firstName))
            {
                ErrFirstName.Text = "Required.";
                ErrFirstName.Visibility = Visibility.Visible;
                ok = false;
            }
            else if (!Regex.IsMatch(firstName, @"^[a-zA-ZñÑ\.\s]+$"))
            {
                ErrFirstName.Text = "Only letters, spaces, and '.' are allowed.";
                ErrFirstName.Visibility = Visibility.Visible;
                ok = false;
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                ErrLastName.Text = "Required.";
                ErrLastName.Visibility = Visibility.Visible;
                ok = false;
            }
            else if (!Regex.IsMatch(lastName, @"^[a-zA-ZñÑ\.\s]+$"))
            {
                ErrLastName.Text = "Only letters, spaces, and '.' are allowed.";
                ErrLastName.Visibility = Visibility.Visible;
                ok = false;
            }

            if (string.IsNullOrWhiteSpace(contactNumber))
            {
                ErrContactNumber.Text = "Required.";
                ErrContactNumber.Visibility = Visibility.Visible;
                ok = false;
            }
            else
            {
                bool is09 = Regex.IsMatch(contactNumber, @"^09\d{9}$");
                bool is63 = Regex.IsMatch(contactNumber, @"^\+63\d{9,10}$");

                if (!is09 && !is63)
                {
                    ErrContactNumber.Text = "Must be 11 digits (starts with 09) or start with +63.";
                    ErrContactNumber.Visibility = Visibility.Visible;
                    ok = false;
                }
            }

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
                bool selectedStatus = (CmbEditStatus?.SelectedItem as ComboBoxItem)?.Tag?.ToString() == "true";
                bool statusChanging = selectedStatus != _editTarget.IsActive;

                string confirmMsg = statusChanging
                    ? $"Are you sure you want to save changes to this user account ({_editTarget.FullName}) and set status to {(selectedStatus ? "Active" : "Disabled")}?"
                    : $"Are you sure you want to save changes to this user account ({_editTarget.FullName})?";

                bool isConfirmed = ActionConfirmDialog.Show(
                    title: "Update User Account",
                    message: confirmMsg,
                    confirmText: "Save Changes",
                    cancelText: "Cancel",
                    theme: ConfirmThemeType.Primary,
                    owner: Window.GetWindow(this),
                    customIconData: "M20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18,2.9 17.35,2.9 16.96,3.29L15.12,5.12L18.87,8.87M3,17.25V21H6.75L17.81,9.93L14.07,6.19L3,17.25Z");

                if (!isConfirmed) return;

                if (UsersLoadingOverlay != null) UsersLoadingOverlay.IsLoading = true;
                try
                {
                    var updateReq = new UpdateApplicationUserRequest
                    {
                        Id = _editTarget.Id,
                        FirstName = firstName,
                        LastName = lastName,
                        ContactNumber = contactNumber
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

                // Check if account status was toggled in the edit dropdown
                if (statusChanging)
                {
                    var statusResponse = await UserApiService.Instance.UpdateUserStatusAsync(_editTarget.Id, selectedStatus);
                    if (!statusResponse.Succeeded)
                    {
                        string err = statusResponse.Errors != null && statusResponse.Errors.Count > 0
                            ? string.Join("\n", statusResponse.Errors)
                            : "Account details updated, but status update failed.";
                        ToastNotification.Show("Status Update Issue", err, ToastType.Warning);
                    }
                    else
                    {
                        _editTarget.IsActive = selectedStatus;
                        RefreshList();
                        AuditLogService.Instance.LogUserAdmin($"status changed to {(selectedStatus ? "Active" : "Disabled")}",
                            $"{_editTarget.FullName} ({_editTarget.Email})",
                            MainWindow.CurrentUserName, MainWindow.CurrentUserRole,
                            _editTarget.Id.ToString());
                    }
                }

                ToastNotification.Show("User Updated", firstName + " " + lastName + " was updated.", ToastType.Info);
                AuditLogService.Instance.LogUserAdmin("updated",
                    firstName + " " + lastName + " (" + _editTarget.Email + ")",
                    MainWindow.CurrentUserName, MainWindow.CurrentUserRole,
                    _editTarget.Id.ToString());
                ClosePanel_Click(null, null);
                await LoadUsersAsync();
            }
            finally
            {
                if (UsersLoadingOverlay != null) UsersLoadingOverlay.IsLoading = false;
            }
        }
            else
            {
                var response = await AuthApiService.Instance.RegisterAsync(email, password, firstName, lastName, contactNumber);

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

            if (PnlAccountStatus != null)
                PnlAccountStatus.Visibility = editMode ? Visibility.Visible : Visibility.Collapsed;
            if (PnlPassword != null)
                PnlPassword.Visibility = editMode ? Visibility.Collapsed : Visibility.Visible;
            if (PnlConfirm != null)
                PnlConfirm.Visibility = editMode ? Visibility.Collapsed : Visibility.Visible;
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
            if (CmbEditStatus != null) CmbEditStatus.SelectedIndex = 0;
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

        private void Name_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only letters (including Filipino ñ/Ñ), space, and period '.'
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-ZñÑ\.\s]+$");
        }

        private void NamePasteHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = ((string)e.DataObject.GetData(typeof(string))) ?? "";
                if (!Regex.IsMatch(text, @"^[a-zA-ZñÑ\.\s]+$"))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void ContactNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!(sender is TextBox tb)) return;

            // Only '+' and digits are allowed
            if (e.Text != "+" && !char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            string current = tb.Text ?? "";
            int selStart = tb.SelectionStart;
            int selLen = tb.SelectionLength;
            string resulting = current.Remove(selStart, selLen).Insert(selStart, e.Text);

            // Must start with '0' or '+'
            if (!resulting.StartsWith("0") && !resulting.StartsWith("+"))
            {
                e.Handled = true;
                return;
            }

            if (resulting.StartsWith("0"))
            {
                if (resulting.Contains("+"))
                {
                    e.Handled = true;
                    return;
                }
                if (resulting.Length >= 2 && resulting[1] != '9')
                {
                    e.Handled = true;
                    return;
                }
                if (!Regex.IsMatch(resulting, @"^\d+$"))
                {
                    e.Handled = true;
                    return;
                }
                if (resulting.Length > 11)
                {
                    e.Handled = true;
                    return;
                }
            }
            else if (resulting.StartsWith("+"))
            {
                if (resulting.IndexOf('+', 1) != -1)
                {
                    e.Handled = true;
                    return;
                }
                if (resulting.Length >= 2 && resulting[1] != '6')
                {
                    e.Handled = true;
                    return;
                }
                if (resulting.Length >= 3 && resulting[2] != '3')
                {
                    e.Handled = true;
                    return;
                }
                if (resulting.Length > 3 && !Regex.IsMatch(resulting.Substring(1), @"^\d+$"))
                {
                    e.Handled = true;
                    return;
                }
                if (resulting.Length > 13)
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void ContactPasteHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = ((string)e.DataObject.GetData(typeof(string)))?.Trim() ?? "";
                if (!(sender is TextBox tb)) { e.CancelCommand(); return; }

                string current = tb.Text ?? "";
                int selStart = tb.SelectionStart;
                int selLen = tb.SelectionLength;
                string resulting = current.Remove(selStart, selLen).Insert(selStart, text);

                bool valid09 = Regex.IsMatch(resulting, @"^09\d{0,9}$") && resulting.Length <= 11;
                bool valid63 = Regex.IsMatch(resulting, @"^\+63\d{0,10}$") && resulting.Length <= 13;

                if (!valid09 && !valid63)
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}