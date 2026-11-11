using System.Windows;

namespace SOLUM_UI
{
    public enum EntryMethod { None, Manual, Scan }

    public partial class EntryMethodDialog : Window
    {
        public EntryMethod Selected { get; private set; } = EntryMethod.None;

        public EntryMethodDialog()
        {
            InitializeComponent();
        }

        private void ManualFill_Click(object sender, RoutedEventArgs e)
        {
            Selected = EntryMethod.Manual;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, true);
        }

        private void ScanDocument_Click(object sender, RoutedEventArgs e)
        {
            Selected = EntryMethod.Scan;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, true);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Selected = EntryMethod.None;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, false);
        }
    }
}