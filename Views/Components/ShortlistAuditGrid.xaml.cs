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
            {
                ColSelect.Width = vm.IsShortlistBatchMode ? 38 : 0;
                if (!vm.IsShortlistBatchMode && ShortlistListView != null)
                {
                    ShortlistListView.SelectedItem = null;
                }
            }
            else
            {
                ColSelect.Width = 0;
            }
            AdjustColumnWidths();
        }

        private void ShortlistListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged || e.PreviousSize.Width <= 0)
            {
                AdjustColumnWidths();
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(AdjustColumnWidths));
            }
        }

        private void ShortlistListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var sv = e.OriginalSource as ScrollViewer ?? FindVisualChild<ScrollViewer>(ShortlistListView);
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
                if (DataContext is SubsidyRecommendationViewModel vm && vm.CanShortlistLoadMore && !vm.IsLoadingMoreShortlist)
                {
                    _ = vm.LoadNextShortlistPageAsync();
                }
            }
        }

        private void AdjustColumnWidths()
        {
            double totalWidth = ShortlistListView.ActualWidth;
            if (totalWidth <= 0) return;

            double scrollMargin = GetScrollMargin();
            double selW = ColSelect != null ? ColSelect.Width : 0;
            double rankW = ColRank != null ? ColRank.Width : 56;
            double scoreW = ColScore != null ? ColScore.Width : 78;
            double crossW = ColCrossCheck != null ? ColCrossCheck.Width : 135;
            double depW = ColDependents != null ? ColDependents.Width : 95;
            double validW = ColValidUntil != null ? ColValidUntil.Width : 95;
            double cardStatusW = ColCardStatus != null ? ColCardStatus.Width : 90;
            double subStatusW = ColSubsidyStatus != null ? ColSubsidyStatus.Width : 110;
            double actionW = 150;
            if (ColAction != null) ColAction.Width = actionW;

            double fixedWidth = selW + rankW + scoreW + crossW + depW + validW + cardStatusW + subStatusW + actionW;
            double minFlexible = 260;
            double availableFlexible = totalWidth - fixedWidth - scrollMargin;
            double flexibleWidth = Math.Max(minFlexible, availableFlexible);

            double wName = Math.Max(110, Math.Floor(flexibleWidth * 0.38));
            double wCompliance = Math.Max(150, flexibleWidth - wName);
            if (ColName != null) ColName.Width = wName;
            if (ColCompliance != null) ColCompliance.Width = wCompliance;
        }

        private double GetScrollMargin()
        {
            var sv = FindVisualChild<ScrollViewer>(ShortlistListView);
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

        private SelectionRow _lastClickedShortlistRow;

        private void Row_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is SubsidyRecommendationViewModel vm)
            {
                if (vm.IsShortlistBatchMode)
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
                        var sourceList = vm.PagedShortlistRows ?? vm.FilteredShortlistRows;
                        if (isShiftDown && _lastClickedShortlistRow != null && sourceList != null)
                        {
                            int fromIndex = sourceList.IndexOf(_lastClickedShortlistRow);
                            int toIndex = sourceList.IndexOf(row);
                            if (fromIndex >= 0 && toIndex >= 0)
                            {
                                int start = Math.Min(fromIndex, toIndex);
                                int end = Math.Max(fromIndex, toIndex);
                                bool targetState = !row.IsSelected;
                                for (int i = start; i <= end; i++)
                                {
                                    vm.SyncShortlistRowSelection(sourceList[i], targetState);
                                }
                                _lastClickedShortlistRow = row;
                                e.Handled = true;
                                return;
                            }
                        }
                        vm.SyncShortlistRowSelection(row, !row.IsSelected);
                        _lastClickedShortlistRow = row;
                        e.Handled = true;
                    }
                }
                else
                {
                    _lastClickedShortlistRow = null;
                    var dep = e.OriginalSource as DependencyObject;
                    if (dep != null && (FindVisualParent<Button>(dep) != null || FindVisualParent<CheckBox>(dep) != null))
                    {
                        return;
                    }
                    if (sender is ListViewItem lvi && lvi.DataContext is SelectionRow row)
                    {
                        var sourceList = vm.PagedShortlistRows ?? vm.FilteredShortlistRows;
                        if (sourceList != null)
                        {
                            foreach (var r in sourceList)
                            {
                                if (r != row && r.IsSelected) vm.SyncShortlistRowSelection(r, false);
                            }
                        }
                        vm.SyncShortlistRowSelection(row, false);

                        if (vm.IsDrawerOpen)
                        {
                            vm.OpenExplainDrawerCommand.Execute(row);
                        }
                    }
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
            if (sender is CheckBox cb && cb.DataContext is SelectionRow row && DataContext is SubsidyRecommendationViewModel vm)
            {
                vm.SyncShortlistRowSelection(row, cb.IsChecked == true);
            }
            else if (DataContext is SubsidyRecommendationViewModel vm2)
            {
                vm2.RecalculateShortlistSelectionCounts();
            }
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