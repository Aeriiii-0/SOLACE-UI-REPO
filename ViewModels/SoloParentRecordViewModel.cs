using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using SOLUM_UI.Models;

namespace SOLUM_UI.ViewModels
{
    public class SoloParentRecordViewModel : INotifyPropertyChanged
    {
        private readonly SoloParentRecord _model;
        private bool _isAlternate;
        private bool _isSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public SoloParentRecordViewModel(SoloParentRecord model)
        {
            _model = model;
        }

        public string Id => _model.Id;
        public string Name => _model.Name;
        public string Surname => _model.Surname;
        public string FirstName => _model.FirstName;
        public string MiddleName => _model.MiddleName;
        public string Barangay => _model.Barangay;
        public string Sex => _model.Sex;
        public string CivilStatus => _model.CivilStatus;
        public string ContactNumber => SOLUM_UI.Services.OcrService.FormatPhoneNumber(_model.ContactNumber);
        public string Address => _model.Address;
        public int Children => _model.Children;
        public string Status => _model.Status;
        public string LastUpdatedFormatted => _model.LastUpdatedFormatted;
        public string DateOfBirthFormatted => _model.DateOfBirthFormatted;
        public DateTime DateOfApplication => _model.DateOfApplication;
        public string ApplicationDateFormatted => _model.ApplicationDateFormatted;
        public string ApplicationDateIso => _model.ApplicationDateIso;
        public string DateCreatedIso => _model.DateCreatedIso;
        public string ValidUntilFormatted =>
            _model.CurrentTermExpiresAt.HasValue
                ? _model.CurrentTermExpiresAt.Value.ToString("yyyy-MM-dd")
                : _model.LastUpdated != System.DateTime.MinValue
                    ? _model.LastUpdated.AddYears(1).ToString("yyyy-MM-dd")
                    : "—";
        /// <summary>True when the record's validity window has passed or record is marked inactive/expired.</summary>
        public bool IsExpired
        {
            get
            {
                // PRIORITY 1: Check actual expiry date first (most reliable)
                if (_model.CurrentTermExpiresAt.HasValue)
                {
                    bool isExpiredByDate = _model.CurrentTermExpiresAt.Value.Date < DateTime.Today;
                    // If we have a future expiry date, record is NOT expired regardless of status
                    if (!isExpiredByDate)
                        return false;
                    // If expiry date is in the past, it's expired
                    return true;
                }
                
                // PRIORITY 2: Check fallback expiry (LastUpdated + 1 year)
                if (_model.LastUpdated != DateTime.MinValue)
                {
                    bool isExpiredByLastUpdated = _model.LastUpdated.AddYears(1).Date < DateTime.Today;
                    if (!isExpiredByLastUpdated)
                        return false;
                    return true;
                }
                
                // PRIORITY 3: Only as last resort, check Status field
                if (string.Equals(_model.Status, "Expired", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(_model.Status, "Inactive", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                
                return false;
            }
        }

        public SoloParentRecord RawModel => _model;
        /// <summary>True when eligible for renewal (active >= 1 month, expiring within 30 days, or expired/inactive/cancelled).</summary>
        public bool CanRenew => _model.CanRenew(out _, out _);

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(RowBackground));
            }
        }

        public void SetAlternate(bool value)
        {
            _isAlternate = value;
        }

        private static readonly SolidColorBrush SelectedRowBrush = CreateFrozen(0xF0, 0xE4, 0xE8);
        private static readonly SolidColorBrush AlternateRowBrush = CreateFrozen(0xFD, 0xF8, 0xF9);
        private static readonly SolidColorBrush WhiteBrush = Brushes.White;

        private static readonly SolidColorBrush StatusValidBadgeBrush = CreateFrozen(0xDC, 0xFC, 0xE7);
        private static readonly SolidColorBrush StatusExpiringBadgeBrush = CreateFrozen(0xFE, 0xF3, 0xC7);
        private static readonly SolidColorBrush StatusExpiredBadgeBrush = CreateFrozen(0xFF, 0xE4, 0xE6);
        private static readonly SolidColorBrush StatusInactiveBadgeBrush = CreateFrozen(0xF1, 0xF5, 0xF9);
        private static readonly SolidColorBrush TransparentBrush = Brushes.Transparent;

        private static readonly SolidColorBrush StatusValidTextBrush = CreateFrozen(0x15, 0x80, 0x3D);
        private static readonly SolidColorBrush StatusExpiringTextBrush = CreateFrozen(0xB4, 0x53, 0x09);
        private static readonly SolidColorBrush StatusExpiredTextBrush = CreateFrozen(0x99, 0x1B, 0x1B);
        private static readonly SolidColorBrush StatusInactiveTextBrush = CreateFrozen(0x64, 0x74, 0x8B);
        private static readonly SolidColorBrush BlackBrush = Brushes.Black;

        private static readonly SolidColorBrush StatusValidDotBrush = CreateFrozen(0x22, 0xC5, 0x5E);
        private static readonly SolidColorBrush StatusExpiringDotBrush = CreateFrozen(0xF5, 0x9E, 0x0B);
        private static readonly SolidColorBrush StatusExpiredDotBrush = CreateFrozen(0xEF, 0x44, 0x44);
        private static readonly SolidColorBrush StatusInactiveDotBrush = CreateFrozen(0x94, 0xA3, 0xB8);

        private static SolidColorBrush CreateFrozen(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        public SolidColorBrush RowBackground
        {
            get
            {
                if (_isSelected)
                    return SelectedRowBrush;
                return _isAlternate ? AlternateRowBrush : WhiteBrush;
            }
        }
        /// <summary>True when expiration date is within 30 days from today (or explicitly marked expiring) and not yet expired.</summary>
        public bool IsExpiringSoon
        {
            get
            {
                if (IsExpired) return false;
                if (string.Equals(_model.Status, "Expiring", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(_model.Status, "ExpiringSoon", StringComparison.OrdinalIgnoreCase))
                    return true;
                DateTime? exp = _model.CurrentTermExpiresAt;
                if (!exp.HasValue && _model.LastUpdated != DateTime.MinValue)
                    exp = _model.LastUpdated.AddYears(1);
                if (exp.HasValue)
                {
                    double days = (exp.Value.Date - DateTime.Today).TotalDays;
                    return days >= 0 && days <= 30;
                }
                return false;
            }
        }
        /// <summary>Friendly human-readable status label with relative hints.</summary>
        public string StatusDisplay
        {
            get
            {
                if (IsExpired) return "Expired";
                if (IsExpiringSoon) return "Expiring";
                if (string.Equals(_model.Status, "Valid", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(_model.Status, "Active", StringComparison.OrdinalIgnoreCase))
                    return "Active";
                return _model.Status ?? "Inactive";
            }
        }
        /// <summary>Formatted valid until date with relative hint (e.g. 'in 29d').</summary>
        public string ValidUntilDisplay
        {
            get
            {
                DateTime? exp = _model.CurrentTermExpiresAt;
                if (!exp.HasValue && _model.LastUpdated != DateTime.MinValue)
                    exp = _model.LastUpdated.AddYears(1);
                if (!exp.HasValue) return "—";

                string dateStr = exp.Value.ToString("MMM dd, yyyy");
                if (IsExpired) return $"{dateStr} (Expired)";
                if (IsExpiringSoon)
                {
                    int days = (int)Math.Max(0, (exp.Value.Date - DateTime.Today).TotalDays);
                    return days == 0 ? $"{dateStr} (Today)" : $"{dateStr} (in {days}d)";
                }
                return dateStr;
            }
        }

        public SolidColorBrush StatusBadgeColor
        {
            get
            {
                if (IsExpired) return StatusExpiredBadgeBrush;
                if (IsExpiringSoon) return StatusExpiringBadgeBrush;
                if (string.Equals(_model.Status, "Valid", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(_model.Status, "Active", StringComparison.OrdinalIgnoreCase))
                    return StatusValidBadgeBrush;
                return StatusInactiveBadgeBrush;
            } 
        }

        public SolidColorBrush StatusTextColor
        {
            get
            {
                if (IsExpired) return StatusExpiredTextBrush;
                if (IsExpiringSoon) return StatusExpiringTextBrush;
                if (string.Equals(_model.Status, "Valid", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(_model.Status, "Active", StringComparison.OrdinalIgnoreCase))
                    return StatusValidTextBrush;
                return StatusInactiveTextBrush;
            }
        }

        public SolidColorBrush StatusDotBrush
        {
            get
            {
                if (IsExpired) return StatusExpiredDotBrush;
                if (IsExpiringSoon) return StatusExpiringDotBrush;
                if (string.Equals(_model.Status, "Valid", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(_model.Status, "Active", StringComparison.OrdinalIgnoreCase))
                    return StatusValidDotBrush;
                return StatusInactiveDotBrush;
            }
        }
    }
}