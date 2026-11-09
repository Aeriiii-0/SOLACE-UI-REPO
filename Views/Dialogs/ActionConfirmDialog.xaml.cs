using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SOLUM_UI.Views.Dialogs
{
    public enum ConfirmThemeType
    {
        Primary,   // Solum Maroon / Burgundy (#702943)
        Success,   // Green (#27AE60)
        Warning,   // Amber / Orange (#E67E22)
        Danger     // Red (#C62828 / #D32F2F)
    }

    public partial class ActionConfirmDialog : Window
    {
        public bool Confirmed { get; private set; }

        public ActionConfirmDialog()
        {
            InitializeComponent();
            KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    if (BtnCancel.Visibility == Visibility.Visible)
                        BtnCancel_Click(s, e);
                    else
                        BtnConfirm_Click(s, e);
                }
            };
        }
        /// <summary>
        /// Show a confirmation modal dialog with customizable theme and options.
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Message body text</param>
        /// <param name="confirmText">Confirm button label (default: "Confirm")</param>
        /// <param name="cancelText">Cancel button label (default: "Cancel")</param>
        /// <param name="theme">Color theme based on action type (default: Primary/Maroon)</param>
        /// <param name="owner">Parent window for modal centering (auto-detects MainWindow if null)</param>
        /// <param name="customIconData">Optional custom SVG path data for icon (default: theme-based icon)</param>
        /// <returns>true if user confirmed, false if cancelled</returns>
        public static bool Show(
            string title,
            string message,
            string confirmText = "Confirm",
            string cancelText = "Cancel",
            ConfirmThemeType theme = ConfirmThemeType.Primary,
            Window owner = null,
            string customIconData = null)
        {
            var dialog = new ActionConfirmDialog();
            if (owner != null)
            {
                dialog.Owner = owner;
            }
            else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                dialog.Owner = Application.Current.MainWindow;
            }
            dialog.TxtTitle.Text = title;
            dialog.TxtMessage.Text = message;
            dialog.TxtConfirmBtn.Text = confirmText;
            dialog.TxtCancelBtn.Text = cancelText;
            dialog.BtnCancel.Visibility = Visibility.Visible;
            Grid.SetColumn(dialog.BtnConfirm, 2);
            Grid.SetColumnSpan(dialog.BtnConfirm, 1);
            dialog.BtnConfirm.HorizontalAlignment = HorizontalAlignment.Stretch;
            dialog.BtnConfirm.Width = double.NaN;
            dialog.ApplyTheme(theme);
            if (!string.IsNullOrWhiteSpace(customIconData))
            {
                dialog.IconPath.Data = Geometry.Parse(customIconData);
            }
            dialog.ShowDialog();
            return dialog.Confirmed;
        }
        /// <summary>
        /// Show an alert/notification dialog (OK-only button).
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Message body text</param>
        /// <param name="theme">Color theme (default: Primary)</param>
        /// <param name="owner">Parent window</param>
        public static void Alert(
            string title,
            string message,
            ConfirmThemeType theme = ConfirmThemeType.Primary,
            Window owner = null)
        {
            void DoAlert()
            {
                var dialog = new ActionConfirmDialog();
                if (owner != null && owner.IsVisible)
                {
                    dialog.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
                dialog.TxtTitle.Text = title;
                dialog.TxtMessage.Text = message;
                dialog.TxtConfirmBtn.Text = "OK";
                dialog.TxtCancelBtn.Text = "";
                dialog.BtnCancel.Visibility = Visibility.Collapsed;
                // Center the single confirmation/OK button across the footer
                Grid.SetColumn(dialog.BtnConfirm, 0);
                Grid.SetColumnSpan(dialog.BtnConfirm, 3);
                dialog.BtnConfirm.HorizontalAlignment = HorizontalAlignment.Center;
                dialog.BtnConfirm.Width = 180;
                dialog.ApplyTheme(theme);
                if (theme == ConfirmThemeType.Danger)
                {
                    // Use error circle-X icon for error alert notifications
                    dialog.IconPath.Data = Geometry.Parse("M12,2C17.53,2 22,6.47 22,12C22,17.53 17.53,22 12,22C6.47,22 2,17.53 2,12C2,6.47 6.47,2 12,2M15.59,7L12,10.59L8.41,7L7,8.41L10.59,12L7,15.59L8.41,17L12,13.41L15.59,17L17,15.59L13.41,12L17,8.41L15.59,7Z");
                }
                dialog.ShowDialog();
            }

            if (Application.Current != null && Application.Current.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DoAlert);
            }
            else
            {
                DoAlert();
            }
        }
        /// <summary>
        /// Show a success notification.
        /// </summary>
        public static void Success(string title, string message, Window owner = null)
        {
            Alert(title, message, ConfirmThemeType.Success, owner);
        }
        /// <summary>
        /// Show an error notification.
        /// </summary>
        public static void Error(string title, string message, Window owner = null)
        {
            Alert(title, message, ConfirmThemeType.Danger, owner);
        }
        /// <summary>
        /// Show a warning notification.
        /// </summary>
        public static void Warning(string title, string message, Window owner = null)
        {
            Alert(title, message, ConfirmThemeType.Warning, owner);
        }
        /// <summary>
        /// Show an info notification.
        /// </summary>
        public static void Info(string title, string message, Window owner = null)
        {
            Alert(title, message, ConfirmThemeType.Primary, owner);
        }

        private void ApplyTheme(ConfirmThemeType theme)
        {
            string primaryColor;
            string circleBg;
            string iconData;
            switch (theme)
            {
                case ConfirmThemeType.Success:
                    primaryColor = "#27AE60";
                    circleBg = "#E8F5E9";
                    // Check mark icon
                    iconData = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M11,16.5L6.5,12L7.91,10.59L11,13.67L16.59,8.09L18,9.5L11,16.5Z";
                    break;
                case ConfirmThemeType.Warning:
                    primaryColor = "#D97706";
                    circleBg = "#FEF3C7";
                    // Alert triangle icon
                    iconData = "M12,2L1,21H23M12,6L19.53,19H4.47M11,10V14H13V10M11,16V18H13V16";
                    break;
                case ConfirmThemeType.Danger:
                    primaryColor = "#C62828";
                    circleBg = "#FFEBEE";
                    // Trash / Delete / Danger icon
                    iconData = "M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z";
                    break;
                case ConfirmThemeType.Primary:
                default:
                    primaryColor = "#702943";
                    circleBg = "#F0E8EC";
                    // Solum Question / Info icon
                    iconData = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M11,16.5L6.5,12L7.91,10.59L11,13.67L16.59,8.09L18,9.5L11,16.5Z";
                    break;
            }
            var colorBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(primaryColor);
            var circleBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(circleBg);
            // Store the theme color in window Tag for binding
            this.Tag = colorBrush;
            IconCircle.Background = circleBrush;
            IconPath.Fill = colorBrush;
            IconPath.Data = Geometry.Parse(iconData);
            Shell.BorderBrush = colorBrush;
            BtnConfirm.Background = colorBrush;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            DialogResult = true;
            Close();
        }
    }
}