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

        private void UpdateDynamicPageSize()
        {
            double availableHeight = ShortlistListView.ActualHeight;
            if (availableHeight <= 100) return;

            // GridViewColumnHeader is 46px, each shortlist candidate row is ~58px
            double rowsHeight = availableHeight - 46.0;
            int calculated = (int)Math.Floor(rowsHeight / 58.0);
            int optimal = Math.Max(4, Math.Min(35, calculated));

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
            double fixedWidth = ColSelect.Width + ColRank.Width + 76 + 110 + 95 + 90 + 150;
            double flexibleWidth = Math.Max(300, totalWidth - fixedWidth - scrollMargin);

            double wName = Math.Max(140, Math.Floor(flexibleWidth * 0.35));
            double wCompliance = Math.Max(200, flexibleWidth - wName);

            ColName.Width = wName;
            ColCompliance.Width = wCompliance;
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
