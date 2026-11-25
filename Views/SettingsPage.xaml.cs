using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SOLUM_UI.Services;
using SOLUM_UI.Views;

namespace SOLUM_UI
{
    public partial class SettingsPage : Page
    {
        private readonly SolidColorBrush _activeScaleBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#702943"));
        private readonly SolidColorBrush _activeScaleFg = new SolidColorBrush(Colors.White);
        private readonly SolidColorBrush _inactiveScaleBg = Brushes.Transparent;
        private readonly SolidColorBrush _inactiveScaleFg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"));

        public SettingsPage()
        {
            InitializeComponent();
            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshUIFromSettings();
        }

        private void RefreshUIFromSettings()
        {
            var settings = AppSettingsService.Instance;

            // Update scale buttons
            UpdateScaleButtons(settings.TextScale);

            // Update auto-lock buttons
            UpdateAutoLockButtons(settings.AutoLockMinutes);

            // Update toggles
            ToggleHighContrast.IsChecked = settings.HighContrast;
            ToggleBolderText.IsChecked = settings.BolderText;
            ToggleReduceMotion.IsChecked = settings.ReduceMotion;
            ToggleSpaciousTouch.IsChecked = settings.SpaciousTouch;
            ToggleSingleClick.IsChecked = settings.SingleClickOpen;
            ToggleFocusOutlines.IsChecked = settings.StrongFocusOutlines;
            ToggleAnonymizeSubsidy.IsChecked = settings.AnonymizeSubsidy;
        }

        private void UpdateScaleButtons(double scale)
        {
            ResetScaleButtonStyle(BtnScale100);
            ResetScaleButtonStyle(BtnScale115);
            ResetScaleButtonStyle(BtnScale130);
            ResetScaleButtonStyle(BtnScale150);

            if (scale >= 1.45)
                SetActiveScaleButtonStyle(BtnScale150);
            else if (scale >= 1.25)
                SetActiveScaleButtonStyle(BtnScale130);
            else if (scale >= 1.10)
                SetActiveScaleButtonStyle(BtnScale115);
            else
                SetActiveScaleButtonStyle(BtnScale100);
        }

        private void SetActiveScaleButtonStyle(Button btn)
        {
            if (btn == null) return;
            btn.Background = _activeScaleBg;
            btn.Foreground = _activeScaleFg;
            btn.FontWeight = FontWeights.Bold;
        }

        private void ResetScaleButtonStyle(Button btn)
        {
            if (btn == null) return;
            btn.Background = _inactiveScaleBg;
            btn.Foreground = _inactiveScaleFg;
            btn.FontWeight = FontWeights.SemiBold;
        }

        private void BtnScale_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tagStr)
            {
                if (double.TryParse(tagStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double scale))
                {
                    AppSettingsService.Instance.TextScale = scale;
                    UpdateScaleButtons(scale);
                }
            }
        }

        private void UpdateAutoLockButtons(int minutes)
        {
            ResetScaleButtonStyle(BtnLockNever);
            ResetScaleButtonStyle(BtnLock5Min);
            ResetScaleButtonStyle(BtnLock15Min);
            ResetScaleButtonStyle(BtnLock30Min);

            switch (minutes)
            {
                case 0:
                    SetActiveScaleButtonStyle(BtnLockNever);
                    break;
                case 5:
                    SetActiveScaleButtonStyle(BtnLock5Min);
                    break;
                case 30:
                    SetActiveScaleButtonStyle(BtnLock30Min);
                    break;
                case 15:
                default:
                    SetActiveScaleButtonStyle(BtnLock15Min);
                    break;
            }
        }

        private void BtnAutoLock_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tagStr && int.TryParse(tagStr, out int minutes))
            {
                AppSettingsService.Instance.AutoLockMinutes = minutes;
                UpdateAutoLockButtons(minutes);
                string toastMsg = minutes > 0 ? $"Screen will lock automatically after {minutes} minutes of inactivity." : "Session auto-lock has been disabled.";
                ToastNotification.Show("Auto-Lock Preference", toastMsg, ToastType.Info);
            }
        }

        private void ToggleSetting_Click(object sender, RoutedEventArgs e)
        {
            var settings = AppSettingsService.Instance;

            if (sender == ToggleHighContrast)
            {
                settings.HighContrast = ToggleHighContrast.IsChecked == true;
            }
            else if (sender == ToggleBolderText)
            {
                settings.BolderText = ToggleBolderText.IsChecked == true;
            }
            else if (sender == ToggleReduceMotion)
            {
                settings.ReduceMotion = ToggleReduceMotion.IsChecked == true;
            }
            else if (sender == ToggleSpaciousTouch)
            {
                settings.SpaciousTouch = ToggleSpaciousTouch.IsChecked == true;
            }
            else if (sender == ToggleSingleClick)
            {
                settings.SingleClickOpen = ToggleSingleClick.IsChecked == true;
            }
            else if (sender == ToggleFocusOutlines)
            {
                settings.StrongFocusOutlines = ToggleFocusOutlines.IsChecked == true;
            }
            else if (sender == ToggleAnonymizeSubsidy)
            {
                settings.AnonymizeSubsidy = ToggleAnonymizeSubsidy.IsChecked == true;
            }
        }

        private void BtnRestoreDefaults_Click(object sender, RoutedEventArgs e)
        {
            AppSettingsService.Instance.ResetToDefaults();
            RefreshUIFromSettings();
            ToastNotification.Show("Settings Restored", "All visual and control settings have been reset to factory defaults.", ToastType.Success);
        }
    }
}
