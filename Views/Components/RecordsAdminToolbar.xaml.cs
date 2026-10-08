using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Components
{
    public partial class RecordsAdminToolbar : UserControl
    {
        // ?? DependencyProperties ?????????????????????????????????????????????
        public static readonly DependencyProperty SearchIdTextProperty =
            DependencyProperty.Register(nameof(SearchIdText), typeof(string), typeof(RecordsAdminToolbar), new PropertyMetadata(string.Empty));
        public string SearchIdText { get => (string)GetValue(SearchIdTextProperty); set => SetValue(SearchIdTextProperty, value); }

        public static readonly DependencyProperty SearchNameTextProperty =
            DependencyProperty.Register(nameof(SearchNameText), typeof(string), typeof(RecordsAdminToolbar), new PropertyMetadata(string.Empty));
        public string SearchNameText { get => (string)GetValue(SearchNameTextProperty); set => SetValue(SearchNameTextProperty, value); }

        public static readonly DependencyProperty SearchBarangayTextProperty =
            DependencyProperty.Register(nameof(SearchBarangayText), typeof(string), typeof(RecordsAdminToolbar), new PropertyMetadata(string.Empty));
        public string SearchBarangayText { get => (string)GetValue(SearchBarangayTextProperty); set => SetValue(SearchBarangayTextProperty, value); }

        public static readonly DependencyProperty IsClearSearchVisibleProperty =
            DependencyProperty.Register(nameof(IsClearSearchVisible), typeof(Visibility), typeof(RecordsAdminToolbar), new PropertyMetadata(Visibility.Collapsed));
        public Visibility IsClearSearchVisible { get => (Visibility)GetValue(IsClearSearchVisibleProperty); set => SetValue(IsClearSearchVisibleProperty, value); }

        public static readonly DependencyProperty IsRefreshEnabledProperty =
            DependencyProperty.Register(nameof(IsRefreshEnabled), typeof(bool), typeof(RecordsAdminToolbar), new PropertyMetadata(true));
        public bool IsRefreshEnabled { get => (bool)GetValue(IsRefreshEnabledProperty); set => SetValue(IsRefreshEnabledProperty, value); }

        public static readonly DependencyProperty SearchCommandProperty =
            DependencyProperty.Register(nameof(SearchCommand), typeof(ICommand), typeof(RecordsAdminToolbar));
        public ICommand SearchCommand { get => (ICommand)GetValue(SearchCommandProperty); set => SetValue(SearchCommandProperty, value); }

        public static readonly DependencyProperty ClearSearchCommandProperty =
            DependencyProperty.Register(nameof(ClearSearchCommand), typeof(ICommand), typeof(RecordsAdminToolbar));
        public ICommand ClearSearchCommand { get => (ICommand)GetValue(ClearSearchCommandProperty); set => SetValue(ClearSearchCommandProperty, value); }

        public static readonly DependencyProperty AddRecordCommandProperty =
            DependencyProperty.Register(nameof(AddRecordCommand), typeof(ICommand), typeof(RecordsAdminToolbar));
        public ICommand AddRecordCommand { get => (ICommand)GetValue(AddRecordCommandProperty); set => SetValue(AddRecordCommandProperty, value); }

        public static readonly DependencyProperty RefreshCommandProperty =
            DependencyProperty.Register(nameof(RefreshCommand), typeof(ICommand), typeof(RecordsAdminToolbar));
        public ICommand RefreshCommand { get => (ICommand)GetValue(RefreshCommandProperty); set => SetValue(RefreshCommandProperty, value); }

        public RecordsAdminToolbar()
        {
            InitializeComponent();
        }

        private void FilterDrop_Click(object sender, RoutedEventArgs e) => FilterPopup.IsOpen = true;
        private void SortDrop_Click(object sender, RoutedEventArgs e)   => SortPopup.IsOpen   = true;

        private void FilterApply_Click(object sender, RoutedEventArgs e)
        {
            string sex      = (CmbFilterSex?.SelectedItem      as ComboBoxItem)?.Content?.ToString() ?? "All";
            string barangay = (CmbFilterBarangay?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
            string status   = (CmbFilterStatus?.SelectedItem   as ComboBoxItem)?.Content?.ToString() ?? "All";
            FilterPopup.IsOpen = false;

            // Push to parent page's ViewModel via DataContext
            var vm = GetVm();
            if (vm == null) return;
            vm.SexFilter      = sex;
            vm.BarangayFilter = barangay;
            vm.StatusFilter   = status;
            vm.ApplyFilterCommand?.Execute(new FilterArgs { Sex = sex, Barangay = barangay, Status = status });
        }

        private void FilterReset_Click(object sender, RoutedEventArgs e)
        {
            if (CmbFilterSex      != null) CmbFilterSex.SelectedIndex      = 0;
            if (CmbFilterBarangay != null) CmbFilterBarangay.SelectedIndex = 0;
            if (CmbFilterStatus   != null) CmbFilterStatus.SelectedIndex   = 0;
            FilterPopup.IsOpen = false;
            GetVm()?.ResetFilterCommand?.Execute(null);
        }

        private void SortOpt_Click(object sender, MouseButtonEventArgs e)
        {
            string tag = (sender as FrameworkElement)?.Tag?.ToString() ?? "Name A-Z";
            SortPopup.IsOpen = false;
            GetVm()?.SortCommand?.Execute(tag);
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SearchCommand?.Execute(null);
        }

        private SoloParentRecordsViewModel GetVm()
        {
            var page = Window.GetWindow(this);
            return (page?.Content as System.Windows.Controls.Page)?.DataContext as SoloParentRecordsViewModel
                ?? DataContext as SoloParentRecordsViewModel;
        }
    }
}
