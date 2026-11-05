using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SOLUM_UI
{
    public class OutlinedTextBlock : FrameworkElement
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(
            nameof(FontFamily), typeof(FontFamily), typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(new FontFamily("Segoe UI"), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
            nameof(FontSize), typeof(double), typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(14.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register(
            nameof(FontWeight), typeof(FontWeight), typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(FontWeights.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
            nameof(Fill), typeof(Brush), typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            nameof(Stroke), typeof(Brush), typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            nameof(StrokeThickness), typeof(double), typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(2.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
        public FontFamily FontFamily { get => (FontFamily)GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }
        public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
        public FontWeight FontWeight { get => (FontWeight)GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }
        public Brush Fill { get => (Brush)GetValue(FillProperty); set => SetValue(FillProperty, value); }
        public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
        public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

        private FormattedText Build()
        {
            return new FormattedText(
                Text ?? string.Empty,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyles.Normal, FontWeight, FontStretches.Normal),
                FontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var ft = Build();
            return new Size(ft.WidthIncludingTrailingWhitespace + StrokeThickness, ft.Height + StrokeThickness);
        }

        protected override void OnRender(DrawingContext dc)
        {
            var ft = Build();
            var geo = ft.BuildGeometry(new Point(StrokeThickness / 2, StrokeThickness / 2));
            var pen = new Pen(Stroke, StrokeThickness) { LineJoin = PenLineJoin.Round };
            dc.DrawGeometry(null, pen, geo);
            dc.DrawGeometry(Fill, null, geo);
        }
    }

    public partial class SplashScreenWindow : Window
    {
        public double LoadingSeconds { get; set; } = 2.5;

        public event EventHandler Completed;

        private readonly TaskCompletionSource<bool> _finished = new TaskCompletionSource<bool>();
        public Task Finished => _finished.Task;

        private readonly double[] _revealAt = { 0.36, 0.02, 0.68 };
        private readonly double[] _riseTo = { 0, -50, 0 };
        private readonly bool[] _revealed = new bool[3];
        private bool _headerShown;

        private double _progress;
        private readonly DateTime _openedAt = DateTime.UtcNow;

        private class WaveLayer
        {
            public Path Shape;
            public bool FromTop;
            public double BaseFrac;
            public double Rise;
            public double Amp;
            public double Length;
            public double Speed;
            public double Phase;
            public double Angle;
        }
        private readonly List<WaveLayer> _waves = new List<WaveLayer>();

        public SplashScreenWindow()
        {
            InitializeComponent();

            LoadLogo(Logo1, "cswdo.png");
            LoadLogo(Logo2, "solace.png");
            LoadLogo(Logo3, "binan.png");

            _waves.Add(new WaveLayer { Shape = WaveB1, BaseFrac = 0.30, Rise = 0.02, Amp = 30, Length = 900, Speed = 0.55, Phase = 0.0, Angle = 9 });
            _waves.Add(new WaveLayer { Shape = WaveB2, BaseFrac = 0.22, Rise = 0.02, Amp = 24, Length = 680, Speed = -0.85, Phase = 1.7, Angle = 10.5 });
            _waves.Add(new WaveLayer { Shape = WaveB3, BaseFrac = 0.14, Rise = 0.02, Amp = 18, Length = 520, Speed = 1.15, Phase = 3.1, Angle = 12 });
            _waves.Add(new WaveLayer { Shape = WaveT1, FromTop = true, BaseFrac = 0.18, Rise = 0, Amp = 24, Length = 760, Speed = -0.6, Phase = 0.8, Angle = 9 });
            _waves.Add(new WaveLayer { Shape = WaveT2, FromTop = true, BaseFrac = 0.30, Rise = 0, Amp = 30, Length = 1000, Speed = 0.4, Phase = 2.4, Angle = 11 });

            Loaded += (s, e) =>
            {
                foreach (var L in _waves)
                    L.Shape.RenderTransform = new RotateTransform(L.Angle, ActualWidth / 2, ActualHeight / 2);

                CompositionTarget.Rendering += OnRender;
                PlayAnimations();
                StartProgress();
            };

            // Ensure proper cleanup when splash screen closes
            Closed += (s, e) =>
            {
                CompositionTarget.Rendering -= OnRender;
                this.DataContext = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            };
        }

        private static void LoadLogo(ImageBrush brush, string fileName)
        {
            try
            {
                brush.ImageSource = new BitmapImage(new Uri($"pack://application:,,,/Assets/{fileName}", UriKind.Absolute));
            }
            catch
            {
            }
        }

        private void OnRender(object sender, EventArgs e)
        {
            double w = ActualWidth, h = ActualHeight;
            if (w < 10 || h < 10) return;
            double t = (DateTime.UtcNow - _openedAt).TotalSeconds;

            foreach (var L in _waves)
            {
                double baseY = (L.BaseFrac + L.Rise * _progress) * h;
                double amp = L.Amp * (1 + 0.15 * Math.Sin(t * 0.7 + L.Phase));
                double phase = L.Phase + t * L.Speed;

                const double pad = 900;
                var pts = new List<Point>();
                for (double x = -pad; x <= w + pad; x += 12)
                {
                    double off = baseY + amp * Math.Sin(2 * Math.PI * x / L.Length + phase);
                    pts.Add(new Point(x, L.FromTop ? off : h - off));
                }

                var geo = new StreamGeometry();
                using (var ctx = geo.Open())
                {
                    double edge = L.FromTop ? -pad : h + pad;
                    ctx.BeginFigure(new Point(-pad, edge), true, true);
                    ctx.PolyLineTo(pts, true, false);
                    ctx.LineTo(new Point(w + pad, edge), true, false);
                }
                geo.Freeze();
                L.Shape.Data = geo;
            }
        }

        private void PlayAnimations()
        {
            Spin(Ring1, 0, 360, 16);
            Spin(Ring2, 360, 0, 16);
            Spin(Ring3, 0, 360, 16);
        }

        private void ShowHeaderAndProgress()
        {
            if (_headerShown) return;
            _headerShown = true;

            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            Fade(TitleBlock, 0, 0.4);
            TitleShift.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(-14, 0, TimeSpan.FromSeconds(0.6)) { EasingFunction = ease });

            Fade(LoadingBlock, 0, 0.4);
        }

        private void PushForward(int i)
        {
            ShowHeaderAndProgress();

            var card = new[] { Card1, Card2, Card3 }[i];
            var scale = new[] { Scale1, Scale2, Scale3 }[i];
            var rise = new[] { Wave1, Wave2, Wave3 }[i];

            var dur = TimeSpan.FromSeconds(0.6);
            var pop = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 };
            var smooth = new CubicEase { EasingMode = EasingMode.EaseOut };

            card.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromSeconds(0.4)));
            var grow = new DoubleAnimation(1.0, dur) { EasingFunction = pop };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
            rise.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(_riseTo[i], dur) { EasingFunction = smooth });
        }

        private void StartProgress()
        {
            var started = DateTime.UtcNow;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += (s, e) =>
            {
                double t = Math.Min(1, (DateTime.UtcNow - started).TotalSeconds / LoadingSeconds);
                double p = t * t * (3 - 2 * t);
                _progress = p;

                BarScale.ScaleX = p;
                TxtPercent.Text = (int)Math.Round(p * 100) + "%";

                for (int i = 0; i < 3; i++)
                    if (!_revealed[i] && p >= _revealAt[i]) { _revealed[i] = true; PushForward(i); }

                if (t >= 1)
                {
                    timer.Stop();
                    TxtStatus.Text = "Ready";
                    TxtPercent.Text = "100%";
                    var hold = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                    hold.Tick += (s2, e2) =>
                    {
                        hold.Stop();
                        Completed?.Invoke(this, EventArgs.Empty);
                        _finished.TrySetResult(true);
                    };
                    hold.Start();
                }
            };
            timer.Start();
        }

        private static void Fade(UIElement el, double delay, double seconds) =>
            el.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromSeconds(seconds)) { BeginTime = TimeSpan.FromSeconds(delay) });

        private static void Spin(RotateTransform rt, double from, double to, double seconds) =>
            rt.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(from, to, TimeSpan.FromSeconds(seconds)) { RepeatBehavior = RepeatBehavior.Forever });
    }
}