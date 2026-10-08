using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Components
{
    public partial class AreaChart : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(ObservableCollection<AreaChartData>), typeof(AreaChart),
                new PropertyMetadata(null, OnDataChanged));

        public ObservableCollection<AreaChartData> Data
        {
            get => (ObservableCollection<AreaChartData>)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public AreaChart()
        {
            InitializeComponent();
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = (AreaChart)d;
            
            // Unsubscribe from old collection
            if (e.OldValue is ObservableCollection<AreaChartData> oldCollection)
            {
                oldCollection.CollectionChanged -= chart.OnCollectionChanged;
            }
            
            // Subscribe to new collection
            if (e.NewValue is ObservableCollection<AreaChartData> newCollection)
            {
                newCollection.CollectionChanged += chart.OnCollectionChanged;
            }
            
            chart.RenderChart();
        }

        private void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RenderChart();
        }

        private void RenderChart()
        {
            ChartCanvas.Children.Clear();

            if (Data == null || Data.Count == 0)
            {
                NoDataPanel.Visibility = Visibility.Visible;
                return;
            }

            NoDataPanel.Visibility = Visibility.Collapsed;

            double padding = 40;
            double chartWidth = ChartCanvas.ActualWidth > 0 ? ChartCanvas.ActualWidth - 2 * padding : 400;
            double chartHeight = ChartCanvas.ActualHeight > 0 ? ChartCanvas.ActualHeight - 2 * padding : 200;

            if (chartWidth <= 0 || chartHeight <= 0)
            {
                chartWidth = 400;
                chartHeight = 200;
            }

            // Find max value
            int maxValue = 0;
            foreach (var point in Data)
            {
                if (point.Value > maxValue)
                    maxValue = point.Value;
            }

            if (maxValue == 0)
            {
                NoDataPanel.Visibility = Visibility.Visible;
                return;
            }

            // Calculate scale
            double scaleX = chartWidth / (Data.Count - 1 > 0 ? Data.Count - 1 : 1);
            double scaleY = chartHeight / maxValue;

            // Draw grid lines and Y-axis labels
            for (int i = 0; i <= 5; i++)
            {
                double y = padding + chartHeight - (i * chartHeight / 5);
                
                // Lighter grid lines
                Line gridLine = new Line
                {
                    X1 = padding,
                    Y1 = y,
                    X2 = padding + chartWidth,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(180, 244, 230, 232)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 2 }
                };
                ChartCanvas.Children.Add(gridLine);

                // Y-axis label
                if (i > 0 || i == 0)
                {
                    int labelValue = (int)(i * maxValue / 5);
                    TextBlock label = new TextBlock
                    {
                        Text = labelValue.ToString(),
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 122, 92, 96)),
                        FontSize = 9,
                        TextAlignment = TextAlignment.Right,
                        Width = 25
                    };
                    Canvas.SetLeft(label, padding - 32);
                    Canvas.SetTop(label, y - 8);
                    ChartCanvas.Children.Add(label);
                }
            }

            // Draw Y-axis
            Line yAxis = new Line
            {
                X1 = padding,
                Y1 = padding,
                X2 = padding,
                Y2 = padding + chartHeight,
                Stroke = new SolidColorBrush(Color.FromArgb(200, 122, 92, 96)),
                StrokeThickness = 1.5
            };
            ChartCanvas.Children.Add(yAxis);

            // Calculate points
            PointCollection points = new PointCollection();
            for (int i = 0; i < Data.Count; i++)
            {
                double x = padding + i * scaleX;
                double y = padding + chartHeight - (Data[i].Value * scaleY);
                points.Add(new Point(x, y));
            }

            // Draw smooth area using Bezier curves instead of straight lines
            PathGeometry areaGeometry = new PathGeometry();
            PathFigure areaFigure = new PathFigure { StartPoint = new Point(padding, padding + chartHeight) };

            if (points.Count > 0)
            {
                areaFigure.Segments.Add(new LineSegment { Point = points[0] });

                // Use Bezier curves for smooth transitions between points
                for (int i = 1; i < points.Count; i++)
                {
                    Point p1 = points[i - 1];
                    Point p2 = points[i];

                    // Calculate control points for smooth curve
                    double controlPointOffset = scaleX * 0.3;
                    Point cp1 = new Point(p1.X + controlPointOffset, p1.Y);
                    Point cp2 = new Point(p2.X - controlPointOffset, p2.Y);

                    BezierSegment curve = new BezierSegment
                    {
                        Point1 = cp1,
                        Point2 = cp2,
                        Point3 = p2
                    };
                    areaFigure.Segments.Add(curve);
                }

                // Close the area
                if (points.Count > 0)
                {
                    areaFigure.Segments.Add(new LineSegment { Point = new Point(padding + chartWidth, padding + chartHeight) });
                }
            }

            areaFigure.IsClosed = true;
            areaGeometry.Figures.Add(areaFigure);

            Path areaPath = new Path
            {
                Data = areaGeometry,
                Fill = new SolidColorBrush(Color.FromArgb(120, 161, 68, 82)), // Adjusted semi-transparent maroon
                Stroke = new SolidColorBrush(Color.FromArgb(255, 123, 20, 38)),
                StrokeThickness = 2.5
            };
            ChartCanvas.Children.Add(areaPath);

            // Draw smooth line using Bezier curves
            PathGeometry lineGeometry = new PathGeometry();
            PathFigure lineFigure = new PathFigure();
            if (points.Count > 0)
            {
                lineFigure.StartPoint = points[0];
                for (int i = 1; i < points.Count; i++)
                {
                    Point p1 = points[i - 1];
                    Point p2 = points[i];

                    double controlPointOffset = scaleX * 0.3;
                    Point cp1 = new Point(p1.X + controlPointOffset, p1.Y);
                    Point cp2 = new Point(p2.X - controlPointOffset, p2.Y);

                    BezierSegment curve = new BezierSegment
                    {
                        Point1 = cp1,
                        Point2 = cp2,
                        Point3 = p2
                    };
                    lineFigure.Segments.Add(curve);
                }
                lineGeometry.Figures.Add(lineFigure);
            }

            Path linePath = new Path
            {
                Data = lineGeometry,
                Stroke = new SolidColorBrush(Color.FromArgb(255, 123, 20, 38)),
                StrokeThickness = 2.5,
                IsHitTestVisible = false
            };
            ChartCanvas.Children.Add(linePath);

            // Draw points, labels, and value indicators
            for (int i = 0; i < Data.Count; i++)
            {
                double pointX = points[i].X;
                double pointY = points[i].Y;

                // Circle point
                Ellipse pointCircle = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(Color.FromArgb(255, 123, 20, 38)),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(pointCircle, pointX - 4);
                Canvas.SetTop(pointCircle, pointY - 4);
                ChartCanvas.Children.Add(pointCircle);

                // Value label above point
                TextBlock valueLabel = new TextBlock
                {
                    Text = Data[i].Value.ToString(),
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 123, 20, 38)),
                    FontSize = 9,
                    FontWeight = FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center,
                    Width = 40
                };
                Canvas.SetLeft(valueLabel, pointX - 20);
                Canvas.SetTop(valueLabel, pointY - 20);
                ChartCanvas.Children.Add(valueLabel);

                // X-axis label
                TextBlock xLabel = new TextBlock
                {
                    Text = Data[i].Label,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 122, 92, 96)),
                    FontSize = 9,
                    TextAlignment = TextAlignment.Center,
                    Width = 50
                };
                Canvas.SetLeft(xLabel, pointX - 25);
                Canvas.SetTop(xLabel, padding + chartHeight + 10);
                ChartCanvas.Children.Add(xLabel);
            }
        }
    }
}
