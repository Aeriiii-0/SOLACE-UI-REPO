using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Components
{
    public partial class ColumnChart : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(ObservableCollection<ColumnChartData>), typeof(ColumnChart),
                new PropertyMetadata(null, OnDataChanged));

        public ObservableCollection<ColumnChartData> Data
        {
            get => (ObservableCollection<ColumnChartData>)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public ColumnChart()
        {
            InitializeComponent();
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = (ColumnChart)d;
            
            // Unsubscribe from old collection
            if (e.OldValue is ObservableCollection<ColumnChartData> oldCollection)
            {
                oldCollection.CollectionChanged -= chart.OnCollectionChanged;
            }
            
            // Subscribe to new collection
            if (e.NewValue is ObservableCollection<ColumnChartData> newCollection)
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
            double chartWidth = ChartCanvas.ActualWidth > 0 ? ChartCanvas.ActualWidth - 2 * padding : 300;
            double chartHeight = ChartCanvas.ActualHeight > 0 ? ChartCanvas.ActualHeight - 2 * padding : 200;

            if (chartWidth <= 0 || chartHeight <= 0)
            {
                chartWidth = 300;
                chartHeight = 200;
            }

            // Find max value
            int maxValue = 0;
            foreach (var item in Data)
            {
                if (item.Value > maxValue)
                    maxValue = item.Value;
            }

            if (maxValue == 0)
            {
                NoDataPanel.Visibility = Visibility.Visible;
                return;
            }

            // Calculate scales
            double columnWidth = chartWidth / (Data.Count * 1.5);
            double scaleY = chartHeight / maxValue;

            // Draw grid lines
            for (int i = 0; i <= 4; i++)
            {
                double y = padding + chartHeight - (i * chartHeight / 4);
                Line gridLine = new Line
                {
                    X1 = padding,
                    Y1 = y,
                    X2 = padding + chartWidth,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 234, 217, 219)),
                    StrokeThickness = 1
                };
                ChartCanvas.Children.Add(gridLine);

                // Y-axis label
                if (i > 0)
                {
                    int labelValue = (int)(i * maxValue / 4);
                    TextBlock label = new TextBlock
                    {
                        Text = labelValue.ToString(),
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 122, 92, 96)),
                        FontSize = 9
                    };
                    Canvas.SetLeft(label, padding - 30);
                    Canvas.SetTop(label, y - 8);
                    ChartCanvas.Children.Add(label);
                }
            }

            // Draw columns
            for (int i = 0; i < Data.Count; i++)
            {
                double columnHeight = Data[i].Value * scaleY;
                double x = padding + i * (columnWidth * 1.5) + columnWidth * 0.25;
                double y = padding + chartHeight - columnHeight;

                // Column rectangle
                Rectangle column = new Rectangle
                {
                    Width = columnWidth,
                    Height = columnHeight,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Data[i].Color)),
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 234, 217, 219)),
                    StrokeThickness = 1
                };

                Canvas.SetLeft(column, x);
                Canvas.SetTop(column, y);
                ChartCanvas.Children.Add(column);

                // Value label on top
                TextBlock valueLabel = new TextBlock
                {
                    Text = Data[i].Value.ToString(),
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 43, 26, 28)),
                    FontSize = 10,
                    FontWeight = FontWeights.Medium,
                    TextAlignment = TextAlignment.Center,
                    Width = columnWidth
                };
                Canvas.SetLeft(valueLabel, x);
                Canvas.SetTop(valueLabel, y - 18);
                ChartCanvas.Children.Add(valueLabel);

                // X-axis label
                TextBlock xLabel = new TextBlock
                {
                    Text = Data[i].Label,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 122, 92, 96)),
                    FontSize = 9,
                    TextAlignment = TextAlignment.Center,
                    Width = columnWidth
                };
                Canvas.SetLeft(xLabel, x);
                Canvas.SetTop(xLabel, padding + chartHeight + 5);
                ChartCanvas.Children.Add(xLabel);
            }
        }
    }
}
