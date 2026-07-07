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
            DialogResult = true;
            Close();
        }

        private void ScanDocument_Click(object sender, RoutedEventArgs e)
        {
            Selected = EntryMethod.Scan;
            DialogResult = true;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Selected = EntryMethod.None;
            DialogResult = false;
            Close();
        }
    }
}
