using System;
using System.ComponentModel;
using System.Windows.Media;
using SOLUM_UI.Models;

namespace SOLUM_UI.ViewModels
{
    /// <summary>
    /// Presentation state and action handlers for <see cref="SOLUM_UI.RecordViewDialog"/>.
    /// All business-rule logic and result flags are preserved verbatim from the
    /// original code-behind. UI-layout helpers (ApplySize) and the programmatic
    /// card-builder methods remain in code-behind until Phase 4 componentisation.
    /// </summary>
    public class RecordViewDialogViewModel : INotifyPropertyChanged
    {
        // ── Private backing ──────────────────────────────────────────────────
        private readonly SoloParentRecord _record;
        private bool _toolbarCollapsed;
        private bool _isDeleteOverlayVisible;

        // ── INotifyPropertyChanged ───────────────────────────────────────────
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ── Result signals (inspected by caller after ShowDialog()) ──────────
        public bool OpenedEdit    { get; private set; }
        public bool Renewed       { get; private set; }   // kept for compat
        public bool OpenedRenew   { get; private set; }
        public bool OpenedDelete  { get; private set; }
        public SoloParentRecord RenewalResult { get; private set; }

        // ── Colour palette (presentation constants) ──────────────────────────
        // Declared internal so the code-behind card-builder helpers can still
        // reference them during Phase 2 without duplication.
        internal static readonly SolidColorBrush BrLabel   = Clr(0x88, 0x88, 0x88);
        internal static readonly SolidColorBrush BrValue   = Clr(0x11, 0x11, 0x11);
        internal static readonly SolidColorBrush BrCardBdr = Clr(0x70, 0x29, 0x43);
        internal static readonly SolidColorBrush BrDivider = Clr(0xE8, 0xD8, 0xDE);
        internal static readonly SolidColorBrush BrHead    = Clr(0x70, 0x29, 0x43);
        internal static readonly SolidColorBrush BrChipBg  = Clr(0xF5, 0xEC, 0xEF);
        internal static readonly SolidColorBrush BrChipTxt = Clr(0x70, 0x29, 0x43);
        internal static readonly SolidColorBrush BrRowAlt  = Clr(0xFD, 0xF5, 0xF7);
        internal static readonly SolidColorBrush BrSecBg   = Clr(0xFF, 0xFF, 0xFF);

        /// <summary>Factory helper preserved verbatim from the original code-behind.</summary>
        internal static SolidColorBrush Clr(byte r, byte g, byte b) =>
            new SolidColorBrush(Color.FromRgb(r, g, b));

        // ── Observable state ─────────────────────────────────────────────────

        /// <summary>True when the action toolbar is in its collapsed (icon-only) state.</summary>
        public bool IsToolbarCollapsed
        {
            get => _toolbarCollapsed;
            private set
            {
                if (_toolbarCollapsed == value) return;
                _toolbarCollapsed = value;
                OnPropertyChanged(nameof(IsToolbarCollapsed));
            }
        }

        /// <summary>True while the in-dialog delete-confirmation overlay is visible.</summary>
        public bool IsDeleteOverlayVisible
        {
            get => _isDeleteOverlayVisible;
            private set
            {
                if (_isDeleteOverlayVisible == value) return;
                _isDeleteOverlayVisible = value;
                OnPropertyChanged(nameof(IsDeleteOverlayVisible));
            }
        }

        // ── Close event (code-behind subscribes to drive the Window) ─────────
        /// <summary>
        /// Fired whenever the dialog should close.
        /// The <c>bool</c> argument is the desired <see cref="System.Windows.Window.DialogResult"/>.
        /// </summary>
        public event Action<bool> RequestClose;

        // ── Constructor ──────────────────────────────────────────────────────
        public RecordViewDialogViewModel(SoloParentRecord record)
        {
            _record = record;
        }

        /// <summary>
        /// Exposes the raw model so the code-behind card-builder helpers can
        /// read it during Phase 2 without holding their own reference.
        /// This accessor will be removed in Phase 4 when the cards become
        /// data-bound <c>UserControl</c> components.
        /// </summary>
        public SoloParentRecord Record => _record;

        // ── Action methods (called by code-behind *_Click handlers) ──────────

        /// <summary>Toggles the action toolbar between collapsed and expanded.</summary>
        public void HandleToolbarToggle()
        {
            IsToolbarCollapsed = !IsToolbarCollapsed;
        }

        /// <summary>
        /// Called by the code-behind after a successful <see cref="RecordDialog"/>
        /// renewal. Sets result flags and fires <see cref="RequestClose"/> with
        /// <c>DialogResult = true</c>.
        /// </summary>
        public void HandleRenewResult(SoloParentRecord renewalResult)
        {
            RenewalResult = renewalResult;
            OpenedRenew   = true;
            RequestClose?.Invoke(true);
        }

        /// <summary>Signals the parent page to open the record in edit mode.</summary>
        public void HandleEdit()
        {
            OpenedEdit = true;
            RequestClose?.Invoke(true);
        }

        /// <summary>Makes the in-dialog delete-confirmation overlay visible.</summary>
        public void HandleDeleteRequest()
        {
            IsDeleteOverlayVisible = true;
        }

        /// <summary>Dismisses the delete-confirmation overlay without deleting.</summary>
        public void HandleDeleteCancel()
        {
            IsDeleteOverlayVisible = false;
        }

        /// <summary>
        /// Confirms deletion: collapses the overlay, sets the delete flag, and
        /// fires <see cref="RequestClose"/> with <c>DialogResult = true</c>.
        /// </summary>
        public void HandleDeleteConfirm()
        {
            IsDeleteOverlayVisible = false;
            OpenedDelete = true;
            RequestClose?.Invoke(true);
        }

        /// <summary>
        /// Closes the dialog without any action
        /// (<c>DialogResult = false</c>).
        /// </summary>
        public void HandleClose()
        {
            RequestClose?.Invoke(false);
        }
    }
}
