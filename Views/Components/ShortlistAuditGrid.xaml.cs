using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SOLUM_UI.Models;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Components
{
    public partial class ShortlistAuditGrid : UserControl
    {
        public ShortlistAuditGrid()
        {
            InitializeComponent();
            Loaded += (s, e) => AdjustColumnWidths();
        }

        private void ShortlistListView_SizeChanged(object sender, SizeChangedEventArgs e) => AdjustColumnWidths();

        private void AdjustColumnWidths()
        {
            double totalWidth = ShortlistListView.ActualWidth;
            if (totalWidth <= 0) return;

            double scrollMargin = GetScrollMargin();
            double fixedWidth = 36 + 46 + 76 + 110 + 95 + 90 + 170; // 623px
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
