using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Components
{
    public partial class RecommendationQueueGrid : UserControl
    {
        public RecommendationQueueGrid()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += (s, e) => AdjustColumnWidths();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SubsidyRecommendationViewModel oldVm)
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            if (e.NewValue is SubsidyRecommendationViewModel newVm)
            {
                newVm.PropertyChanged += OnViewModelPropertyChanged;
                UpdateBatchColumn(newVm.IsBatchMode);
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

        private void QueueListView_SizeChanged(object sender, SizeChangedEventArgs e) => AdjustColumnWidths();

        private void AdjustColumnWidths()
        {
            double totalWidth = QueueListView.ActualWidth;
            if (totalWidth <= 0) return;

            double scrollMargin = GetScrollMargin();
            double fixedWidth = ColSelect.Width + 48 + 76 + 120 + 118; // Rank(48) + Score(76) + CrossCheck(120) + Actions(118)
            double flexibleWidth = Math.Max(500, totalWidth - fixedWidth - scrollMargin);

            double wName = Math.Max(130, Math.Floor(flexibleWidth * 0.22));
            double wIncome = Math.Max(110, Math.Floor(flexibleWidth * 0.18));
            double wCare = Math.Max(115, Math.Floor(flexibleWidth * 0.18));
            double wCirc = Math.Max(130, Math.Floor(flexibleWidth * 0.21));
            double wNeeds = Math.Max(120, flexibleWidth - (wName + wIncome + wCare + wCirc));

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
