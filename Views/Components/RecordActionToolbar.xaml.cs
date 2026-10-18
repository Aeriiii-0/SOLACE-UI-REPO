using System;
using System.Windows;
using System.Windows.Controls;
using SOLUM_UI.ViewModels;
using SOLUM_UI.Views.Dialogs;

namespace SOLUM_UI.Views.Components
{
    public partial class RecordActionToolbar : UserControl
    {
        /// <summary>
        /// Typed shortcut to the inherited DataContext.
        /// The parent RecordViewDialog sets DataContext = RecordViewDialogViewModel,
        /// which is automatically inherited by this UserControl.
        /// </summary>
        private RecordViewDialogViewModel Vm => DataContext as RecordViewDialogViewModel;

        public RecordActionToolbar()
        {
            InitializeComponent();
            Loaded += (s, e) => UpdateDeleteButtonVisibility();
        }

        private void UpdateDeleteButtonVisibility()
        {
            if (BtnDelete == null) return;
            bool isAdmin = MainWindow.CurrentUserRole != null &&
                           (MainWindow.CurrentUserRole.Equals("Administrator", System.StringComparison.OrdinalIgnoreCase) ||
                            MainWindow.CurrentUserRole.Equals("Admin", System.StringComparison.OrdinalIgnoreCase));

            if (!isAdmin)
            {
                BtnDelete.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnDelete.Visibility = (Vm != null && Vm.IsToolbarCollapsed) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        // ── Toolbar toggle ────────────────────────────────────────────────────
        private void BtnToolbarToggle_Click(object sender, RoutedEventArgs e)
        {
            Vm?.HandleToolbarToggle();
            UpdateDeleteButtonVisibility();
            // ActionToolbar.Width stays fixed at 58 (both states) per original.
            if (ActionToolbar == null) return;
            ActionToolbar.Width = 58;

            // Original tooltip-template probe preserved verbatim.
            if (BtnToolbarToggle.ToolTip is ToolTip t1)
            {
                if (t1.Template != null)
                {
                    t1.ApplyTemplate();
                    var tb = t1.Template.FindName("Content", t1) as TextBlock;
                }
            }
        }

        private void Renew_Click(object sender, RoutedEventArgs e)
        {
            if (Vm?.Record == null || string.IsNullOrWhiteSpace(Vm.Record.Id)) return;

            var parentWindow = Window.GetWindow(this);
            var methodDialog = new RenewalMethodDialog
            {
                Owner = parentWindow?.Owner ?? parentWindow
            };
            if (methodDialog.ShowDialog() != true) return;

            if (methodDialog.SelectedChoice == RenewalChoice.QuickRenew)
            {
                string personName = !string.IsNullOrWhiteSpace(Vm.Record.Name) ? Vm.Record.Name : "this solo parent";
                string nextExpiry = DateTime.Today.AddYears(1).ToString("MMMM d, yyyy");
                const string renewIcon = "M12,4V1L8,5L12,9V6C15.31,6 18,8.69 18,12C18,13.04 17.73,14.01 17.27,14.86L18.74,16.33C19.54,15.08 20,13.6 20,12C20,7.58 16.42,4 12,4M12,18C8.69,18 6,15.31 6,12C6,10.96 6.27,9.99 6.73,9.14L5.26,7.67C4.46,8.92 4,10.4 4,12C4,16.42 7.58,20 12,20V23L16,19L12,15V18Z";

                bool isConfirmed = Views.Dialogs.ActionConfirmDialog.Show(
                    title: "Confirm Quick Renewal",
                    message: $"Are you sure you want to renew the solo parent ID card for {personName}?\n\nThis will automatically extend record validity by 1 year until {nextExpiry} with no changes reported.",
                    confirmText: "Renew Record",
                    cancelText: "Cancel",
                    theme: Views.Dialogs.ConfirmThemeType.Success,
                    owner: parentWindow,
                    customIconData: renewIcon);

                if (!isConfirmed) return;

                Vm.HandleQuickRenew();
                return;
            }

            if (methodDialog.SelectedChoice == RenewalChoice.ReassessAndRenew)
            {
                var dialog = new RenewalReassessmentDialog(Vm.Record)
                {
                    Owner = parentWindow?.Owner ?? parentWindow
                };
                if (dialog.ShowDialog() != true || dialog.UpdatedRecord == null) return;

                Vm.HandleRenewResult(dialog.UpdatedRecord);
            }
        }

        // ── Edit ──────────────────────────────────────────────────────────────
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            // VM sets OpenedEdit = true and fires RequestClose(true).
            Vm?.HandleEdit();
        }

        // ── Delete (shows overlay) ────────────────────────────────────────────
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            bool isAdmin = MainWindow.CurrentUserRole != null &&
                           (MainWindow.CurrentUserRole.Equals("Administrator", System.StringComparison.OrdinalIgnoreCase) ||
                            MainWindow.CurrentUserRole.Equals("Admin", System.StringComparison.OrdinalIgnoreCase));
            if (!isAdmin) return;

            // VM sets IsDeleteOverlayVisible = true.
            // The overlay's Visibility is bound to that property in the parent XAML.
            Vm?.HandleDeleteRequest();
        }
    }
}
