using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SOLUM_UI.Views.Components
{
    public partial class SubsidyCycleControlBar : UserControl
    {
        private bool _userManuallyToggled = false;
        private Window _parentWindow;

        public SubsidyCycleControlBar()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _parentWindow = Window.GetWindow(this);
            if (_parentWindow != null)
            {
                _parentWindow.StateChanged -= ParentWindow_StateChanged;
                _parentWindow.StateChanged += ParentWindow_StateChanged;
                // Initial state check based on window state
                if (_parentWindow.WindowState == WindowState.Normal && _parentWindow.ActualWidth < 1200)
                {
                    SetExpanded(false);
                }
            }
        }

        private void ParentWindow_StateChanged(object sender, EventArgs e)
        {
            if (sender is Window win)
            {
                // When restored / unmaximized, default to compact/collapsed mode
                if (win.WindowState == WindowState.Normal)
                {
                    _userManuallyToggled = false;
                    SetExpanded(false);
                }
                else if (win.WindowState == WindowState.Maximized)
                {
                    _userManuallyToggled = false;
                    SetExpanded(true);
                }
            }
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_userManuallyToggled)
            {
                if (e.NewSize.Width < 1080)
                {
                    SetExpanded(false);
                }
                else
                {
                    SetExpanded(true);
                }
            }
        }

        private void QuotaHeaderCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject dep)
            {
                // Do not toggle collapse when interacting with buttons, textboxes, or popups
                var parent = dep;
                while (parent != null)
                {
                    if (parent is Button || parent is System.Windows.Controls.Primitives.Popup || parent is TextBox)
                        return;
                    parent = VisualTreeHelper.GetParent(parent);
                }
            }
            _userManuallyToggled = true;
            bool isCurrentlyExpanded = QuotaExpandedPanel != null && QuotaExpandedPanel.Visibility == Visibility.Visible;
            SetExpanded(!isCurrentlyExpanded);
        }

        private void SetExpanded(bool expand)
        {
            if (QuotaExpandedPanel != null)
            {
                QuotaExpandedPanel.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
            }
            if (QuotaChevronIcon != null)
            {
                QuotaChevronIcon.Data = Geometry.Parse(
                    expand
                        ? "M7.41,15.41L12,10.83L16.59,15.41L18,14L12,8L6,14L7.41,15.41Z" // Up chevron (collapse)
                        : "M7.41,8.58L12,13.17L16.59,8.58L18,10L12,16L6,10L7.41,8.58Z" // Down chevron (expand)
                );
            }
            if (QuotaHeaderCard != null)
            {
                QuotaHeaderCard.ToolTip = expand ? "Click to collapse details" : "Click to expand quota & actions";
            }
        }

        private void SlotBreakdownBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SlotPopup != null) SlotPopup.IsOpen = !SlotPopup.IsOpen;
        }

        private void EditQuotaBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EditQuotaPopup != null) EditQuotaPopup.IsOpen = !EditQuotaPopup.IsOpen;
        }

        private void CancelEditQuota_Click(object sender, RoutedEventArgs e)
        {
            if (EditQuotaPopup != null) EditQuotaPopup.IsOpen = false;
        }

        private void SaveEditQuota_Click(object sender, RoutedEventArgs e)
        {
            if (EditQuotaPopup != null) EditQuotaPopup.IsOpen = false;
        }
    }
}