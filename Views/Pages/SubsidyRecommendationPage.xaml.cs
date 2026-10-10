using System.Windows.Controls;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Pages
{
    public partial class SubsidyRecommendationPage : Page
    {
        public SubsidyRecommendationPage(SubsidyRecommendationViewModel viewModel = null)
        {
            InitializeComponent();
            var vm = viewModel ?? new SubsidyRecommendationViewModel();
            DataContext = vm;
            Loaded += async (s, e) =>
            {
                if (DataContext is SubsidyRecommendationViewModel activeVm && activeVm.AvailableFiscalCycles.Count == 0)
                {
                    await activeVm.RefreshDataAsync();
                }
            };
        }

        private void SlotBreakdownBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SlotPopup != null) SlotPopup.IsOpen = !SlotPopup.IsOpen;
        }
    }
}
