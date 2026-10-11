using System;
using System.Text;
using System.Windows;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class AccountCreatedDialog : Window
    {
        private readonly string _fullName;
        private readonly string _email;
        private readonly string _password;
        private readonly string _role;

        public AccountCreatedDialog(string fullName, string email, string password, string role = "Encoder", string heading = "User Account Created", string subheading = "The user account has been registered. Share the credentials below with the employee.")
        {
            InitializeComponent();
            _fullName = fullName ?? string.Empty;
            _email = email ?? string.Empty;
            _password = password ?? string.Empty;
            _role = string.IsNullOrWhiteSpace(role) ? "Encoder" : role;

            if (TxtHeading != null && !string.IsNullOrEmpty(heading))
                TxtHeading.Text = heading;
            if (TxtSubheading != null && !string.IsNullOrEmpty(subheading))
                TxtSubheading.Text = subheading;

            TxtFullName.Text = _fullName.ToUpper();
            TxtEmail.Text = _email;
            TxtPassword.Text = _password;
            TxtRole.Text = _role.ToUpper();
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SOLACE SYSTEM CREDENTIALS ===");
            sb.AppendLine($"Name: {_fullName.ToUpper()}");
            sb.AppendLine($"Position: {_role.ToUpper()}");
            sb.AppendLine($"Email: {_email}");
            sb.AppendLine($"Temporary Password: {_password}");
            sb.AppendLine("=================================");
            sb.AppendLine("Please log in and update your password on your Profile page.");

            try
            {
                Clipboard.SetText(sb.ToString());
                TxtCopyBtn.Text = "Copied to Clipboard!";
                ToastNotification.Show("Credentials Copied", "Login credentials copied to clipboard.", ToastType.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not copy to clipboard: " + ex.Message, "Copy Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnDone_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        public static void Show(string fullName, string email, string password, string role = "Encoder", Window owner = null, string heading = "User Account Created", string subheading = "The user account has been registered. Share the credentials below with the employee.")
        {
            var dlg = new AccountCreatedDialog(fullName, email, password, role, heading, subheading)
            {
                Owner = owner
            };
            dlg.ShowDialog();
        }
    }
}
