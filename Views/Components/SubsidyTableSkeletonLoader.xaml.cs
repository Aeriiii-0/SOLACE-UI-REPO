using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SOLUM_UI.Views.Components
{
    public partial class SubsidyTableSkeletonLoader : UserControl
    {
        private Storyboard _pulseStoryboard;

        public SubsidyTableSkeletonLoader()
        {
            InitializeComponent();
            _pulseStoryboard = TryFindResource("PulseStoryboard") as Storyboard;

            IsVisibleChanged += SubsidyTableSkeletonLoader_IsVisibleChanged;
            Loaded += SubsidyTableSkeletonLoader_Loaded;
            Unloaded += SubsidyTableSkeletonLoader_Unloaded;
        }

        private void SubsidyTableSkeletonLoader_Loaded(object sender, RoutedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                _pulseStoryboard?.Begin(this, true);
            }
        }

        private void SubsidyTableSkeletonLoader_Unloaded(object sender, RoutedEventArgs e)
        {
            _pulseStoryboard?.Stop(this);
        }

        private void SubsidyTableSkeletonLoader_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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
