using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using SOLUM_UI.Models;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Components
{
    public partial class ShortlistAuditGrid : UserControl
    {
        private readonly DispatcherTimer _scrollHintTimer;

        public ShortlistAuditGrid()
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
                UpdateBatchColumn();
                AdjustColumnWidths();
            };
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    UpdateBatchColumn();
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
                UpdateBatchColumn();
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SubsidyRecommendationViewModel.IsShortlistBatchMode))
                UpdateBatchColumn();
        }

        private void UpdateBatchColumn()
        {
            if (DataContext is SubsidyRecommendationViewModel vm)
                ColSelect.Width = vm.IsShortlistBatchMode ? 34 : 0;
            else
                ColSelect.Width = 0;
            AdjustColumnWidths();
        }

        private void ShortlistListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustColumnWidths();
        }

        private void ShortlistListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var sv = e.OriginalSource as ScrollViewer ?? FindVisualChild<ScrollViewer>(ShortlistListView);
            if (sv != null)
            {
                // Infinite scrolling: trigger fetch of next batch when approaching bottom of current loaded items
                if (e.VerticalChange > 0 && sv.ScrollableHeight > 0)
                {
                    bool nearBottom = sv.VerticalOffset >= (sv.ScrollableHeight - 5) ||
                                      (sv.VerticalOffset + sv.ViewportHeight >= sv.ExtentHeight - 120);

                    if (nearBottom && DataContext is SubsidyRecommendationViewModel vm && vm.CanShortlistLoadMore)
                    {
                        _ = vm.LoadNextShortlistPageAsync();
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
            double totalWidth = ShortlistListView.ActualWidth;
            if (totalWidth <= 0) return;

            // Reserve room for vertical scrollbar and prevent horizontal overflow
            double scrollMargin = 20.0;
            double selW = (ColSelect != null && ColSelect.Width > 0) ? 34.0 : 0.0;
            if (ColSelect != null) ColSelect.Width = selW;

            double rankW = 58.0;
            if (ColRank != null) ColRank.Width = rankW;

            double scoreW = 62.0;
            if (ColScore != null) ColScore.Width = scoreW;

            double crossW = 115.0;
            if (ColCrossCheck != null) ColCrossCheck.Width = crossW;

            double depW = 60.0;
            if (ColDependents != null) ColDependents.Width = depW;

            double cardStatusW = 74.0;
            if (ColCardStatus != null) ColCardStatus.Width = cardStatusW;

            double validW = 82.0;
            if (ColValidUntil != null) ColValidUntil.Width = validW;

            double subStatusW = 100.0;
            if (ColSubsidyStatus != null) ColSubsidyStatus.Width = subStatusW;

            double actionW = 140.0;
            if (ColAction != null) ColAction.Width = actionW;

            double fixedWidth = selW + rankW + scoreW + crossW + depW + cardStatusW + validW + subStatusW + actionW;
            double availableFlexible = Math.Max(100.0, totalWidth - fixedWidth - scrollMargin);

            double wName = Math.Floor(availableFlexible * 0.38);
            double wCompliance = Math.Max(40.0, availableFlexible - wName);

            if (ColName != null) ColName.Width = wName;
            if (ColCompliance != null) ColCompliance.Width = wCompliance;
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

        private SelectionRow _lastClickedShortlistRow;

        private void Row_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is SubsidyRecommendationViewModel vm && vm.IsShortlistBatchMode)
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
                        if (sender is ListViewItem lviBox && lviBox.DataContext is SelectionRow rowBox)
                        {
                            _lastClickedShortlistRow = rowBox;
                        }
                        return;
                    }
                }
                if (sender is ListViewItem lvi && lvi.DataContext is SelectionRow row)
                {
                    bool isShiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                    if (isShiftDown && _lastClickedShortlistRow != null && vm.FilteredShortlistRows != null)
                    {
                        int fromIndex = vm.FilteredShortlistRows.IndexOf(_lastClickedShortlistRow);
                        int toIndex = vm.FilteredShortlistRows.IndexOf(row);
                        if (fromIndex >= 0 && toIndex >= 0)
                        {
                            int start = Math.Min(fromIndex, toIndex);
                            int end = Math.Max(fromIndex, toIndex);
                            bool targetState = !row.IsSelected;
                            for (int i = start; i <= end; i++)
                            {
                                vm.FilteredShortlistRows[i].IsSelected = targetState;
                            }
                            _lastClickedShortlistRow = row;
                            vm.RecalculateShortlistSelectionCounts();
                            e.Handled = true;
                            return;
                        }
                    }
                    row.IsSelected = !row.IsSelected;
                    _lastClickedShortlistRow = row;
                    vm.RecalculateShortlistSelectionCounts();
                    e.Handled = true;
                }
            }
        }

        private void Row_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is SelectionRow row && DataContext is SubsidyRecommendationViewModel vm)
            {
                vm.OpenExplainDrawerCommand.Execute(row);
                e.Handled = true;
            }
        }

        private void RowCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SubsidyRecommendationViewModel vm)
                vm.RecalculateShortlistSelectionCounts();
        }

        private void FilterDropBtn_Click(object sender, RoutedEventArgs e) => FilterPopup.IsOpen = !FilterPopup.IsOpen;

        private void FilterOpt_Click(object sender, MouseButtonEventArgs e)
        {
            FilterPopup.IsOpen = false;
            if (sender is FrameworkElement elem && elem.Tag is string tag && DataContext is SubsidyRecommendationViewModel vm)
                vm.ApplyShortlistAuditFilter(tag);
        }

        private void ExportDropBtn_Click(object sender, RoutedEventArgs e) => ExportPopup.IsOpen = !ExportPopup.IsOpen;

        private void ExportCityEducOpt_Click(object sender, MouseButtonEventArgs e)
        {
            ExportPopup.IsOpen = false;
            if (DataContext is SubsidyRecommendationViewModel vm) vm.ExportCityEducCommand.Execute(null);
        }

        private void ExportPantawidOpt_Click(object sender, MouseButtonEventArgs e)
        {
            ExportPopup.IsOpen = false;
            if (DataContext is SubsidyRecommendationViewModel vm) vm.ExportPantawid4PsCommand.Execute(null);
        }
    }
}