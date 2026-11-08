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
        private readonly DispatcherTimer _resizeDebounceTimer;
        private readonly DispatcherTimer _scrollHintTimer;

        public ShortlistAuditGrid()
        {
            InitializeComponent();
            _resizeDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
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
                UpdateBatchColumn();
                AdjustColumnWidths();
                UpdateDynamicPageSize();
            };
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    UpdateBatchColumn();
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
                UpdateBatchColumn();
                UpdateDynamicPageSize();
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
                ColSelect.Width = vm.IsShortlistBatchMode ? 38 : 0;
            else
                ColSelect.Width = 0;
            AdjustColumnWidths();
        }

        private void ShortlistListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
                AdjustColumnWidths();
            if (e.HeightChanged)
            {
                _resizeDebounceTimer.Stop();
                _resizeDebounceTimer.Start();
            }
        }

        private void ShortlistListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (ScrollHintIndicator == null) return;
            if (Math.Abs(e.VerticalChange) > 0.01)
            {
                var sv = e.OriginalSource as ScrollViewer ?? FindVisualChild<ScrollViewer>(ShortlistListView);
                if (sv != null && sv.ScrollableHeight > 0 && sv.VerticalOffset < sv.ScrollableHeight - 8)
                {
                    ScrollHintIndicator.Visibility = Visibility.Visible;
                    _scrollHintTimer.Stop();
                    _scrollHintTimer.Start();
                    return;
                }
            }
            var scrollV = e.OriginalSource as ScrollViewer ?? FindVisualChild<ScrollViewer>(ShortlistListView);
            if (scrollV != null && (scrollV.ScrollableHeight <= 0 || scrollV.VerticalOffset >= scrollV.ScrollableHeight - 8))
            {
                _scrollHintTimer.Stop();
                ScrollHintIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateDynamicPageSize()
        {
            double availableHeight = ShortlistListView.ActualHeight;
            if (availableHeight <= 100) return;
            // ColHeader is 46px, each shortlist row is ~48px
            double rowsHeight = availableHeight - 46.0;
            int calculated = (int)Math.Floor(rowsHeight / 48.0);
            int optimal = Math.Max(4, Math.Min(40, calculated));
            if (DataContext is SubsidyRecommendationViewModel vm && optimal != vm.ShortlistPageSize)
            {
                vm.UpdateShortlistPageSize(optimal);
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
            double minFlexible = 400;
            double availableFlexible = totalWidth - fixedWidth - scrollMargin;
            double flexibleWidth = Math.Max(minFlexible, availableFlexible);

            double wName = Math.Max(140, Math.Floor(flexibleWidth * 0.38));
            double wCompliance = Math.Max(220, flexibleWidth - wName);
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