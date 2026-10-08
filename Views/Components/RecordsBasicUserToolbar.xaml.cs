using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SOLUM_UI.Views.Components
{
    public partial class RecordsBasicUserToolbar : UserControl
    {
        public static readonly DependencyProperty BasicSearchNameTextProperty =
            DependencyProperty.Register(nameof(BasicSearchNameText), typeof(string), typeof(RecordsBasicUserToolbar), new PropertyMetadata(string.Empty));
        public string BasicSearchNameText { get => (string)GetValue(BasicSearchNameTextProperty); set => SetValue(BasicSearchNameTextProperty, value); }

        public static readonly DependencyProperty SearchCommandProperty =
            DependencyProperty.Register(nameof(SearchCommand), typeof(ICommand), typeof(RecordsBasicUserToolbar));
        public ICommand SearchCommand { get => (ICommand)GetValue(SearchCommandProperty); set => SetValue(SearchCommandProperty, value); }

        public static readonly DependencyProperty AddRecordCommandProperty =
            DependencyProperty.Register(nameof(AddRecordCommand), typeof(ICommand), typeof(RecordsBasicUserToolbar));
        public ICommand AddRecordCommand { get => (ICommand)GetValue(AddRecordCommandProperty); set => SetValue(AddRecordCommandProperty, value); }

        public RecordsBasicUserToolbar()
        {
            InitializeComponent();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SearchCommand?.Execute(null);
        }
    }
}
