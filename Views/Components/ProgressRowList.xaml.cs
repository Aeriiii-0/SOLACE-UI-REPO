using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Components
{
    public partial class ProgressRowList : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(ObservableCollection<ProgressItem>), typeof(ProgressRowList),
                new PropertyMetadata(null, OnDataChanged));

        public ObservableCollection<ProgressItem> Data
        {
            get => (ObservableCollection<ProgressItem>)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public ProgressRowList()
        {
            InitializeComponent();
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ProgressRowList)d;
            
            // Unsubscribe from old collection
            if (e.OldValue is ObservableCollection<ProgressItem> oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnCollectionChanged;
            }
            
            // Subscribe to new collection
            if (e.NewValue is ObservableCollection<ProgressItem> newCollection)
            {
                newCollection.CollectionChanged += control.OnCollectionChanged;
            }
            
            control.RenderRows();
        }

        private void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RenderRows();
        }

        private void RenderRows()
        {
            ItemsStackPanel.Children.Clear();

            if (Data == null || Data.Count == 0)
            {
                NoDataPanel.Visibility = Visibility.Visible;
                return;
            }

            NoDataPanel.Visibility = Visibility.Collapsed;

            foreach (var item in Data)
            {
                // Outer row container
                Border rowBorder = new Border
                {
                    Margin = new Thickness(0, 0, 0, 14),
                    Background = new SolidColorBrush(Color.FromArgb(20, 123, 20, 38)) // Subtle background
                };

                Grid row = new Grid();
                row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.Margin = new Thickness(12);

                // Label
                TextBlock label = new TextBlock
                {
                    Text = item.Label,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 43, 26, 28)),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 8, 6)
                };
                Grid.SetRow(label, 0);
                Grid.SetColumn(label, 0);
                row.Children.Add(label);

                // Value with rounded percentage and tooltip
                TextBlock value = new TextBlock
                {
                    Text = $"{item.Value} ({item.Percentage:F1}%)",
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 123, 20, 38)),
                    FontSize = 10,
                    FontWeight = FontWeights.Medium,
                    Margin = new Thickness(0, 0, 0, 6)
                };
                
                // Add tooltip with category info
                value.ToolTip = new ToolTip
                {
                    Content = $"{item.Label}: {item.Percentage:F1}%",
                    Background = new SolidColorBrush(Color.FromArgb(255, 123, 20, 38)),
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 10,
                    Padding = new Thickness(8, 4, 8, 4)
                };
                
                Grid.SetRow(value, 0);
                Grid.SetColumn(value, 1);
                row.Children.Add(value);

                // Progress Bar with enhanced styling
                ProgressBar progressBar = new ProgressBar
                {
                    Value = item.Percentage,
                    Maximum = 100,
                    Height = 8,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 123, 20, 38)),
                    Background = new SolidColorBrush(Color.FromArgb(255, 244, 230, 232))
                };
                
                // Add tooltip to progress bar
                progressBar.ToolTip = new ToolTip
                {
                    Content = $"{item.Label}: {item.Percentage:F1}%",
                    Background = new SolidColorBrush(Color.FromArgb(255, 123, 20, 38)),
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 10,
                    Padding = new Thickness(8, 4, 8, 4)
                };
                
                Grid.SetRow(progressBar, 1);
                Grid.SetColumn(progressBar, 0);
                Grid.SetColumnSpan(progressBar, 2);
                row.Children.Add(progressBar);

                rowBorder.Child = row;
                ItemsStackPanel.Children.Add(rowBorder);
            }
        }
    }
}
