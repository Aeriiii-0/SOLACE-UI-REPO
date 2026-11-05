using System;
using System.Windows;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class ConfirmCreateUserDialog : Window
    {
        public bool Confirmed { get; private set; }

        public ConfirmCreateUserDialog(
            string fullName,
            string email,
            string role,
            string contactNumber,
            string scope,
            string password)
        {
            InitializeComponent();

            TxtFullName.Text = string.IsNullOrWhiteSpace(fullName) ? "—" : fullName.ToUpper();
            TxtEmail.Text = string.IsNullOrWhiteSpace(email) ? "—" : email;
            TxtRole.Text = string.IsNullOrWhiteSpace(role) ? "CASEWORKER" : role.ToUpper();
            TxtContact.Text = string.IsNullOrWhiteSpace(contactNumber) ? "—" : contactNumber;
            TxtScope.Text = string.IsNullOrWhiteSpace(scope) ? "Citywide" : scope;
            TxtPassword.Text = string.IsNullOrWhiteSpace(password) ? "—" : password;
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
            Close();
        }

        public static bool Show(
            string fullName,
            string email,
            string role,
            string contactNumber,
            string scope,
            string password,
            Window owner = null)
        {
            var dlg = new ConfirmCreateUserDialog(fullName, email, role, contactNumber, scope, password);

            if (owner != null)
            {
                dlg.Owner = owner;
            }
            else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                dlg.Owner = Application.Current.MainWindow;
            }

            dlg.ShowDialog();
            return dlg.Confirmed;
        }
    }
}
