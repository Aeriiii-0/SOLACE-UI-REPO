using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Components
{
    public partial class RecommendationQueueGrid : UserControl
    {
        private readonly DispatcherTimer _scrollHintTimer;

        public RecommendationQueueGrid()
        {
            InitializeComponent();
            _scrollHintTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1200)
            };
            _scrollHintTimer.Tick += (s, e) =>
            {
                _scrollHintTimer.Stop();
                if (ScrollHintIndicator != null)
                    ScrollHintIndicator.Visibility = Visibility.Collapsed;
            };
            DataContextChanged += OnDataContextChanged;
            Loaded += (s, e) =>
            {
                AdjustColumnWidths();
            };
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    AdjustColumnWidths();
                }
            };
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SubsidyRecommendationViewModel oldVm)
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            if (e.NewValue is SubsidyRecommendationViewModel newVm)
            {
                newVm.PropertyChanged += OnViewModelPropertyChanged;
                UpdateBatchColumn(newVm.IsBatchMode);
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SubsidyRecommendationViewModel.IsBatchMode))
            {
                if (DataContext is SubsidyRecommendationViewModel vm)
                    UpdateBatchColumn(vm.IsBatchMode);
            }
        }

        private void UpdateBatchColumn(bool isBatch)
        {
            ColSelect.Width = isBatch ? 34 : 0;
            AdjustColumnWidths();
        }

        private void RowCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SubsidyRecommendationViewModel vm)
                vm.RecalculateSelectionCounts();
        }

        private void QueueListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustColumnWidths();
        }

        private void QueueListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var sv = e.OriginalSource as ScrollViewer ?? FindVisualChild<ScrollViewer>(QueueListView);
            if (sv != null)
            {
                // Infinite scrolling: trigger fetch of next batch when approaching bottom of current loaded items
                if (e.VerticalChange > 0 && sv.ScrollableHeight > 0)
                {
                    bool nearBottom = sv.VerticalOffset >= (sv.ScrollableHeight - 5) ||
                                      (sv.VerticalOffset + sv.ViewportHeight >= sv.ExtentHeight - 120);

                    if (nearBottom && DataContext is SubsidyRecommendationViewModel vm && vm.CanQueueLoadMore)
                    {
                        _ = vm.LoadNextQueuePageAsync();
                    }
                }

                // Dynamic Floating Scroll-Down Arrow
                if (ScrollHintIndicator != null)
                {
                    if (Math.Abs(e.VerticalChange) > 0.01 && sv.ScrollableHeight > 0 && sv.VerticalOffset < sv.ScrollableHeight - 8)
                    {
                        ScrollHintIndicator.Visibility = Visibility.Visible;
                        _scrollHintTimer.Stop();
                        _scrollHintTimer.Start();
                    }
                    else if (sv.ScrollableHeight <= 0 || sv.VerticalOffset >= sv.ScrollableHeight - 8)
                    {
                        _scrollHintTimer.Stop();
                        ScrollHintIndicator.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void AdjustColumnWidths()
        {
            double totalWidth = QueueListView.ActualWidth;
            if (totalWidth <= 0) return;

            // Reserve room for vertical scrollbar and prevent horizontal overflow
            double scrollMargin = 20.0;
            double selW = (ColSelect != null && ColSelect.Width > 0) ? 34.0 : 0.0;
            if (ColSelect != null) ColSelect.Width = selW;

            double rankW = 58.0;
            if (ColRank != null) ColRank.Width = rankW;

            double scoreW = 62.0;
            if (ColScore != null) ColScore.Width = scoreW;

            double depW = 60.0;
            if (ColDependents != null) ColDependents.Width = depW;

            double cardStatusW = 74.0;
            if (ColCardStatus != null) ColCardStatus.Width = cardStatusW;

            double validUntilW = 82.0;
            if (ColValidUntil != null) ColValidUntil.Width = validUntilW;

            double actionW = 144.0;
            if (ColAction != null) ColAction.Width = actionW;

            double fixedWidth = selW + rankW + scoreW + depW + cardStatusW + validUntilW + actionW;
            double availableFlexible = Math.Max(100.0, totalWidth - fixedWidth - scrollMargin);

            // Proportional allocation across flexible columns so their sum exactly equals availableFlexible
            double wName = Math.Floor(availableFlexible * 0.24);
            double wIncome = Math.Floor(availableFlexible * 0.18);
            double wCare = Math.Floor(availableFlexible * 0.19);
            double wCirc = Math.Floor(availableFlexible * 0.20);
            double wNeeds = Math.Max(20.0, availableFlexible - (wName + wIncome + wCare + wCirc));

            if (ColName != null) ColName.Width = wName;
            if (ColIncome != null) ColIncome.Width = wIncome;
            if (ColCare != null) ColCare.Width = wCare;
            if (ColCircumstance != null) ColCircumstance.Width = wCirc;
            if (ColNeeds != null) ColNeeds.Width = wNeeds;
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var sub = FindVisualChild<T>(child);
                if (sub != null) return sub;
            }
            return null;
        }

        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindVisualParent<T>(parentObject);
        }

        private Models.SubsidyItem _lastClickedQueueItem;

        private void Row_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is SubsidyRecommendationViewModel vm && vm.IsBatchMode)
            {
                var dep = e.OriginalSource as DependencyObject;
                if (dep != null)
                {
                    if (FindVisualParent<Button>(dep) != null)
                    {
                        return;
                    }
                    if (FindVisualParent<CheckBox>(dep) != null)
                    {
                        if (sender is ListViewItem lviBox && lviBox.DataContext is Models.SubsidyItem itemBox)
                        {
                            _lastClickedQueueItem = itemBox;
                        }
                        return;
                    }
                }
                if (sender is ListViewItem lvi && lvi.DataContext is Models.SubsidyItem item)
                {
                    bool isShiftDown = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == System.Windows.Input.ModifierKeys.Shift;
                    if (isShiftDown && _lastClickedQueueItem != null && vm.FilteredRecommendations != null)
                    {
                        int fromIndex = vm.FilteredRecommendations.IndexOf(_lastClickedQueueItem);
                        int toIndex = vm.FilteredRecommendations.IndexOf(item);
                        if (fromIndex >= 0 && toIndex >= 0)
                        {
                            int start = Math.Min(fromIndex, toIndex);
                            int end = Math.Max(fromIndex, toIndex);
                            bool targetState = !item.IsSelected;
                            for (int i = start; i <= end; i++)
                            {
                                vm.FilteredRecommendations[i].IsSelected = targetState;
                            }
                            _lastClickedQueueItem = item;
                            vm.RecalculateSelectionCounts();
                            e.Handled = true;
                            return;
                        }
                    }
                    item.IsSelected = !item.IsSelected;
                    _lastClickedQueueItem = item;
                    vm.RecalculateSelectionCounts();
                    e.Handled = true;
                }
            }
        }

        private void Row_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is Models.SubsidyItem candidate && DataContext is SubsidyRecommendationViewModel vm)
            {
                vm.OpenExplainDrawerCommand.Execute(candidate);
                e.Handled = true;
            }
        }

        // ── Search & Filter handlers ──────────────────────────────────────────

        private void FilterPill_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (QueueFilterPopup != null)
                QueueFilterPopup.IsOpen = !QueueFilterPopup.IsOpen;
        }

        private void FilterOpt_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border b && b.Tag is string tag
                && DataContext is SubsidyRecommendationViewModel vm)
            {
                vm.ApplyAuditFilter(tag);
            }
            // Close the popup after selection
            if (QueueFilterPopup != null)
                QueueFilterPopup.IsOpen = false;
        }

        private void FilterOptReset_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is SubsidyRecommendationViewModel vm)
            {
                vm.ClearAuditFilter();
            }
            // Close the popup after reset
            if (QueueFilterPopup != null)
                QueueFilterPopup.IsOpen = false;
        }

        private void ClearQueueSearch_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SubsidyRecommendationViewModel vm)
                vm.QueueSearchText = string.Empty;
            if (TxtQueueSearch != null) TxtQueueSearch.Focus();
        }

        private void ClearQueueFilter_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SubsidyRecommendationViewModel vm)
                vm.ApplyAuditFilter("All");
        }

        private void ClearAllQueueFilters_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SubsidyRecommendationViewModel vm)
            {
                vm.QueueSearchText = string.Empty;
                vm.ApplyAuditFilter("All");
            }
            if (TxtQueueSearch != null) TxtQueueSearch.Focus();
        }
    }
}