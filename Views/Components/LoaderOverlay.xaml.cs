using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace SOLUM_UI.Views.Components
{
    public partial class LoaderOverlay : UserControl
    {
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(LoaderOverlay),
                new PropertyMetadata(false, (d, e) => ((LoaderOverlay)d).Apply((bool)e.NewValue)));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(LoaderOverlay),
                new PropertyMetadata("Loading...", (d, e) => ((LoaderOverlay)d).TxtMessage.Text = (string)e.NewValue));

        public static readonly DependencyProperty BlurTargetProperty =
            DependencyProperty.Register(nameof(BlurTarget), typeof(UIElement), typeof(LoaderOverlay));

        public static readonly DependencyProperty BlurRadiusProperty =
            DependencyProperty.Register(nameof(BlurRadius), typeof(double), typeof(LoaderOverlay), new PropertyMetadata(6.0));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public UIElement BlurTarget
        {
            get => (UIElement)GetValue(BlurTargetProperty);
            set => SetValue(BlurTargetProperty, value);
        }

        public double BlurRadius
        {
            get => (double)GetValue(BlurRadiusProperty);
            set => SetValue(BlurRadiusProperty, value);
        }

        public LoaderOverlay()
        {
            InitializeComponent();
        }

        private void Apply(bool active)
        {
            var target = BlurTarget;
            if (active)
            {
                Visibility = Visibility.Visible;
                IsHitTestVisible = true;
                BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(160)));
                if (target != null)
                {
                    var blur = new BlurEffect { Radius = 0, RenderingBias = RenderingBias.Quality, KernelType = KernelType.Gaussian };
                    target.Effect = blur;
                    blur.BeginAnimation(BlurEffect.RadiusProperty, new DoubleAnimation(BlurRadius, TimeSpan.FromMilliseconds(220)));
                }
            }
            else
            {
                IsHitTestVisible = false;
                var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(160));
                fade.Completed += (s, e) =>
                {
                    if (!IsActive) Visibility = Visibility.Collapsed;
                };
                BeginAnimation(OpacityProperty, fade);
                if (target?.Effect is BlurEffect blur)
                {
                    var unblur = new DoubleAnimation(0, TimeSpan.FromMilliseconds(160));
                    unblur.Completed += (s, e) =>
                    {
                        if (!IsActive) target.Effect = null;
                    };
                    blur.BeginAnimation(BlurEffect.RadiusProperty, unblur);
                }
            }
        }
    }
}
