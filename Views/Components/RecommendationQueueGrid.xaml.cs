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
            if (!isBatch && QueueListView != null)
            {
                QueueListView.SelectedItem = null;
            }
            AdjustColumnWidths();
        }

        private void RowCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is Models.SubsidyItem item && DataContext is SubsidyRecommendationViewModel vm)
            {
                vm.SyncQueueItemSelection(item, cb.IsChecked == true);
            }
            else if (DataContext is SubsidyRecommendationViewModel vm2)
            {
                vm2.RecalculateSelectionCounts();
            }
        }

        private void QueueListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged || e.PreviousSize.Width <= 0)
            {
                AdjustColumnWidths();
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(AdjustColumnWidths));
            }
        }

        private void QueueListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var sv = e.OriginalSource as ScrollViewer ?? FindVisualChild<ScrollViewer>(QueueListView);
            if (sv == null) return;

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

            // Infinite scrolling: load next batch when scrolled near bottom (within 45px)
            if (sv.ScrollableHeight > 0 && sv.VerticalOffset >= sv.ScrollableHeight - 45)
            {
                if (DataContext is SubsidyRecommendationViewModel vm && vm.CanQueueLoadMore && !vm.IsLoadingMoreQueue)
                {
                    _ = vm.LoadNextQueuePageAsync();
                }
            }
        }

        private void AdjustColumnWidths()
        {
            double totalWidth = QueueListView.ActualWidth;
            if (totalWidth <= 0) return;

            double scrollMargin = GetScrollMargin();
            double selW = ColSelect != null ? ColSelect.Width : 0;
            double rankW = ColRank != null ? ColRank.Width : 56;
            double scoreW = ColScore != null ? ColScore.Width : 78;
            double depW = ColDependents != null ? ColDependents.Width : 95;
            double cardStatusW = ColCardStatus != null ? ColCardStatus.Width : 90;
            double validUntilW = ColValidUntil != null ? ColValidUntil.Width : 95;
            double actionW = 177;
            if (ColAction != null) ColAction.Width = actionW;

            double fixedWidth = selW + rankW + scoreW + depW + cardStatusW + validUntilW + actionW;
            double minFlexible = 420;
            double availableFlexible = totalWidth - fixedWidth - scrollMargin;
            double flexibleWidth = Math.Max(minFlexible, availableFlexible);

            double wName = Math.Max(110, Math.Floor(flexibleWidth * 0.22));
            double wIncome = Math.Max(85, Math.Floor(flexibleWidth * 0.17));
            double wCare = Math.Max(95, Math.Floor(flexibleWidth * 0.19));
            double wCirc = Math.Max(95, Math.Floor(flexibleWidth * 0.20));
            double wNeeds = Math.Max(95, flexibleWidth - (wName + wIncome + wCare + wCirc));
            if (ColName != null) ColName.Width = wName;
            if (ColIncome != null) ColIncome.Width = wIncome;
            if (ColCare != null) ColCare.Width = wCare;
            if (ColCircumstance != null) ColCircumstance.Width = wCirc;
            if (ColNeeds != null) ColNeeds.Width = wNeeds;
        }

        private double GetScrollMargin()
        {
            var sv = FindVisualChild<ScrollViewer>(QueueListView);
            return (sv != null && sv.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                ? SystemParameters.VerticalScrollBarWidth
                : 2.0;
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
            if (DataContext is SubsidyRecommendationViewModel vm)
            {
                if (vm.IsBatchMode)
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
                        var sourceList = vm.PagedRecommendations ?? vm.FilteredRecommendations;
                        if (isShiftDown && _lastClickedQueueItem != null && sourceList != null)
                        {
                            int fromIndex = sourceList.IndexOf(_lastClickedQueueItem);
                            int toIndex = sourceList.IndexOf(item);
                            if (fromIndex >= 0 && toIndex >= 0)
                            {
                                int start = Math.Min(fromIndex, toIndex);
                                int end = Math.Max(fromIndex, toIndex);
                                bool targetState = !item.IsSelected;
                                for (int i = start; i <= end; i++)
                                {
                                    vm.SyncQueueItemSelection(sourceList[i], targetState);
                                }
                                _lastClickedQueueItem = item;
                                e.Handled = true;
                                return;
                            }
                        }
                        vm.SyncQueueItemSelection(item, !item.IsSelected);
                        _lastClickedQueueItem = item;
                        e.Handled = true;
                    }
                }
                else
                {
                    _lastClickedQueueItem = null;
                    var dep = e.OriginalSource as DependencyObject;
                    if (dep != null && (FindVisualParent<Button>(dep) != null || FindVisualParent<CheckBox>(dep) != null))
                    {
                        return;
                    }
                    if (sender is ListViewItem lvi && lvi.DataContext is Models.SubsidyItem item)
                    {
                        var sourceList = vm.PagedRecommendations ?? vm.FilteredRecommendations;
                        if (sourceList != null)
                        {
                            foreach (var it in sourceList)
                            {
                                if (it != item && it.IsSelected) vm.SyncQueueItemSelection(it, false);
                            }
                        }
                        vm.SyncQueueItemSelection(item, false);

                        if (vm.IsDrawerOpen)
                        {
                            vm.OpenExplainDrawerCommand.Execute(item);
                        }
                    }
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

        private void TxtQueueSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (DataContext is SubsidyRecommendationViewModel vm)
                {
                    vm.ExecuteQueueBackendSearchCommand?.Execute(null);
                }
                e.Handled = true;
            }
        }

        private void ClearQueueSearch_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SubsidyRecommendationViewModel vm)
            {
                vm.QueueSearchText = string.Empty;
                vm.ExecuteQueueBackendSearchCommand?.Execute(null);
            }
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