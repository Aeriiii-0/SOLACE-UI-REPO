using System.Windows;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class OpenFilePromptDialog : Window
    {
        public bool ShouldOpenFile { get; private set; }

        public OpenFilePromptDialog(string message = null)
        {
            InitializeComponent();

            // Set default message if none provided
            if (string.IsNullOrWhiteSpace(message))
            {
                TxtMessage.Text = "Would you like to open the exported file now?";
            }
            else
            {
                TxtMessage.Text = message;
            }

            // Escape key to close
            KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                    BtnNotNow_Click(s, e);
            };

            // Guard against custom-chrome window losing focus
            Deactivated += (s, e) =>
            {
                if (IsLoaded && IsVisible)
                {
                    Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        if (IsVisible && !IsActive)
                            Activate();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            };
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            ShouldOpenFile = true;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, true);
        }

        private void BtnNotNow_Click(object sender, RoutedEventArgs e)
        {
            ShouldOpenFile = false;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, false);
        }
    }
}
