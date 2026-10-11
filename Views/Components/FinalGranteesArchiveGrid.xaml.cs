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

            double scrollMargin = GetScrollMargin();
            // Fixed columns: CONTACT(130) + EM_PHONE(130) + STATUS(85) + ACTIONS(90) = 435px
            double fixedWidth = 130 + 130 + 85 + 90;
            double flexibleWidth = Math.Max(450, totalWidth - fixedWidth - scrollMargin);

            double wName = Math.Max(150, Math.Floor(flexibleWidth * 0.28));
            double wBarangay = Math.Max(110, Math.Floor(flexibleWidth * 0.18));
            double wAddress = Math.Max(150, Math.Floor(flexibleWidth * 0.30));
            double wEmergencyName = Math.Max(130, flexibleWidth - (wName + wBarangay + wAddress));

            ColName.Width = wName;
            ColBarangay.Width = wBarangay;
            ColAddress.Width = wAddress;
            ColEmergencyName.Width = wEmergencyName;
        }

        private double GetScrollMargin()
        {
            var sv = FindVisualChild<ScrollViewer>(FinalGranteesListView);
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
    }
}
