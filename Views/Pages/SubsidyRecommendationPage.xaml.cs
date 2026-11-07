using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Pages
{
    public partial class SubsidyRecommendationPage : Page
    {
        private Popup _filterPopup;

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
                };
            }
        }

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
                }
            }
        }

        private void FilterDropBtn_Click(object sender, RoutedEventArgs e)
        {
            // Create filter popup if not already created
            if (_filterPopup == null)
            {
                _filterPopup = new Popup();
                _filterPopup.PlacementTarget = FilterDropBtn;
                _filterPopup.Placement = PlacementMode.Bottom;
                _filterPopup.StaysOpen = false;
                _filterPopup.AllowsTransparency = true;

                var border = new Border
                {
                    Background = System.Windows.Media.Brushes.White,
                    CornerRadius = new CornerRadius(12),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 92, 29, 56)), // Maroon #5C1D38
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(6),
                    Child = CreateFilterContent()
                };

                // Add drop shadow effect
                border.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = System.Windows.Media.Color.FromArgb(255, 92, 29, 56),
                    BlurRadius = 12,
                    ShadowDepth = 3,
                    Opacity = 0.15
                };

                _filterPopup.Child = border;
            }

            _filterPopup.IsOpen = true;
        }

        private StackPanel CreateFilterContent()
        {
            var stack = new StackPanel();
            
            var allOption = CreateFilterOption("All Records", "All");
            var clearedOption = CreateFilterOption("Cleared (Standard)", "Clean");
            var collegeOption = CreateFilterOption("City Educ Check (17–24y)", "CollegeAge");

            stack.Children.Add(allOption);
            stack.Children.Add(clearedOption);
            stack.Children.Add(collegeOption);

            return stack;
        }

        private Border CreateFilterOption(string label, string tag)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6, 10, 6),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = tag,
                Background = System.Windows.Media.Brushes.Transparent
            };

            border.MouseLeftButtonDown += (s, e) =>
            {
                if (_filterPopup != null)
                    _filterPopup.IsOpen = false;
                // Filter logic can be added here or in ViewModel
            };

            // Add hover effect
            border.MouseEnter += (s, e) =>
            {
                border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 245, 236, 238)); // #F5ECEE
            };

            border.MouseLeave += (s, e) =>
            {
                border.Background = System.Windows.Media.Brushes.Transparent;
            };

            var text = new TextBlock
            {
                Text = label,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 92, 29, 56)) // Maroon #5C1D38
            };

            border.Child = text;
            return border;
        }
    }
}