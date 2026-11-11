using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
                // Update progress bar on load
                UpdateProgressBar();
                
                // Attach hover handlers to tab TextBlocks
                AttachTabHoverHandlers();
            };
            
            // Clean up overlays when navigating away from this page
            Unloaded += (s, e) =>
            {
                MainWindow.HideDimOverlay();
            };
            
            // Subscribe to ViewModel property changes to update progress bar
            if (vm != null)
            {
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SubsidyRecommendationViewModel.ConfirmedGranteesCount) ||
                        e.PropertyName == nameof(SubsidyRecommendationViewModel.GranteeSlots))
                    {
                        UpdateProgressBar();
                    }
                    
                    // Reset tab font weights when switching tabs
                    if (e.PropertyName == nameof(SubsidyRecommendationViewModel.TabRecommendationsVisible) ||
                        e.PropertyName == nameof(SubsidyRecommendationViewModel.TabShortlistVisible) ||
                        e.PropertyName == nameof(SubsidyRecommendationViewModel.TabFinalVisible))
                    {
                        ResetTabFontWeights();
                    }
                    
                    // Drive skeleton visibility
                    if (e.PropertyName == nameof(SubsidyRecommendationViewModel.IsLoading))
                    {
                        if (SubsidySkeleton != null)
                            SubsidySkeleton.Visibility = vm.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                    }
                };
            }        }

        private void UpdateProgressBar()
        {
            if (ProgressBarContainer?.ActualWidth > 0 && DataContext is SubsidyRecommendationViewModel vm)
            {
                int confirmed = vm.ConfirmedGranteesCount;
                int total = vm.GranteeSlots;
                
                if (total > 0)
                {
                    double percentage = (double)confirmed / total;
                    double progressWidth = ProgressBarContainer.ActualWidth * percentage;
                    
                    if (ProgressBarFill != null)
                    {
                        ProgressBarFill.Width = Math.Max(0, Math.Min(progressWidth, ProgressBarContainer.ActualWidth));
                    }
                }
                else if (ProgressBarFill != null)
                {
                    ProgressBarFill.Width = 0;
                }
            }
        }

        private void AttachTabHoverHandlers()
        {
            // Tab 0 Hover - Bold font only (no line)
            if (FindName("Tab0Text") is TextBlock tab0)
            {
                tab0.MouseEnter += (s, e) => 
                {
                    if (DataContext is SubsidyRecommendationViewModel vm && !vm.TabRecommendationsVisible)
                    {
                        tab0.FontWeight = System.Windows.FontWeights.Bold;
                    }
                };
                tab0.MouseLeave += (s, e) =>
                {
                    if (DataContext is SubsidyRecommendationViewModel vm && !vm.TabRecommendationsVisible)
                    {
                        tab0.FontWeight = System.Windows.FontWeights.SemiBold;
                    }
                };
            }

            // Tab 1 Hover - Bold font only (no line)
            if (FindName("Tab1Text") is TextBlock tab1)
            {
                tab1.MouseEnter += (s, e) => 
                {
                    if (DataContext is SubsidyRecommendationViewModel vm && !vm.TabShortlistVisible)
                    {
                        tab1.FontWeight = System.Windows.FontWeights.Bold;
                    }
                };
                tab1.MouseLeave += (s, e) =>
                {
                    if (DataContext is SubsidyRecommendationViewModel vm && !vm.TabShortlistVisible)
                    {
                        tab1.FontWeight = System.Windows.FontWeights.SemiBold;
                    }
                };
            }

            // Tab 2 Hover - Bold font only (no line)
            if (FindName("Tab2Text") is TextBlock tab2)
            {
                tab2.MouseEnter += (s, e) => 
                {
                    if (DataContext is SubsidyRecommendationViewModel vm && !vm.TabFinalVisible)
                    {
                        tab2.FontWeight = System.Windows.FontWeights.Bold;
                    }
                };
                tab2.MouseLeave += (s, e) =>
                {
                    if (DataContext is SubsidyRecommendationViewModel vm && !vm.TabFinalVisible)
                    {
                        tab2.FontWeight = System.Windows.FontWeights.SemiBold;
                    }
                };
            }
        }

        private void ResetTabFontWeights()
        {
            if (DataContext is SubsidyRecommendationViewModel vm)
            {
                // Reset Tab 0
                if (FindName("Tab0Text") is TextBlock tab0)
                {
                    tab0.FontWeight = vm.TabRecommendationsVisible 
                        ? System.Windows.FontWeights.Bold 
                        : System.Windows.FontWeights.SemiBold;
                }

                // Reset Tab 1
                if (FindName("Tab1Text") is TextBlock tab1)
                {
                    tab1.FontWeight = vm.TabShortlistVisible 
                        ? System.Windows.FontWeights.Bold 
                        : System.Windows.FontWeights.SemiBold;
                }

                // Reset Tab 2
                if (FindName("Tab2Text") is TextBlock tab2)
                {
                    tab2.FontWeight = vm.TabFinalVisible 
                        ? System.Windows.FontWeights.Bold 
                        : System.Windows.FontWeights.SemiBold;
                }
            }
        }

        /// <summary>
        /// Handle menu button click - open the floating popup menu
        /// </summary>
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (MenuPopup != null)
            {
                MenuPopup.IsOpen = true;
            }
        }

        /// <summary>
        /// Handle menu option selection - wire to edit/view commands
        /// </summary>
        private void MenuOption_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string option)
            {
                MenuPopup.IsOpen = false;

                // Get the ViewModel and execute appropriate command
                if (DataContext is SubsidyRecommendationViewModel vm)
                {
                    if (option == "EditCycle" && vm.EditCycleCommand?.CanExecute(null) == true)
                    {
                        vm.EditCycleCommand.Execute(null);
                    }
                    else if (option == "ViewDetails" && vm.ViewDetailsCommand?.CanExecute(null) == true)
                    {
                        vm.ViewDetailsCommand.Execute(null);
                    }
                    else if (option == "ViewDisqualified" && vm.OpenDisqualifiedModalCommand?.CanExecute(null) == true)
                    {
                        vm.OpenDisqualifiedModalCommand.Execute(null);
                    }
                }
            }
        }

        private void FilterDropBtn_Click(object sender, RoutedEventArgs e)
        {
            // Filter is now handled inside RecommendationQueueGrid directly
        }

        private StackPanel CreateFilterContent() => new StackPanel();

        private Border CreateFilterOption(string label, string tag) => new Border();
    }
}