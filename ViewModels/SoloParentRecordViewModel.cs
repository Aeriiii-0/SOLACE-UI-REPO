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
        public string ValidUntilFormatted =>
            _model.CurrentTermExpiresAt.HasValue
                ? _model.CurrentTermExpiresAt.Value.ToString("yyyy-MM-dd")
                : _model.LastUpdated != System.DateTime.MinValue
                    ? _model.LastUpdated.AddYears(1).ToString("yyyy-MM-dd")
                    : "—";

        /// <summary>True when the record's validity window has passed.</summary>
        public bool IsExpired =>
            _model.CurrentTermExpiresAt.HasValue
                ? _model.CurrentTermExpiresAt.Value.Date < System.DateTime.Today
                : _model.LastUpdated != System.DateTime.MinValue &&
                  _model.LastUpdated.AddYears(1).Date < System.DateTime.Today;

        public SoloParentRecord RawModel => _model;

        /// <summary>True when eligible for renewal (active >= 1 month or expired/inactive).</summary>
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

        private static readonly SolidColorBrush StatusValidBadgeBrush = CreateFrozen(0xE6, 0xF9, 0xEE);
        private static readonly SolidColorBrush StatusInactiveBadgeBrush = CreateFrozen(0xF5, 0xF5, 0xF5);
        private static readonly SolidColorBrush TransparentBrush = Brushes.Transparent;

        private static readonly SolidColorBrush StatusValidTextBrush = CreateFrozen(0x27, 0xAE, 0x60);
        private static readonly SolidColorBrush StatusInactiveTextBrush = CreateFrozen(0x99, 0x99, 0x99);
        private static readonly SolidColorBrush BlackBrush = Brushes.Black;

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

        public SolidColorBrush StatusBadgeColor
        {
            get
            {
                switch (_model.Status)
                {
                    case "Valid": return StatusValidBadgeBrush;
                    case "Inactive": return StatusInactiveBadgeBrush;
                    default: return TransparentBrush;
                }
            } 
        }

        public SolidColorBrush StatusTextColor
        {
            get
            {
                switch (_model.Status)
                {
                    case "Valid": return StatusValidTextBrush;
                    case "Inactive": return StatusInactiveTextBrush;
                    default: return BlackBrush;
                }
            }
        }
    }
}
