using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Components
{
    public partial class RecommendationQueueGrid : UserControl
    {
        private readonly DispatcherTimer _resizeDebounceTimer;

        public RecommendationQueueGrid()
        {
            InitializeComponent();
            _resizeDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _resizeDebounceTimer.Tick += (s, e) =>
            {
                _resizeDebounceTimer.Stop();
                UpdateDynamicPageSize();
            };

            DataContextChanged += OnDataContextChanged;
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

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SubsidyRecommendationViewModel oldVm)
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            if (e.NewValue is SubsidyRecommendationViewModel newVm)
            {
                newVm.PropertyChanged += OnViewModelPropertyChanged;
                UpdateBatchColumn(newVm.IsBatchMode);
                UpdateDynamicPageSize();
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SubsidyRecommendationViewModel.IsBatchMode))
            {
                if (DataContext is SubsidyRecommendationViewModel vm)
                    UpdateBatchColumn(vm.IsBatchMode);
            }
        }

        private void UpdateBatchColumn(bool isBatch)
        {
            ColSelect.Width = isBatch ? 38 : 0;
            AdjustColumnWidths();
        }

        private void RowCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SubsidyRecommendationViewModel vm)
                vm.RecalculateSelectionCounts();
        }

        private void QueueListView_SizeChanged(object sender, SizeChangedEventArgs e)
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
            double availableHeight = QueueListView.ActualHeight;
            if (availableHeight <= 100) return;

            // GridViewColumnHeader is 46px, each candidate row is ~58px
            double rowsHeight = availableHeight - 46.0;
            int calculated = (int)Math.Floor(rowsHeight / 58.0);
            int optimal = Math.Max(4, Math.Min(35, calculated));

            if (DataContext is SubsidyRecommendationViewModel vm && optimal != vm.QueuePageSize && !vm.IsLoading)
            {
                _ = vm.UpdateQueuePageSizeAsync(optimal);
            }
        }

        private void AdjustColumnWidths()
        {
            double totalWidth = QueueListView.ActualWidth;
            if (totalWidth <= 0) return;

            double scrollMargin = GetScrollMargin();
            double fixedWidth = ColSelect.Width + ColRank.Width + 76 + 145; // ColRank + Score(76) + Actions(145)
            double flexibleWidth = Math.Max(500, totalWidth - fixedWidth - scrollMargin);

            double wName = Math.Max(140, Math.Floor(flexibleWidth * 0.23));
            double wIncome = Math.Max(115, Math.Floor(flexibleWidth * 0.17));
            double wCare = Math.Max(120, Math.Floor(flexibleWidth * 0.18));
            double wCirc = Math.Max(140, Math.Floor(flexibleWidth * 0.22));
            double wNeeds = Math.Max(130, flexibleWidth - (wName + wIncome + wCare + wCirc));

            ColName.Width = wName;
            ColIncome.Width = wIncome;
            ColCare.Width = wCare;
            ColCircumstance.Width = wCirc;
            ColNeeds.Width = wNeeds;
        }

        private double GetScrollMargin()
        {
            var sv = FindVisualChild<ScrollViewer>(QueueListView);
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

        private void Row_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is Models.SubsidyItem candidate && DataContext is SubsidyRecommendationViewModel vm)
            {
                vm.OpenExplainDrawerCommand.Execute(candidate);
                e.Handled = true;
            }
        }
    }
}
