using System;
using System.Windows;
using System.Windows.Controls;
using SOLUM_UI.ViewModels;
using SOLUM_UI.Views.Dialogs;
using SOLUM_UI.Services.Api;

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
            Loaded += (s, e) =>
            {
                UpdateDeleteButtonVisibility();
                UpdateRenewButtonAppearance();
            };
        }

        private void UpdateRenewButtonAppearance()
        {
            if (BtnRenew == null || Vm?.Record == null) return;
            if (!Vm.Record.CanRenew(out _, out var earliestDate))
            {
                BtnRenew.Opacity = 0.55;
                if (earliestDate.HasValue)
                {
                    BtnRenew.ToolTip = $"Renewal available on {earliestDate.Value:MMM dd, yyyy} (Active < 1 month)";
                }
            }
            else
            {
                BtnRenew.Opacity = 1.0;
            }
        }

        private void UpdateDeleteButtonVisibility()
        {
            if (BtnDelete == null) return;
            bool isAdmin = MainWindow.CurrentUserRole != null &&
                           (MainWindow.CurrentUserRole.Equals("Administrator", System.StringComparison.OrdinalIgnoreCase) ||
                            MainWindow.CurrentUserRole.Equals("Admin", System.StringComparison.OrdinalIgnoreCase) ||
                            MainWindow.CurrentUserRole.Equals("SuperAdmin", System.StringComparison.OrdinalIgnoreCase) ||
                            AuthApiService.Instance.IsAdmin);

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

        private async void Renew_Click(object sender, RoutedEventArgs e)
        {
            if (Vm?.Record == null || string.IsNullOrWhiteSpace(Vm.Record.Id)) return;

            var parentWindow = Window.GetWindow(this);
            var record = Vm.Record;

            // Pre-check with in-memory record first
            if (!record.CanRenew(out string preCheckReason, out _))
            {
                ActionConfirmDialog.Warning(
                    "Renewal Not Available",
                    preCheckReason ?? "This solo parent record has been active for less than a month and cannot be renewed yet.",
                    parentWindow);
                return;
            }

            Models.Api.SoloParentDto existingDto = null;
            if (Guid.TryParse(record.Id, out var guid))
            {
                var fetch = await Services.Api.SoloParentApiService.Instance.GetSoloParentByIdAsync(guid);
                if (fetch.Succeeded && fetch.Data != null)
                {
                    existingDto = fetch.Data;
                    record = Services.Api.SoloParentApiService.Instance.MapToRecord(existingDto);
                }
            }

            // Post-fetch check with latest backend data
            if (!record.CanRenew(out string reason, out _))
            {
                ActionConfirmDialog.Warning(
                    "Renewal Not Available",
                    reason ?? "This solo parent record has been active for less than a month and cannot be renewed yet.",
                    parentWindow);
                return;
            }

            var dialog = new Views.Dialogs.RenewalReassessmentDialog(record, existingDto)
            {
                Owner = parentWindow?.Owner ?? parentWindow
            };
            if (dialog.ShowDialog() != true || dialog.UpdatedRecord == null) return;

            Vm.HandleRenewResult(dialog.UpdatedRecord);
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
                            MainWindow.CurrentUserRole.Equals("Admin", System.StringComparison.OrdinalIgnoreCase) ||
                            MainWindow.CurrentUserRole.Equals("SuperAdmin", System.StringComparison.OrdinalIgnoreCase) ||
                            AuthApiService.Instance.IsAdmin);
            if (!isAdmin) return;

            // VM sets IsDeleteOverlayVisible = true.
            // The overlay's Visibility is bound to that property in the parent XAML.
            Vm?.HandleDeleteRequest();
        }
    }
}
