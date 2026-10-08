using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Threading;
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
        private bool      _toolbarCollapsed;
        private bool      _isDeleteOverlayVisible;

        // Backing fields for observable record-level properties
        private string    _applicationType;
        private string    _status;
        private DateTime  _lastUpdated;
        private DateTime? _validUntil;

        // Backing fields for notification card
        private bool   _isNotificationVisible;
        private string _notificationTitle;
        private string _notificationMessage;
        private bool   _isNotificationSuccess;
        private DispatcherTimer _notifyTimer;
        private DispatcherTimer _closeTimer;

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

        /// <summary>
        /// Application type label shown in the dialog header.
        /// Writing this property also updates the canonical flags on the underlying
        /// model so that <see cref="SoloParentRecord.ApplicationType"/> stays in sync.
        /// Accepted values: "New Record", "Renewed".  Any other value leaves the
        /// model flags untouched (guards against accidental UI-only strings).
        /// </summary>
        public string ApplicationType
        {
            get => _applicationType;
            private set
            {
                if (_applicationType == value) return;
                _applicationType = value;

                // Keep model flags consistent with the display value so that
                // record.ApplicationType (the computed property) reflects the
                // same state and callers can rely on either surface.
                switch (value)
                {
                    case "New Record":
                        _record.IsNewApplicant = true;
                        _record.IsRenewal      = false;
                        break;
                    case "Renewed":
                        _record.IsNewApplicant = false;
                        _record.IsRenewal      = true;
                        break;
                }

                OnPropertyChanged(nameof(ApplicationType));
            }
        }

        /// <summary>Record status. Auto-set to "Inactive" on expiry; "Active" on renewal. Synced back to model.</summary>
        public string Status
        {
            get => _status;
            private set
            {
                if (_status == value) return;
                _status        = value;
                _record.Status = value;   // keep model in sync for code-behind readers
                OnPropertyChanged(nameof(Status));
            }
        }

        /// <summary>Date the record was last updated. Synced to model; updated immediately on renewal.</summary>
        public DateTime LastUpdated
        {
            get => _lastUpdated;
            private set
            {
                if (_lastUpdated == value) return;
                _lastUpdated        = value;
                _record.LastUpdated = value;
                OnPropertyChanged(nameof(LastUpdated));
            }
        }

        /// <summary>
        /// Term expiry (maps to CurrentTermExpiresAt). Set to LastUpdated + 1 yr on renewal.
        /// Triggers an immediate expiration check so <see cref="Status"/> is kept consistent.
        /// </summary>
        public DateTime? ValidUntil
        {
            get => _validUntil;
            private set
            {
                if (_validUntil == value) return;
                _validUntil                  = value;
                _record.CurrentTermExpiresAt = value;
                OnPropertyChanged(nameof(ValidUntil));
                EvaluateExpiration();   // re-evaluate Active/Inactive whenever validity changes
            }
        }

        // ── Constructor ──────────────────────────────────────────────────────
        public RecordViewDialogViewModel(SoloParentRecord record)
        {
            _record          = record;
            _applicationType = record.ApplicationType;
            _status          = record.Status;
            _lastUpdated     = record.LastUpdated;
            _validUntil      = record.CurrentTermExpiresAt;
            EvaluateExpiration();
        }

        /// <summary>
        /// Exposes the raw model so the code-behind card-builder helpers can
        /// read it during Phase 2 without holding their own reference.
        /// Will be removed in Phase 4 when cards become data-bound UserControls.
        /// </summary>
        public SoloParentRecord Record => _record;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void EvaluateExpiration()
        {
            // Only set Inactive; never override an explicit "Active" stamp made
            // by a renewal that hasn't persisted yet — renewal sets ValidUntil
            // to future before calling this, so the guard is naturally safe.
            if (_validUntil.HasValue && DateTime.Now > _validUntil.Value)
                Status = "Inactive";
        }

        // ── Notification card properties ──────────────────────────────────────
        public bool   IsNotificationVisible
        {
            get => _isNotificationVisible;
            private set { _isNotificationVisible = value; OnPropertyChanged(nameof(IsNotificationVisible)); }
        }
        public string NotificationTitle
        {
            get => _notificationTitle;
            private set { _notificationTitle = value; OnPropertyChanged(nameof(NotificationTitle)); }
        }
        public string NotificationMessage
        {
            get => _notificationMessage;
            private set { _notificationMessage = value; OnPropertyChanged(nameof(NotificationMessage)); }
        }
        public bool IsNotificationSuccess
        {
            get => _isNotificationSuccess;
            private set { _isNotificationSuccess = value; OnPropertyChanged(nameof(IsNotificationSuccess)); }
        }

        /// <summary>Shows the notification card and auto-hides it after 3 seconds.</summary>
        public void ShowNotification(string title, string message, bool isSuccess)
        {
            NotificationTitle     = title;
            NotificationMessage   = message;
            IsNotificationSuccess = isSuccess;
            IsNotificationVisible = true;

            _notifyTimer?.Stop();
            _notifyTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _notifyTimer.Tick += (s, e) => { _notifyTimer.Stop(); IsNotificationVisible = false; };
            _notifyTimer.Start();
        }

        /// <summary>
        /// Shows the notification card then fires <see cref="RequestClose"/> after
        /// <paramref name="closeDelaySecs"/> seconds so the user can read the feedback
        /// before the dialog disappears.
        /// </summary>
        private void ShowThenClose(string title, string message, bool isSuccess,
                                   double closeDelaySecs, bool dialogResult)
        {
            ShowNotification(title, message, isSuccess);

            _closeTimer?.Stop();
            _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(closeDelaySecs) };
            _closeTimer.Tick += (s, e) => { _closeTimer.Stop(); RequestClose?.Invoke(dialogResult); };
            _closeTimer.Start();
        }

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
            // ── Stamp dates and status on the result the caller will persist ──
            renewalResult.LastUpdated          = DateTime.Now;
            renewalResult.CurrentTermExpiresAt = renewalResult.LastUpdated.AddYears(1);
            renewalResult.Status               = "Active";
            renewalResult.IsRenewal            = true;
            renewalResult.IsNewApplicant       = false;

            // ── Mirror onto INPC properties for immediate UI update ───────────
            // Order matters: set ValidUntil before Status so the EvaluateExpiration
            // call inside the ValidUntil setter sees the new future date and does
            // not override the "Active" we are about to assign.
            LastUpdated     = renewalResult.LastUpdated;
            ValidUntil      = renewalResult.CurrentTermExpiresAt;   // triggers EvaluateExpiration
            Status          = renewalResult.Status;                  // "Active" — wins over any expiry check
            // Derive from the model's computed property to keep a single source of truth.
            ApplicationType = renewalResult.ApplicationType;         // "Renewed" (IsRenewal=true)

            RenewalResult = renewalResult;
            OpenedRenew   = true;
            Renewed       = true;
            ShowThenClose("Record Renewed",
                "The solo parent record has been successfully renewed.", true,
                closeDelaySecs: 1.2, dialogResult: true);
        }

        /// <summary>Signals the parent page to open the record in edit mode.</summary>
        public void HandleEdit()
        {
            OpenedEdit = true;
            ShowThenClose("Opening Editor",
                "Loading the record in edit mode.", true,
                closeDelaySecs: 1.0, dialogResult: true);
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
        /// fires <see cref="RequestClose"/> with <c>DialogResult = true</c> after
        /// briefly showing a notification so the user sees confirmation feedback.
        /// </summary>
        public void HandleDeleteConfirm()
        {
            IsDeleteOverlayVisible = false;
            OpenedDelete = true;
            ShowThenClose("Record Deleted",
                "The solo parent record has been permanently removed.", false,
                closeDelaySecs: 1.2, dialogResult: true);
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
