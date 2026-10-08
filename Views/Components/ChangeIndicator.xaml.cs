using System.Windows.Controls;

namespace SOLUM_UI.Views.Components
{
    /// <summary>
    /// Displays a summary badge of field changes count.
    /// DataContext should be an AuditLog object.
    /// </summary>
    public partial class ChangeIndicator : UserControl
    {
        public ChangeIndicator()
        {
            InitializeComponent();
        }
    }
}
