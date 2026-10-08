using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Components
{
    public partial class CivilStatusListCard : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(ObservableCollection<ListItem>), typeof(CivilStatusListCard),
                new PropertyMetadata(null, OnDataChanged));

        public ObservableCollection<ListItem> Data
        {
            get => (ObservableCollection<ListItem>)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public CivilStatusListCard()
        {
            InitializeComponent();
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CivilStatusListCard)d;
            
            // Unsubscribe from old collection
            if (e.OldValue is ObservableCollection<ListItem> oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnCollectionChanged;
            }
            
            // Subscribe to new collection
            if (e.NewValue is ObservableCollection<ListItem> newCollection)
            {
                newCollection.CollectionChanged += control.OnCollectionChanged;
            }
            
            control.RenderList();
        }

        private void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RenderList();
        }

        private void RenderList()
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
                Grid row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.Margin = new Thickness(0, 0, 0, 8);

                // Label
                TextBlock label = new TextBlock
                {
                    Text = item.Label,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 43, 26, 28)),
                    FontSize = 11
                };
                Grid.SetColumn(label, 0);
                row.Children.Add(label);

                // Value
                TextBlock value = new TextBlock
                {
                    Text = item.Value.ToString(),
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 123, 20, 38)),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold
                };
                Grid.SetColumn(value, 1);
                row.Children.Add(value);

                ItemsStackPanel.Children.Add(row);
            }
        }
    }
}
