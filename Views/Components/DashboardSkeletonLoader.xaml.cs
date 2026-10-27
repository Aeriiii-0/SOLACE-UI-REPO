using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SOLUM_UI.Views.Components
{
    public partial class DashboardSkeletonLoader : UserControl
    {
        private Storyboard _pulseStoryboard;

        public DashboardSkeletonLoader()
        {
            InitializeComponent();
            _pulseStoryboard = TryFindResource("PulseStoryboard") as Storyboard;

            IsVisibleChanged += DashboardSkeletonLoader_IsVisibleChanged;
            Loaded += DashboardSkeletonLoader_Loaded;
            Unloaded += DashboardSkeletonLoader_Unloaded;
        }

        private void DashboardSkeletonLoader_Loaded(object sender, RoutedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                _pulseStoryboard?.Begin(this, true);
            }
        }

        private void DashboardSkeletonLoader_Unloaded(object sender, RoutedEventArgs e)
        {
            _pulseStoryboard?.Stop(this);
        }

        private void DashboardSkeletonLoader_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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
