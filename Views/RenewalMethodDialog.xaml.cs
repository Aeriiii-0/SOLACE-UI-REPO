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
            DialogResult = true;
            Close();
        }

        private void ReassessRenew_Click(object sender, RoutedEventArgs e)
        {
            SelectedChoice = RenewalChoice.ReassessAndRenew;
            DialogResult = true;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            SelectedChoice = RenewalChoice.Cancel;
            DialogResult = false;
            Close();
        }
    }
}
