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
            TxtFileName.Text = System.IO.Path.GetFileName(fileName);
            KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                    BtnCancel_Click(s, e);
            };
        }

        public ExportOptionsDialog(string fileName, Window owner = null)
        {
            InitializeComponent();
            var win = owner ?? Application.Current?.MainWindow;
            if (win != null && win.IsVisible)
            {
                Owner = win;
                Width = win.ActualWidth > 0 ? win.ActualWidth : 1200;
                Height = win.ActualHeight > 0 ? win.ActualHeight : 700;
                Left = win.Left;
                Top = win.Top;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                Width = 1200;
                Height = 700;
            }
            Shell.MaxHeight = Height * 0.90;
            TxtFileName.Text = System.IO.Path.GetFileName(fileName);
            KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                    BtnCancel_Click(s, e);
            };
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
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
            Close();
        }
    }
}