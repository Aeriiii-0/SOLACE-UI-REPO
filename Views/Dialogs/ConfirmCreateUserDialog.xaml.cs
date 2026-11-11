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
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, true);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, false);
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

            try
            {
                var targetOwner = (owner != null && owner.IsVisible)
                    ? owner
                    : (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible ? Application.Current.MainWindow : null);
                if (targetOwner != null && targetOwner.IsVisible)
                {
                    dlg.Owner = targetOwner;
                }
            }
            catch { }
            SOLUM_UI.Core.WindowPositionHelper.CenterOnOwner(dlg, dlg.Owner);
            using (MainWindow.ModalScope(true))
            {
                dlg.ShowDialog();
            }
            return dlg.Confirmed;
        }
    }
}
