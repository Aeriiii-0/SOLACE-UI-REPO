using System.Windows.Controls;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Pages
{
    public partial class SubsidyRecommendationPage : Page
    {
        public SubsidyRecommendationPage(SubsidyRecommendationViewModel viewModel = null)
        {
            InitializeComponent();
            DataContext = viewModel ?? new SubsidyRecommendationViewModel();
        }
    }
}
