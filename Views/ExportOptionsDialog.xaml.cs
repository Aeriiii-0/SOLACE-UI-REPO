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
            Width  = screenW;
            Height = screenH;
            Left   = screenLeft;
            Top    = screenTop;
            Shell.MaxHeight = screenH * 0.90;
            TxtFileName.Text = fileName;
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
                    ToastNotification.Show("Password Required", "Enter a password or uncheck the option.", ToastType.Warning);
                    return;
                }
                Password = pwd;
            }
            OpenAfter = ChkOpenAfter.IsChecked == true;
            Confirmed = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            Close();
        }
    }
}
