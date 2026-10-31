using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Components
{
    public partial class FinalGranteesArchiveGrid : UserControl
    {
        private readonly DispatcherTimer _resizeDebounceTimer;

        public FinalGranteesArchiveGrid()
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

            DataContextChanged += (s, e) => UpdateDynamicPageSize();
            Loaded += (s, e) =>
            {
                AdjustColumnWidths();
                UpdateDynamicPageSize();
            };
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    AdjustColumnWidths();
                    UpdateDynamicPageSize();
                }
            };
        }

        private void FinalGranteesListView_SizeChanged(object sender, SizeChangedEventArgs e)
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
            double availableHeight = FinalGranteesListView.ActualHeight;
            if (availableHeight <= 100) return;

            // GridViewColumnHeader is 46px, each final grantee row is ~48px
            double rowsHeight = availableHeight - 46.0;
            int calculated = (int)Math.Floor(rowsHeight / 48.0);
            int optimal = Math.Max(4, Math.Min(40, calculated));

            if (DataContext is SubsidyRecommendationViewModel vm && optimal != vm.FinalPageSize)
            {
                vm.UpdateFinalPageSize(optimal);
            }
        }

        private void AdjustColumnWidths()
        {
            double totalWidth = FinalGranteesListView.ActualWidth;
            if (totalWidth <= 0) return;

            double scrollMargin = GetScrollMargin();
            // Fixed columns: CONTACT(130) + EM_PHONE(130) + STATUS(90) = 350px
            double fixedWidth = 130 + 130 + 90;
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
