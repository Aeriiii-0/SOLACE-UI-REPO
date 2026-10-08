using System.Windows;
using System.Windows.Controls;

namespace SOLUM_UI.Views.Components
{
    public partial class KpiCard : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(KpiCard), 
                new PropertyMetadata(0, OnValueChanged));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(KpiCard), 
                new PropertyMetadata("", OnLabelChanged));

        public static readonly DependencyProperty CardStyleProperty =
            DependencyProperty.Register("CardStyle", typeof(Style), typeof(KpiCard), 
                new PropertyMetadata(null, OnCardStyleChanged));

        public static readonly DependencyProperty ValueStyleProperty =
            DependencyProperty.Register("ValueStyle", typeof(Style), typeof(KpiCard), 
                new PropertyMetadata(null, OnValueStyleChanged));

        public static readonly DependencyProperty LabelStyleProperty =
            DependencyProperty.Register("LabelStyle", typeof(Style), typeof(KpiCard), 
                new PropertyMetadata(null, OnLabelStyleChanged));

        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public Style CardStyle
        {
            get => (Style)GetValue(CardStyleProperty);
            set => SetValue(CardStyleProperty, value);
        }

        public Style ValueStyle
        {
            get => (Style)GetValue(ValueStyleProperty);
            set => SetValue(ValueStyleProperty, value);
        }

        public Style LabelStyle
        {
            get => (Style)GetValue(LabelStyleProperty);
            set => SetValue(LabelStyleProperty, value);
        }

        public KpiCard()
        {
            InitializeComponent();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var card = (KpiCard)d;
            card.ValueTextBlock.Text = e.NewValue?.ToString() ?? "0";
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var card = (KpiCard)d;
            card.LabelTextBlock.Text = e.NewValue?.ToString() ?? "";
        }

        private static void OnCardStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var card = (KpiCard)d;
            if (e.NewValue is Style style)
            {
                card.CardBorder.Style = style;
            }
        }

        private static void OnValueStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var card = (KpiCard)d;
            if (e.NewValue is Style style)
            {
                card.ValueTextBlock.Style = style;
            }
        }

        private static void OnLabelStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var card = (KpiCard)d;
            if (e.NewValue is Style style)
            {
                card.LabelTextBlock.Style = style;
            }
        }
    }
}
