using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SOLUM_UI.Views.Components
{
    public partial class ArchiveTableSkeletonLoader : UserControl
    {
        private Storyboard _pulseStoryboard;

        public ArchiveTableSkeletonLoader()
        {
            InitializeComponent();
            _pulseStoryboard = TryFindResource("PulseStoryboard") as Storyboard;
            IsVisibleChanged += ArchiveTableSkeletonLoader_IsVisibleChanged;
            Loaded += ArchiveTableSkeletonLoader_Loaded;
            Unloaded += ArchiveTableSkeletonLoader_Unloaded;
        }

        private void ArchiveTableSkeletonLoader_Loaded(object sender, RoutedEventArgs e)
        {
            if (Visibility == Visibility.Visible && !SOLUM_UI.Services.AppSettingsService.Instance.ReduceMotion)
            {
                _pulseStoryboard?.Begin(this, true);
            }
        }

        private void ArchiveTableSkeletonLoader_Unloaded(object sender, RoutedEventArgs e)
        {
            _pulseStoryboard?.Stop(this);
        }

        private void ArchiveTableSkeletonLoader_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && !SOLUM_UI.Services.AppSettingsService.Instance.ReduceMotion)
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
