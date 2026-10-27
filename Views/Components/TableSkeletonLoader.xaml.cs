using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SOLUM_UI.Views.Components
{
    public partial class TableSkeletonLoader : UserControl
    {
        private Storyboard _pulseStoryboard;

        public TableSkeletonLoader()
        {
            InitializeComponent();
            _pulseStoryboard = TryFindResource("PulseStoryboard") as Storyboard;

            IsVisibleChanged += TableSkeletonLoader_IsVisibleChanged;
            Loaded += TableSkeletonLoader_Loaded;
            Unloaded += TableSkeletonLoader_Unloaded;
        }

        private void TableSkeletonLoader_Loaded(object sender, RoutedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                _pulseStoryboard?.Begin(this, true);
            }
        }

        private void TableSkeletonLoader_Unloaded(object sender, RoutedEventArgs e)
        {
            _pulseStoryboard?.Stop(this);
        }

        private void TableSkeletonLoader_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                _pulseStoryboard?.Begin(this, true);
            }
            else
            {
                _pulseStoryboard?.Stop(this);
            }
        }
    }
}
