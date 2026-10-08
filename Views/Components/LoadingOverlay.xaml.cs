using System.Windows;
using System.Windows.Controls;

namespace SOLUM_UI.Views.Components
{
    public partial class LoadingOverlay : UserControl
    {
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(LoadingOverlay),
                new PropertyMetadata(false, OnIsLoadingChanged));

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var overlay = (LoadingOverlay)d;
            bool loading = (bool)e.NewValue;
            overlay.Visibility  = loading ? Visibility.Visible : Visibility.Collapsed;
            overlay.IsHitTestVisible = loading;
        }

        public LoadingOverlay()
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed;
        }
    }
}
