using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Components
{
    public partial class QuotaAllocatorBar : UserControl
    {
        public QuotaAllocatorBar()
        {
            InitializeComponent();
        }

        private void FilterDrop_Click(object sender, RoutedEventArgs e)
        {
            FilterPopup.IsOpen = !FilterPopup.IsOpen;
        }

        private void AuditFilterOpt_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is string tag)
            {
                if (DataContext is SubsidyRecommendationViewModel vm)
                {
                    vm.ApplyAuditFilter(tag);
                }
            }
            FilterPopup.IsOpen = false;
        }
    }
}
