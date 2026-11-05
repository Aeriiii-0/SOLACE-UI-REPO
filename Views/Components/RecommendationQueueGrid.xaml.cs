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
        private readonly DispatcherTimer _resizeDebounceTimer;
        private readonly DispatcherTimer _scrollHintTimer;

        public RecommendationQueueGrid()
        {
            InitializeComponent();
            _resizeDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _resizeDebounceTimer.Tick += (s, e) =>
            {
                _resizeDebounceTimer.Stop();
                UpdateDynamicPageSize();
            };
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
                UpdateDynamicPageSize();
            };
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    AdjustColumnWidths();
                    UpdateDynamicPageSize();
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
                UpdateDynamicPageSize();
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
            ColSelect.Width = isBatch ? 38 : 0;
            AdjustColumnWidths();
        }

        private void RowCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SubsidyRecommendationViewModel vm)
                vm.RecalculateSelectionCounts();
        }

        private void QueueListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
                AdjustColumnWidths();
            if (e.HeightChanged)
            {
                _resizeDebounceTimer.Stop();
                _resizeDebounceTimer.Start();
            }
        }

        private void QueueListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (ScrollHintIndicator == null) return;
            if (Math.Abs(e.VerticalChange) > 0.01)
            {
                var sv = e.OriginalSource as ScrollViewer ?? FindVisualChild<ScrollViewer>(QueueListView);
                if (sv != null && sv.ScrollableHeight > 0 && sv.VerticalOffset < sv.ScrollableHeight - 8)
                {
                    ScrollHintIndicator.Visibility = Visibility.Visible;
                    _scrollHintTimer.Stop();
                    _scrollHintTimer.Start();
                    return;
                }
            }
            var scrollV = e.OriginalSource as ScrollViewer ?? FindVisualChild<ScrollViewer>(QueueListView);
            if (scrollV != null && (scrollV.ScrollableHeight <= 0 || scrollV.VerticalOffset >= scrollV.ScrollableHeight - 8))
            {
                _scrollHintTimer.Stop();
                ScrollHintIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateDynamicPageSize()
        {
            // Continuous virtualized scrolling active across all candidate records.
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
            double actionW = 145;
            if (ColAction != null) ColAction.Width = actionW;

            double fixedWidth = selW + rankW + scoreW + depW + cardStatusW + validUntilW + actionW;
            double minFlexible = 650;
            double availableFlexible = totalWidth - fixedWidth - scrollMargin;
            double flexibleWidth = Math.Max(minFlexible, availableFlexible);

            double wName = Math.Max(140, Math.Floor(flexibleWidth * 0.22));
            double wIncome = Math.Max(115, Math.Floor(flexibleWidth * 0.17));
            double wCare = Math.Max(125, Math.Floor(flexibleWidth * 0.19));
            double wCirc = Math.Max(130, Math.Floor(flexibleWidth * 0.20));
            double wNeeds = Math.Max(140, flexibleWidth - (wName + wIncome + wCare + wCirc));
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
    }
}