using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Components
{
    public partial class FinalGranteesArchiveGrid : UserControl
    {
        public FinalGranteesArchiveGrid()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                AdjustColumnWidths();
            };
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    AdjustColumnWidths();
                }
            };
        }

        private void FinalGranteesListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustColumnWidths();
        }

        private void AdjustColumnWidths()
        {
            double totalWidth = FinalGranteesListView.ActualWidth;
            if (totalWidth <= 0) return;

            double scrollMargin = 5.0;
            double wContact = 135.0;
            double wEmergencyPhone = 145.0;
            double wStatus = 100.0;
            if (ColContact != null) ColContact.Width = wContact;
            if (ColEmergencyPhone != null) ColEmergencyPhone.Width = wEmergencyPhone;
            if (ColStatus != null) ColStatus.Width = wStatus;

            double fixedWidth = wContact + wEmergencyPhone + wStatus;
            double availableFlexible = Math.Max(100.0, totalWidth - fixedWidth - scrollMargin);

            double wName = Math.Floor(availableFlexible * 0.28);
            double wBarangay = Math.Floor(availableFlexible * 0.18);
            double wAddress = Math.Floor(availableFlexible * 0.30);
            double wEmergencyName = Math.Max(20.0, availableFlexible - (wName + wBarangay + wAddress));

            if (ColName != null) ColName.Width = wName;
            if (ColBarangay != null) ColBarangay.Width = wBarangay;
            if (ColAddress != null) ColAddress.Width = wAddress;
            if (ColEmergencyName != null) ColEmergencyName.Width = wEmergencyName;
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
