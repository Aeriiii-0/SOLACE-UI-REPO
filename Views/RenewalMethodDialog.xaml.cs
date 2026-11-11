using System.Windows;

namespace SOLUM_UI
{
    public enum RenewalChoice
    {
        Cancel,
        QuickRenew,
        ReassessAndRenew
    }

    public partial class RenewalMethodDialog : Window
    {
        public RenewalChoice SelectedChoice { get; private set; } = RenewalChoice.Cancel;

        public RenewalMethodDialog()
        {
            InitializeComponent();
        }

        private void QuickRenew_Click(object sender, RoutedEventArgs e)
        {
            SelectedChoice = RenewalChoice.QuickRenew;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, true);
        }

        private void ReassessRenew_Click(object sender, RoutedEventArgs e)
        {
            SelectedChoice = RenewalChoice.ReassessAndRenew;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, true);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            SelectedChoice = RenewalChoice.Cancel;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, false);
        }
    }
}