using System.Windows;
using System.Windows.Controls;
using SOLUM_UI.ViewModels;

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
        }

        // ── Toolbar toggle ────────────────────────────────────────────────────
        private void BtnToolbarToggle_Click(object sender, RoutedEventArgs e)
        {
            Vm?.HandleToolbarToggle();
            // Button visibility (BtnRenew / BtnEdit / BtnDelete) is driven by
            // XAML bindings to IsToolbarCollapsed — no manual Visibility
            // assignments are needed here.
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

        // ── Renew ─────────────────────────────────────────────────────────────
        private void Renew_Click(object sender, RoutedEventArgs e)
        {
            if (Vm?.Record == null || string.IsNullOrWhiteSpace(Vm.Record.Id)) return;

            // Open RecordDialog in renewal mode — identity fields locked,
            // only Civil Status / Income / Employment are editable.
            // Window.GetWindow(this) resolves to RecordViewDialog; its Owner
            // mirrors the original `Owner ?? this` logic from the code-behind.
            var parentWindow = Window.GetWindow(this);
            var dialog = new RecordDialog(Vm.Record, isRenewal: true)
            {
                Owner = parentWindow?.Owner ?? parentWindow
            };
            if (dialog.ShowDialog() != true || dialog.Result == null) return;

            // Relay result to VM; it sets flags and fires RequestClose(true).
            Vm.HandleRenewResult(dialog.Result);
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
            // VM sets IsDeleteOverlayVisible = true.
            // The overlay's Visibility is bound to that property in the parent XAML.
            Vm?.HandleDeleteRequest();
        }
    }
}
