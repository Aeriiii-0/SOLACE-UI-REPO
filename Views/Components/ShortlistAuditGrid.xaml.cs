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
        }

        private void ShortlistListView_SizeChanged(object sender, SizeChangedEventArgs e) => AdjustColumnWidths();

        private void AdjustColumnWidths()
        {
            double totalWidth = ShortlistListView.ActualWidth;
            if (totalWidth <= 0) return;

            double fixedWidth = 36 + 46 + 76 + 64 + 110 + 95 + 90 + 170;
            double flexibleWidth = totalWidth - fixedWidth - 25;
            if (flexibleWidth > 200)
            {
                ColName.Width = Math.Max(130, flexibleWidth * 0.32);
                ColCompliance.Width = Math.Max(240, flexibleWidth * 0.68);
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
