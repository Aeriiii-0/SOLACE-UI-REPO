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

            double fixedWidth = 48 + 80 + 68 + 125 + 95 + 108;
            double flexibleWidth = totalWidth - fixedWidth - 25;
            if (flexibleWidth > 200)
            {
                ColName.Width = Math.Max(140, flexibleWidth * 0.35);
                ColCompliance.Width = Math.Max(220, flexibleWidth * 0.65);
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
    }
}
