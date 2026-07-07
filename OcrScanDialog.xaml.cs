using System.Windows;

namespace SOLUM_UI
{
    public partial class OcrScanDialog : Window
    {
        public OcrScanDialog()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
