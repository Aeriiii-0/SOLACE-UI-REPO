using System;
using System.Windows;
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
        }

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

            dialog.ApplyTheme(theme);

            if (!string.IsNullOrWhiteSpace(customIconData))
            {
                dialog.IconPath.Data = Geometry.Parse(customIconData);
            }

            dialog.ShowDialog();
            return dialog.Confirmed;
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
