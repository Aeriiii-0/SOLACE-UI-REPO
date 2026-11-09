using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SOLUM_UI.ViewModels;
using Point = System.Windows.Point;

namespace SOLUM_UI
{
    public partial class DashboardPage : Page
    {
        private readonly AuditLogViewModel _logsVm = new AuditLogViewModel();

        private enum ChartGranularity { Monthly, Quarterly }
        private ChartGranularity _mode = ChartGranularity.Quarterly;
        private List<ChartPoint> _points = new List<ChartPoint>();
        private bool _ready;

        private static readonly Brush Maroon = Frozen("#702943");

        public DashboardPage()
        {
            InitializeComponent();
            if (!(DataContext is DashboardViewModel))
                DataContext = new DashboardViewModel();
            if (DataContext is DashboardViewModel dvm)
            {
                dvm.PropertyChanged += Vm_PropertyChanged;
            }
            LogsOverlay.DataContext = _logsVm;
            _ready = true;
            Loaded += async (s, e) =>
            {
                if (DataContext is DashboardViewModel vm)
                {
                    await vm.LoadAsync();
                    RefreshChart();
                }
                if (!_logsVm.IsInitialized)
                    _ = _logsVm.LoadAsync();
            };
        }

        private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var vm = sender as DashboardViewModel;
            if (vm == null) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(DashboardViewModel.RegistrationDates):
                        RefreshChart();
                        break;
                }
            }));
        }

        private void HeroClipHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var host = (FrameworkElement)sender;
            host.Clip = new RectangleGeometry(new Rect(0, 0, host.ActualWidth, host.ActualHeight), 22, 22);
        }

        private class ChartPoint
        {
            public string Label { get; set; }
            public string Title { get; set; }
            public string Range { get; set; }
            public int Value { get; set; }
            public int NewCount { get; set; }
            public string DeltaText { get; set; }
            public int DeltaKind { get; set; }
            public bool IsFuture { get; set; }
        }

        private void ChartMode_Checked(object sender, RoutedEventArgs e)
        {
            if (!_ready) return;
            _mode = RbMonthly.IsChecked == true ? ChartGranularity.Monthly : ChartGranularity.Quarterly;
            RefreshChart();
        }

        private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawChart();

        private void RefreshChart()
        {
            if (!_ready) return;
            var vm = DataContext as DashboardViewModel;
            IEnumerable<DateTime> dates = vm?.RegistrationDates;
            _points = BuildPoints(dates, _mode);

            int year = DateTime.Today.Year;
            if (_mode == ChartGranularity.Monthly)
            {
                TxtChartTitle.Text = "Monthly registrations";
                TxtChartSubtitle.Text = "Cumulative solo parents registered, Jan to Dec " + year;
            }
            else
            {
                TxtChartTitle.Text = "Quarterly registrations";
                TxtChartSubtitle.Text = "Cumulative solo parents registered, Q1 to Q4 " + year;
            }
            DrawChart();
        }

        private static List<ChartPoint> BuildPoints(IEnumerable<DateTime> dates, ChartGranularity mode)
        {
            var list = dates == null ? new List<DateTime>() : dates.ToList();
            var result = new List<ChartPoint>();
            var today = DateTime.Today;
            int year = today.Year;
            // Start running total from baseline records registered prior to the current year
            int runningTotal = list.Count(d => d < new DateTime(year, 1, 1));

            if (mode == ChartGranularity.Monthly)
            {
                for (int m = 1; m <= 12; m++)
                {
                    var start = new DateTime(year, m, 1);
                    var end = start.AddMonths(1).AddDays(-1);
                    int newInMonth = list.Count(d => d.Year == year && d.Month == m);
                    runningTotal += newInMonth;

                    result.Add(new ChartPoint
                    {
                        Label = start.ToString("MMM"),
                        Title = start.ToString("MMMM yyyy"),
                        Range = start.ToString("MMM d") + " – " + end.ToString("MMM d"),
                        Value = runningTotal,
                        NewCount = newInMonth,
                        IsFuture = start > today && newInMonth == 0
                    });
                }
            }
            else
            {
                for (int q = 1; q <= 4; q++)
                {
                    var qs = new DateTime(year, (q - 1) * 3 + 1, 1);
                    var qe = qs.AddMonths(3);
                    int newInQuarter = list.Count(d => d.Year == year && d >= qs && d < qe);
                    runningTotal += newInQuarter;

                    result.Add(new ChartPoint
                    {
                        Label = "Q" + q,
                        Title = "Q" + q + " " + year,
                        Range = qs.ToString("MMM d") + " – " + qe.AddDays(-1).ToString("MMM d"),
                        Value = runningTotal,
                        NewCount = newInQuarter,
                        IsFuture = qs > today && newInQuarter == 0
                    });
                }
            }

            for (int i = 0; i < result.Count; i++)
            {
                var pt = result[i];
                string periodUnit = mode == ChartGranularity.Monthly ? "this month" : "this quarter";

                if (pt.NewCount > 0)
                {
                    pt.DeltaKind = 1;
                    pt.DeltaText = $"+{pt.NewCount:N0} new {periodUnit}";
                }
                else
                {
                    pt.DeltaKind = 0;
                    pt.DeltaText = $"No new registrations {periodUnit}";
                }
            }
            return result;
        }

        private static double NiceMax(int max)
        {
            if (max <= 2) return 2;
            double step = max / 2.0;
            double mag = Math.Pow(10, Math.Floor(Math.Log10(step)));
            double norm = step / mag;
            double nice = norm <= 1 ? 1 : norm <= 1.5 ? 1.5 : norm <= 2 ? 2 : norm <= 3 ? 3 : norm <= 5 ? 5 : 10;
            return nice * mag * 2;
        }

        private void ShowChartTip(int index, Point p, bool placeLeft)
        {
            var pt = _points[index];
            TipTitle.Text = pt.Title;
            TipRange.Text = pt.Range;
            TipValue.Text = pt.Value.ToString("N0");
            if (string.IsNullOrEmpty(pt.DeltaText))
            {
                TipDeltaPill.Visibility = Visibility.Collapsed;
            }
            else
            {
                TipDeltaPill.Visibility = Visibility.Visible;
                TipDelta.Text = pt.DeltaText;
                TipDeltaPill.Background = Frozen(pt.DeltaKind > 0 ? "#DDF3EA" : pt.DeltaKind < 0 ? "#FCE0E3" : "#F3E8EC");
                TipDelta.Foreground = Frozen(pt.DeltaKind > 0 ? "#0B7A5A" : pt.DeltaKind < 0 ? "#B4283F" : "#7A5566");
            }
            ChartTip.Visibility = Visibility.Visible;
            ChartTip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            double tw = ChartTip.DesiredSize.Width, th = ChartTip.DesiredSize.Height;
            double cw = ChartCanvas.ActualWidth, ch = ChartCanvas.ActualHeight;
            double left, top;
            if (placeLeft)
            {
                left = p.X - tw - 22;
                if (left < 0) left = p.X + 22;
                top = Math.Max(0, Math.Min(ch - th, p.Y - th / 2));
            }
            else
            {
                left = Math.Max(0, Math.Min(cw - tw, p.X - tw / 2));
                top = p.Y - th - 18;
                if (top < 0) top = p.Y + 18;
            }
            ChartTip.Margin = new Thickness(left, top, 0, 0);
        }

        private void HideChartTip()
        {
            ChartTip.Visibility = Visibility.Collapsed;
        }

        private void DrawChart()
        {
            if (!_ready) return;
            var canvas = ChartCanvas;
            canvas.Children.Clear();
            HideChartTip();

            double w = canvas.ActualWidth, h = canvas.ActualHeight;
            if (w < 120 || h < 100) return;
            const double L = 40, R = 24, T = 44, B = 30;
            double pw = w - L - R, ph = h - T - B;
            int n = _points.Count;
            if (n == 0)
            {
                var empty = new TextBlock { Text = "No registration data yet", Foreground = Frozen("#C0A0AA"), FontSize = 13 };
                Canvas.SetLeft(empty, L); Canvas.SetTop(empty, T);
                canvas.Children.Add(empty);
                return;
            }

            int m = _points.Count(p => !p.IsFuture);
            double yMax = NiceMax(_points.Max(p => p.Value));
            for (int i = 0; i <= 2; i++)
            {
                double y = T + ph * i / 2.0;
                var line = new Line
                {
                    X1 = L,
                    X2 = L + pw,
                    Y1 = y,
                    Y2 = y,
                    Stroke = Frozen(i == 2 ? "#EAD3DB" : "#F1E4E9"),
                    StrokeThickness = 1,
                    StrokeDashArray = i == 2 ? null : new DoubleCollection { 2, 4 }
                };
                canvas.Children.Add(line);
                var lbl = new TextBlock
                {
                    Text = (yMax * (2 - i) / 2.0).ToString("0"),
                    FontSize = 11,
                    Foreground = Frozen("#C7A9B3"),
                    Width = L - 14,
                    TextAlignment = TextAlignment.Right
                };
                Canvas.SetLeft(lbl, 0);
                Canvas.SetTop(lbl, y - 8);
                canvas.Children.Add(lbl);
            }
            var pts = new Point[n];
            for (int i = 0; i < n; i++)
            {
                double x = L + pw * (i + 0.5) / n;
                double y = T + ph - (ph * _points[i].Value / yMax);
                pts[i] = new Point(x, y);
            }
            for (int i = 0; i < n; i++)
            {
                var xl = new TextBlock
                {
                    Text = _points[i].Label,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Maroon,
                    Width = 60,
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetLeft(xl, pts[i].X - 30);
                Canvas.SetTop(xl, T + ph + 8);
                canvas.Children.Add(xl);
            }
            if (m == 0) return;

            double baseY = T + ph;
            Func<double, double> clampY = y => Math.Max(T, Math.Min(baseY, y));
            Action<PathFigure> addCurve = fig =>
            {
                for (int i = 0; i < m - 1; i++)
                {
                    Point p0 = i == 0 ? pts[i] : pts[i - 1];
                    Point p1 = pts[i];
                    Point p2 = pts[i + 1];
                    Point p3 = i + 2 < m ? pts[i + 2] : pts[i + 1];
                    var c1 = new Point(p1.X + (p2.X - p0.X) / 6.0, clampY(p1.Y + (p2.Y - p0.Y) / 6.0));
                    var c2 = new Point(p2.X - (p3.X - p1.X) / 6.0, clampY(p2.Y - (p3.Y - p1.Y) / 6.0));
                    fig.Segments.Add(new BezierSegment(c1, c2, p2, true));
                }
            };
            if (m >= 2)
            {
                var areaFig = new PathFigure { StartPoint = new Point(pts[0].X, baseY), IsClosed = true };
                areaFig.Segments.Add(new LineSegment(pts[0], true));
                addCurve(areaFig);
                areaFig.Segments.Add(new LineSegment(new Point(pts[m - 1].X, baseY), true));
                var areaBrush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
                areaBrush.GradientStops.Add(new GradientStop(Frozenless("#F4BFD0"), 0));
                areaBrush.GradientStops.Add(new GradientStop(Frozenless("#FFF6F9"), 1));
                canvas.Children.Add(new Path { Data = new PathGeometry(new[] { areaFig }), Fill = areaBrush, IsHitTestVisible = false });
                var lineFig = new PathFigure { StartPoint = pts[0] };
                addCurve(lineFig);
                canvas.Children.Add(new Path
                {
                    Data = new PathGeometry(new[] { lineFig }),
                    Stroke = Maroon,
                    StrokeThickness = 3,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    IsHitTestVisible = false
                });
            }
            for (int i = 0; i < m; i++)
            {
                int idx = i;
                bool isLast = i == m - 1;
                Ellipse visual = null;
                if (isLast)
                {
                    const double halo = 28, core = 14;
                    var haloDot = new Ellipse
                    {
                        Width = halo,
                        Height = halo,
                        Fill = Frozen("#F5D3DE"),
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(haloDot, pts[i].X - halo / 2);
                    Canvas.SetTop(haloDot, pts[i].Y - halo / 2);
                    canvas.Children.Add(haloDot);
                    var coreDot = new Ellipse
                    {
                        Width = core,
                        Height = core,
                        Fill = Maroon,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(coreDot, pts[i].X - core / 2);
                    Canvas.SetTop(coreDot, pts[i].Y - core / 2);
                    canvas.Children.Add(coreDot);
                    var badge = new Border
                    {
                        Background = Maroon,
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(11, 3, 11, 3),
                        IsHitTestVisible = false,
                        Child = new TextBlock
                        {
                            Text = _points[i].Value.ToString("N0") + " total",
                            FontFamily = new FontFamily("Segoe UI"),
                            FontSize = 11,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = Brushes.White
                        }
                    };
                    badge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    double bw = badge.DesiredSize.Width;
                    double bl = Math.Max(0, Math.Min(w - bw, pts[i].X - bw / 2));
                    Canvas.SetLeft(badge, bl);
                    Canvas.SetTop(badge, Math.Max(0, pts[i].Y - 44));
                    canvas.Children.Add(badge);
                }
                else
                {
                    const double d = 13;
                    visual = new Ellipse
                    {
                        Width = d,
                        Height = d,
                        Fill = Brushes.White,
                        Stroke = Maroon,
                        StrokeThickness = 2.5,
                        IsHitTestVisible = false,
                        RenderTransformOrigin = new Point(0.5, 0.5),
                        RenderTransform = new ScaleTransform(1, 1)
                    };
                    Canvas.SetLeft(visual, pts[i].X - d / 2);
                    Canvas.SetTop(visual, pts[i].Y - d / 2);
                    canvas.Children.Add(visual);
                }
                const double hitSize = 34;
                var hit = new Ellipse
                {
                    Width = hitSize,
                    Height = hitSize,
                    Fill = Brushes.Transparent,
                    Cursor = Cursors.Hand
                };
                Canvas.SetLeft(hit, pts[i].X - hitSize / 2);
                Canvas.SetTop(hit, pts[i].Y - hitSize / 2);
                var captured = visual;
                hit.MouseEnter += (s, e) =>
                {
                    if (captured != null)
                    {
                        captured.Fill = Maroon;
                        ((ScaleTransform)captured.RenderTransform).ScaleX = 1.3;
                        ((ScaleTransform)captured.RenderTransform).ScaleY = 1.3;
                    }
                    ShowChartTip(idx, pts[idx], isLast);
                };
                hit.MouseLeave += (s, e) =>
                {
                    if (captured != null)
                    {
                        captured.Fill = Brushes.White;
                        ((ScaleTransform)captured.RenderTransform).ScaleX = 1;
                        ((ScaleTransform)captured.RenderTransform).ScaleY = 1;
                    }
                    HideChartTip();
                };
                canvas.Children.Add(hit);
            }
        }

        public void SetMarkedDates(IEnumerable<DateTime> dates)
        {
            // Calendar removed from dashboard
        }

        private void ViewAllLogs_Click(object sender, RoutedEventArgs e)
        {
            OverlayRoot.Visibility = Visibility.Visible;
        }

        private void LogsOverlay_CloseRequested(object sender, RoutedEventArgs e)
        {
            OverlayRoot.Visibility = Visibility.Collapsed;
        }

        private void ReviewExpiring_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ReviewExpiring_Click: Starting navigation");
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("ReviewExpiring_Click: MainWindow is null!");
                    return;
                }
                System.Diagnostics.Debug.WriteLine("ReviewExpiring_Click: MainWindow found");
                // Clear cache to force fresh page load
                mainWindow.ClearPageCache("SoloParentRecords");
                System.Diagnostics.Debug.WriteLine("ReviewExpiring_Click: Cache cleared");
                // Set the filter to ExpiringSoon
                SoloParentRecordsPage.RequestedStatusFilter = "ExpiringSoon";
                System.Diagnostics.Debug.WriteLine("ReviewExpiring_Click: Filter set to ExpiringSoon");
                // Activate the SoloParentRecords button and navigate
                mainWindow.ActivateButton("SoloParentRecords");
                System.Diagnostics.Debug.WriteLine("ReviewExpiring_Click: Button activated");
                mainWindow.NavigateTo("SoloParentRecords");
                System.Diagnostics.Debug.WriteLine("ReviewExpiring_Click: Navigation called");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReviewExpiring_Click: Exception - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ReviewExpiring_Click: StackTrace - {ex.StackTrace}");
            }
        }

        private static Color Frozenless(string hex) => (Color)ColorConverter.ConvertFromString(hex);

        private static Brush Frozen(string hex)
        {
            var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
            b.Freeze();
            return b;
        }
    }
}