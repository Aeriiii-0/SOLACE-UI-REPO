using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Components
{
    public partial class SubsidyExplainDrawer : UserControl
    {
        private Guid _lastCandidateId = Guid.Empty;
        private SubsidyRecommendationViewModel _currentVm;

        public SubsidyExplainDrawer()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                if (!(RenderTransform is TranslateTransform))
                {
                    RenderTransform = new TranslateTransform();
                }
                HookViewModel(DataContext as SubsidyRecommendationViewModel);
            };

            DataContextChanged += (s, e) =>
            {
                HookViewModel(e.NewValue as SubsidyRecommendationViewModel);
            };

            IsVisibleChanged += (s, e) =>
            {
                if (e.NewValue is bool isVis && isVis)
                {
                    if (_currentVm?.ActiveCandidate != null)
                    {
                        _lastCandidateId = _currentVm.ActiveCandidate.SoloParentId;
                    }
                    DrawerScrollViewer?.ScrollToTop();
                    PlaySlideAnimation(380);
                }
                else
                {
                    _lastCandidateId = Guid.Empty;
                }
            };
        }

        private void HookViewModel(SubsidyRecommendationViewModel vm)
        {
            if (_currentVm != null)
            {
                _currentVm.PropertyChanged -= OnViewModelPropertyChanged;
            }
            _currentVm = vm;
            if (_currentVm != null)
            {
                _currentVm.PropertyChanged += OnViewModelPropertyChanged;
                CheckAndAnimateCandidateChange();
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SubsidyRecommendationViewModel.ActiveCandidate))
            {
                CheckAndAnimateCandidateChange();
            }
        }

        private void CheckAndAnimateCandidateChange()
        {
            if (Visibility != Visibility.Visible || _currentVm == null) return;
            var candidate = _currentVm.ActiveCandidate;
            if (candidate == null) return;

            if (_lastCandidateId != Guid.Empty && _lastCandidateId != candidate.SoloParentId)
            {
                DrawerScrollViewer?.ScrollToTop();
                PlaySlideAnimation(280);
            }
            _lastCandidateId = candidate.SoloParentId;
        }

        private void PlaySlideAnimation(double fromX)
        {
            var tt = RenderTransform as TranslateTransform;
            if (tt == null)
            {
                tt = new TranslateTransform();
                RenderTransform = tt;
            }
            tt.BeginAnimation(TranslateTransform.XProperty, null);
            tt.X = fromX;
            var anim = new DoubleAnimation
            {
                From = fromX,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };
            tt.BeginAnimation(TranslateTransform.XProperty, anim);
        }
    }
}
