using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SOLUM_UI.Views.Components
{
    public partial class UserTableSkeletonLoader : UserControl
    {
        private Storyboard _pulseStoryboard;

        public UserTableSkeletonLoader()
        {
            InitializeComponent();
            _pulseStoryboard = TryFindResource("PulseStoryboard") as Storyboard;

            IsVisibleChanged += UserTableSkeletonLoader_IsVisibleChanged;
            Loaded += UserTableSkeletonLoader_Loaded;
            Unloaded += UserTableSkeletonLoader_Unloaded;
        }

        private void UserTableSkeletonLoader_Loaded(object sender, RoutedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                _pulseStoryboard?.Begin(this, true);
            }
        }

        private void UserTableSkeletonLoader_Unloaded(object sender, RoutedEventArgs e)
        {
            _pulseStoryboard?.Stop(this);
        }

        private void UserTableSkeletonLoader_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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
