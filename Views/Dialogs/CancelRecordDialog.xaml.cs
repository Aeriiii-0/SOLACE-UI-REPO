using System.Windows;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class CancelRecordDialog : Window
    {
        public bool Confirmed { get; private set; }
        public string Reason { get; private set; }

        public CancelRecordDialog()
        {
            InitializeComponent();
            TxtReason.Focus();
            
            // Show dim overlay when cancel application modal opens
            Loaded += (s, e) => MainWindow.ShowDimOverlay(fullWindow: true);
            Closed += (s, e) => MainWindow.HideDimOverlay();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            Reason = null;
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            var reason = TxtReason.Text?.Trim();
            
            if (string.IsNullOrWhiteSpace(reason))
            {
                TxtError.Text = "Please provide a reason for cancellation.";
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            if (reason.Length < 10)
            {
                TxtError.Text = "Reason must be at least 10 characters long.";
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            Confirmed = true;
            Reason = reason;
            Close();
        }
    }
}
