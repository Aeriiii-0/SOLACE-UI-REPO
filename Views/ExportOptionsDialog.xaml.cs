using System.Windows;
using System.Windows.Controls;

namespace SOLUM_UI
{
    public partial class ExportOptionsDialog : Window
    {
        public bool   Confirmed    { get; private set; }
        public string Password     { get; private set; }
        public bool   OpenAfter    { get; private set; }

        private bool _showingPlain = false;

        public ExportOptionsDialog(string fileName, double screenW, double screenH, double screenLeft, double screenTop)
        {
            InitializeComponent();
            TxtFileName.Text = System.IO.Path.GetFileName(fileName);
            SOLUM_UI.Core.WindowPositionHelper.CoverOwnerWindow(this, Shell);
            KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                    BtnCancel_Click(s, e);
            };
            Loaded += (s, e) => SOLUM_UI.Core.WindowPositionHelper.CoverOwnerWindow(this, Shell);
        }

        public ExportOptionsDialog(string fileName, Window owner = null)
        {
            InitializeComponent();
            var targetOwner = owner ?? Application.Current?.MainWindow;
            if (targetOwner != null && targetOwner.IsVisible)
            {
                try { Owner = targetOwner; } catch { }
            }
            TxtFileName.Text = System.IO.Path.GetFileName(fileName);
            SOLUM_UI.Core.WindowPositionHelper.CoverOwnerWindow(this, Shell);
            KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                    BtnCancel_Click(s, e);
            };
            Loaded += (s, e) => SOLUM_UI.Core.WindowPositionHelper.CoverOwnerWindow(this, Shell);
        }

        private void ChkPassword_Changed(object sender, RoutedEventArgs e)
        {
            PasswordSection.Visibility = ChkPassword.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void BtnTogglePwd_Click(object sender, RoutedEventArgs e)
        {
            _showingPlain = !_showingPlain;
            if (_showingPlain)
            {
                PwdBoxVisible.Text       = PwdBox.Password;
                PwdBox.Visibility        = Visibility.Collapsed;
                PwdBoxVisible.Visibility = Visibility.Visible;
            }
            else
            {
                PwdBox.Password          = PwdBoxVisible.Text;
                PwdBoxVisible.Visibility = Visibility.Collapsed;
                PwdBox.Visibility        = Visibility.Visible;
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (ChkPassword.IsChecked == true)
            {
                string pwd = _showingPlain ? PwdBoxVisible.Text : PwdBox.Password;
                if (string.IsNullOrWhiteSpace(pwd))
                {
                    Views.Dialogs.ActionConfirmDialog.Warning("Password Required", "Please enter a passcode or uncheck the password protection option.", this);
                    return;
                }
                Password = pwd;
            }
            OpenAfter = ChkOpenAfter.IsChecked == true;
            Confirmed = true;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, true);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, false);
        }
    }
}