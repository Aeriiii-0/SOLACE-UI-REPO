using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Components
{
    public partial class DonutChart : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(ObservableCollection<DonutSegment>), typeof(DonutChart),
                new PropertyMetadata(null, OnDataChanged));

        public ObservableCollection<DonutSegment> Data
        {
            get => (ObservableCollection<DonutSegment>)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        private Dictionary<Path, (DonutSegment segment, double percentage)> segmentMap = new Dictionary<Path, (DonutSegment, double)>();
        private ToolTip currentToolTip;

        public DonutChart()
        {
            InitializeComponent();
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = (DonutChart)d;
            
            // Unsubscribe from old collection
            if (e.OldValue is ObservableCollection<DonutSegment> oldCollection)
            {
                oldCollection.CollectionChanged -= chart.OnCollectionChanged;
            }
            
            // Subscribe to new collection
            if (e.NewValue is ObservableCollection<DonutSegment> newCollection)
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
            segmentMap.Clear();

            if (Data == null || Data.Count == 0)
            {
                NoDataPanel.Visibility = Visibility.Visible;
                return;
            }

            NoDataPanel.Visibility = Visibility.Collapsed;

            double centerX = 100;
            double centerY = 100;
            double outerRadius = 70;
            double innerRadius = 40;

            int total = 0;
            foreach (var segment in Data)
            {
                total += segment.Value;
            }

            if (total == 0)
            {
                NoDataPanel.Visibility = Visibility.Visible;
                return;
            }

            double startAngle = 0;
            int colorIndex = 0;

            foreach (var segment in Data)
            {
                double percentage = (double)segment.Value / total;
                double sweepAngle = percentage * 360;

                // Draw segment arc
                Path arc = DrawArc(centerX, centerY, outerRadius, innerRadius, startAngle, sweepAngle, segment.Color);
                
                // Store segment info for tooltip
                segmentMap[arc] = (segment, percentage);
                
                // Add mouse events for tooltip
                arc.MouseEnter += Arc_MouseEnter;
                arc.MouseLeave += Arc_MouseLeave;
                
                ChartCanvas.Children.Add(arc);

                // Add label at midpoint of arc
                double midAngle = startAngle + sweepAngle / 2;
                double midAngleRad = midAngle * Math.PI / 180;
                double labelRadius = (outerRadius + innerRadius) / 2;
                double labelX = centerX + labelRadius * Math.Cos(midAngleRad);
                double labelY = centerY + labelRadius * Math.Sin(midAngleRad);

                TextBlock label = new TextBlock
                {
                    Text = $"{percentage:P0}",
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center
                };

                Canvas.SetLeft(label, labelX - 15);
                Canvas.SetTop(label, labelY - 8);
                ChartCanvas.Children.Add(label);

                startAngle += sweepAngle;
                colorIndex++;
            }

            // Draw center circle for donut hole
            Ellipse centerCircle = new Ellipse
            {
                Width = innerRadius * 2,
                Height = innerRadius * 2,
                Fill = new SolidColorBrush(Colors.White),
                Stroke = new SolidColorBrush(Color.FromArgb(255, 234, 217, 219)),
                StrokeThickness = 1
            };
            Canvas.SetLeft(centerCircle, centerX - innerRadius);
            Canvas.SetTop(centerCircle, centerY - innerRadius);
            ChartCanvas.Children.Add(centerCircle);
        }

        private void Arc_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Path path && segmentMap.TryGetValue(path, out var info))
            {
                var (segment, percentage) = info;
                
                // Create tooltip
                ToolTip tooltip = new ToolTip
                {
                    Content = $"{segment.Label}\n{segment.Value} ({percentage:P1})",
                    Background = new SolidColorBrush(Color.FromArgb(255, 123, 20, 38)),
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 11,
                    Padding = new Thickness(10, 6, 10, 6),
                    IsOpen = true
                };
                
                currentToolTip = tooltip;
                path.ToolTip = tooltip;
            }
        }

        private void Arc_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Path path)
            {
                if (path.ToolTip is ToolTip tooltip)
                {
                    tooltip.IsOpen = false;
                }
                path.ToolTip = null;
            }
            currentToolTip = null;
        }

        private Path DrawArc(double centerX, double centerY, double outerRadius, double innerRadius, 
            double startAngle, double sweepAngle, string colorHex)
        {
            // Convert hex to Color
            Color color = (Color)ColorConverter.ConvertFromString(colorHex);

            double startAngleRad = startAngle * Math.PI / 180;
            double sweepAngleRad = sweepAngle * Math.PI / 180;
            double endAngleRad = startAngleRad + sweepAngleRad;

            // Outer arc points
            Point outerStart = new Point(
                centerX + outerRadius * Math.Cos(startAngleRad),
                centerY + outerRadius * Math.Sin(startAngleRad)
            );

            Point outerEnd = new Point(
                centerX + outerRadius * Math.Cos(endAngleRad),
                centerY + outerRadius * Math.Sin(endAngleRad)
            );

            // Inner arc points
            Point innerEnd = new Point(
                centerX + innerRadius * Math.Cos(endAngleRad),
                centerY + innerRadius * Math.Sin(endAngleRad)
            );

            Point innerStart = new Point(
                centerX + innerRadius * Math.Cos(startAngleRad),
                centerY + innerRadius * Math.Sin(startAngleRad)
            );

            // Build geometry
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure { StartPoint = outerStart };

            // Outer arc
            ArcSegment outerArc = new ArcSegment(
                outerEnd,
                new Size(outerRadius, outerRadius),
                0,
                sweepAngle > 180,
                SweepDirection.Clockwise,
                true
            );
            figure.Segments.Add(outerArc);

            // Line to inner arc
            figure.Segments.Add(new LineSegment { Point = innerEnd });

            // Inner arc (reverse)
            ArcSegment innerArc = new ArcSegment(
                innerStart,
                new Size(innerRadius, innerRadius),
                0,
                sweepAngle > 180,
                SweepDirection.Counterclockwise,
                true
            );
            figure.Segments.Add(innerArc);

            figure.IsClosed = true;
            geometry.Figures.Add(figure);

            Path path = new Path
            {
                Data = geometry,
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1
            };

            return path;
        }
    }
}
