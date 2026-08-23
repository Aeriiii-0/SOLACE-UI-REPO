using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SOLUM_UI.Services;

namespace SOLUM_UI
{
    public class AppUser
    {
        public string FirstName     { get; set; }
        public string LastName      { get; set; }
        public string Email         { get; set; }
        public string Role          { get; set; }
        public string ContactNumber { get; set; }
        public string Password      { get; set; }
        public string DateAdded     { get; set; }

        public string FullName => FirstName + " " + LastName;

        public string Initials
        {
            get
            {
                string f = string.IsNullOrEmpty(FirstName) ? "" : FirstName[0].ToString().ToUpper();
                string l = string.IsNullOrEmpty(LastName)  ? "" : LastName[0].ToString().ToUpper();
                return f + l;
            }
        }

        public SolidColorBrush RoleBadgeColor => Role == "Administrator"
            ? new SolidColorBrush(Color.FromRgb(0xF0, 0xE4, 0xE8))
            : new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9));

        public SolidColorBrush RoleTextColor => Role == "Administrator"
            ? new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43))
            : new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));
    }

    public partial class UserAdministrationPage : Page
    {
        private readonly List<AppUser> _allUsers = new List<AppUser>
        {
            new AppUser { FirstName="Admin",  LastName="User",      Email="admin@gmail.com",        Role="Administrator", ContactNumber="09170000001", DateAdded="Jan 1, 2024"  },
            new AppUser { FirstName="Maria",  LastName="Santos",    Email="maria.santos@gmail.com", Role="Encoder",       ContactNumber="09171234567", DateAdded="Feb 12, 2024" },
            new AppUser { FirstName="Juan",   LastName="Dela Cruz", Email="juan.delacruz@gmail.com",Role="Encoder",       ContactNumber="09281234567", DateAdded="Mar 3, 2024"  },
            new AppUser { FirstName="Ana",    LastName="Reyes",     Email="ana.reyes@gmail.com",    Role="Encoder",       ContactNumber="09391234567", DateAdded="Mar 20, 2024" },
            new AppUser { FirstName="Rosa",   LastName="Martinez",  Email="rosa.martinez@gmail.com",Role="Encoder",       ContactNumber="09611234567", DateAdded="Apr 5, 2024"  },
        };

        private List<AppUser> _filtered;
        private AppUser _editTarget = null;

        public UserAdministrationPage()
        {
            InitializeComponent();
            Loaded += (s, e) => { _filtered = new List<AppUser>(_allUsers); RefreshList(); };
        }

        private void RefreshList()
        {
            UserList.ItemsSource = null;
            UserList.ItemsSource = _filtered;
            TxtUserCount.Text = " " + _filtered.Count;
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = SearchBox.Text.ToLower().Trim();
            _filtered = string.IsNullOrEmpty(q)
                ? new List<AppUser>(_allUsers)
                : _allUsers.FindAll(u =>
                    (u.FullName       ?? "").ToLower().Contains(q) ||
                    (u.Email          ?? "").ToLower().Contains(q) ||
                    (u.ContactNumber  ?? "").ToLower().Contains(q));
            RefreshList();
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
            string email = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            AppUser user = _allUsers.Find(u => u.Email == email);
            if (user == null) return;

            _editTarget = user;
            TxtFirstName.Text      = user.FirstName;
            TxtLastName.Text       = user.LastName;
            TxtEmail.Text          = user.Email;
            TxtContactNumber.Text  = user.ContactNumber;
            PwdPassword.Password   = string.Empty;
            PwdConfirm.Password    = string.Empty;

            SetPanelMode(true);
            ClearErrors(null, null);
            ColPanel.Width = new GridLength(324);
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            string email = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            AppUser user = _allUsers.Find(u => u.Email == email);
            if (user == null) return;

            if (user.Role == "Administrator")
            {
                ToastNotification.Show("Cannot Delete", "Administrator accounts cannot be deleted.", ToastType.Warning);
                return;
            }

            var result = MessageBox.Show("Delete " + user.FullName + "?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            _allUsers.Remove(user);
            if (_editTarget == user) ClosePanel_Click(null, null);
            Search_TextChanged(null, null);
            ToastNotification.Show("User Deleted", user.FullName + " was removed.", ToastType.Warning);
            AuditLogService.Instance.LogUserAdmin("deleted", user.FullName + " (" + user.Email + ")",
                MainWindow.CurrentUserName, MainWindow.CurrentUserRole);
        }

        private void ClosePanel_Click(object sender, RoutedEventArgs e)
        {
            ColPanel.Width = new GridLength(0);
            _editTarget = null;
            ResetForm();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            ClearErrors(null, null);

            string firstName = TxtFirstName.Text.Trim();
            string lastName  = TxtLastName.Text.Trim();
            string email     = TxtEmail.Text.Trim();
            string contact   = TxtContactNumber.Text.Trim();
            string password  = PwdPassword.Password;
            string confirm   = PwdConfirm.Password;
            bool ok = true;

            if (string.IsNullOrWhiteSpace(firstName))
            { ErrFirstName.Visibility = Visibility.Visible; ok = false; }

            if (string.IsNullOrWhiteSpace(lastName))
            { ErrLastName.Visibility = Visibility.Visible; ok = false; }

            if (string.IsNullOrWhiteSpace(email))
            { ErrEmail.Text = "Required."; ErrEmail.Visibility = Visibility.Visible; ok = false; }
            else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            { ErrEmail.Text = "Enter a valid email."; ErrEmail.Visibility = Visibility.Visible; ok = false; }
            else if (_editTarget == null && _allUsers.Exists(u => u.Email.ToLower() == email.ToLower()))
            { ErrEmail.Text = "Email already registered."; ErrEmail.Visibility = Visibility.Visible; ok = false; }
            else if (_editTarget != null && email.ToLower() != _editTarget.Email.ToLower()
                     && _allUsers.Exists(u => u.Email.ToLower() == email.ToLower()))
            { ErrEmail.Text = "Email already in use."; ErrEmail.Visibility = Visibility.Visible; ok = false; }

            if (string.IsNullOrWhiteSpace(contact))
            { ErrContactNumber.Text = "Required."; ErrContactNumber.Visibility = Visibility.Visible; ok = false; }
            else if (!Regex.IsMatch(contact, @"^\d{7,15}$"))
            { ErrContactNumber.Text = "Enter a valid contact number."; ErrContactNumber.Visibility = Visibility.Visible; ok = false; }

            bool pwdProvided = !string.IsNullOrWhiteSpace(password) || !string.IsNullOrWhiteSpace(confirm);
            if (_editTarget == null || pwdProvided)
            {
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
                _editTarget.FirstName     = firstName;
                _editTarget.LastName      = lastName;
                _editTarget.Email         = email;
                _editTarget.ContactNumber = contact;
                if (pwdProvided) _editTarget.Password = password;

                Search_TextChanged(null, null);
                ToastNotification.Show("User Updated", firstName + " " + lastName + " was updated.", ToastType.Info);
                AuditLogService.Instance.LogUserAdmin("updated",
                    firstName + " " + lastName + " (" + email + ")",
                    MainWindow.CurrentUserName, MainWindow.CurrentUserRole);
                ClosePanel_Click(null, null);
            }
            else
            {
                _allUsers.Add(new AppUser
                {
                    FirstName     = firstName,
                    LastName      = lastName,
                    Email         = email,
                    Role          = "Encoder",
                    ContactNumber = contact,
                    Password      = password,
                    DateAdded     = DateTime.Today.ToString("MMM d, yyyy")
                });

                Search_TextChanged(null, null);
                ToastNotification.Show("User Created", firstName + " " + lastName + " was added.", ToastType.Success);
                AuditLogService.Instance.LogUserAdmin("created",
                    firstName + " " + lastName + " (" + email + ")",
                    MainWindow.CurrentUserName, MainWindow.CurrentUserRole);
                ClosePanel_Click(null, null);
            }
        }

        private void SetPanelMode(bool editMode)
        {
            TxtPanelTitle.Text = editMode ? "Edit User" : "Add New User";
            TxtPanelSub.Text   = editMode ? "Update account details." : "Create a new user account.";
            TxtSubmitBtn.Text  = editMode ? "Save Changes" : "Create Account";
        }

        private void ResetForm()
        {
            TxtFirstName.Text         = string.Empty;
            TxtLastName.Text          = string.Empty;
            TxtEmail.Text             = string.Empty;
            TxtContactNumber.Text     = string.Empty;
            PwdPassword.Password      = string.Empty;
            PwdConfirm.Password       = string.Empty;
            FormErrorBanner.Visibility = Visibility.Collapsed;
            SetPanelMode(false);
        }

        private void ClearErrors(object sender, object e)
        {
            ErrFirstName.Visibility      = Visibility.Collapsed;
            ErrLastName.Visibility       = Visibility.Collapsed;
            ErrEmail.Visibility          = Visibility.Collapsed;
            ErrContactNumber.Visibility  = Visibility.Collapsed;
            ErrPassword.Visibility       = Visibility.Collapsed;
            ErrConfirm.Visibility        = Visibility.Collapsed;
            FormErrorBanner.Visibility   = Visibility.Collapsed;
        }
    }
}
