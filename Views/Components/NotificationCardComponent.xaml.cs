using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SOLUM_UI.Views.Components
{
    public partial class NotificationCardComponent : UserControl
    {
        // ── Dependency Properties ─────────────────────────────────────────────
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string),
                typeof(NotificationCardComponent), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string),
                typeof(NotificationCardComponent), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsSuccessProperty =
            DependencyProperty.Register(nameof(IsSuccess), typeof(bool),
                typeof(NotificationCardComponent),
                new PropertyMetadata(true, OnIsSuccessChanged));

        public string Title   { get => (string)GetValue(TitleProperty);   set => SetValue(TitleProperty, value); }
        public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
        public bool   IsSuccess { get => (bool)GetValue(IsSuccessProperty); set => SetValue(IsSuccessProperty, value); }

        // ── Colours ───────────────────────────────────────────────────────────
        private static readonly SolidColorBrush SuccessDot    = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));
        private static readonly SolidColorBrush SuccessBorder = new SolidColorBrush(Color.FromRgb(0xD5, 0xF5, 0xE3));
        private static readonly SolidColorBrush FailDot       = new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C));
        private static readonly SolidColorBrush FailBorder    = new SolidColorBrush(Color.FromRgb(0xFA, 0xDB, 0xD8));

        public NotificationCardComponent() => InitializeComponent();

        // ── State callback ────────────────────────────────────────────────────
        private static void OnIsSuccessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NotificationCardComponent c) c.ApplyTheme((bool)e.NewValue);
        }

        private void ApplyTheme(bool success)
        {
            DotBadge.Background    = success ? SuccessDot    : FailDot;
            CardBorder.BorderBrush = success ? SuccessBorder : FailBorder;
        }

        private void Dismiss_Click(object sender, RoutedEventArgs e)
            => Visibility = Visibility.Collapsed;
    }
}
