using System;
using System.Windows;
using System.Windows.Controls;

namespace SOLUM_UI.Views.Components
{
    public partial class FinalGranteesArchiveGrid : UserControl
    {
        public FinalGranteesArchiveGrid()
        {
            InitializeComponent();
            Loaded += (s, e) => AdjustColumnWidths();
        }

        private void FinalGranteesListView_SizeChanged(object sender, SizeChangedEventArgs e) => AdjustColumnWidths();

        private void AdjustColumnWidths()
        {
            double totalWidth = FinalGranteesListView.ActualWidth;
            if (totalWidth <= 0) return;

            // Fixed columns: ID(140) + SEX(55) + CIVIL STATUS(95) + DOB(95) + LAST UPDATED(95) + VALID UNTIL(95) + STATUS(80) + ACTIONS(90) = 745px
            double fixedWidth = 140 + 55 + 95 + 95 + 95 + 95 + 80 + 90;
            double flexibleWidth = totalWidth - fixedWidth - 25; // 25px scrollbar/border buffer
            if (flexibleWidth > 150)
            {
                ColName.Width = Math.Max(160, flexibleWidth * 0.58);
                ColBarangay.Width = Math.Max(110, flexibleWidth * 0.42);
            }
        }
    }
}
