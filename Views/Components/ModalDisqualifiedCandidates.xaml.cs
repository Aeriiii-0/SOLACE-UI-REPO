using System.Windows;
using System.Windows.Controls;

namespace SOLUM_UI.Views.Components
{
    public partial class ModalDisqualifiedCandidates : UserControl
    {
        public ModalDisqualifiedCandidates()
        {
            InitializeComponent();
            // Removed automatic overlay management - overlays should only show for dialogs, not in-page components
        }
        
        /// <summary>
        /// Show this modal with overlay
        /// </summary>
        public void ShowModal()
        {
            Visibility = Visibility.Visible;
            MainWindow.ShowDimOverlay(fullWindow: false);
        }
        
        /// <summary>
        /// Hide this modal and clear overlay
        /// </summary>
        public void HideModal()
        {
            Visibility = Visibility.Collapsed;
            MainWindow.HideDimOverlay();
        }
    }
}