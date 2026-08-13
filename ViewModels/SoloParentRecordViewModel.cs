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
        public string ContactNumber => _model.ContactNumber;
        public string Address => _model.Address;
        public int Children => _model.Children;
        public string Status => _model.Status;
        public string LastUpdatedFormatted => _model.LastUpdatedFormatted;
        public string DateOfBirthFormatted => _model.DateOfBirthFormatted;
        public string ValidUntilFormatted  =>
            _model.LastUpdated != System.DateTime.MinValue
                ? _model.LastUpdated.AddYears(1).ToString("yyyy-MM-dd")
                : "—";
        public SoloParentRecord RawModel => _model;

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

        public SolidColorBrush RowBackground
        {
            get
            {
                if (_isSelected)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0E4E8"));
                return _isAlternate
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDF8F9"))
                    : new SolidColorBrush(Colors.White);
            }
        }

        public SolidColorBrush StatusBadgeColor
        {
            get
            {
                switch (_model.Status)
                {
                    case "Valid": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6F9EE"));
                    case "Inactive": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
                    default: return new SolidColorBrush(Colors.Transparent);
                }
            } 
        }

        public SolidColorBrush StatusTextColor
        {
            get
            {
                switch (_model.Status)
                {
                    case "Valid": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                    case "Inactive": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
                    default: return new SolidColorBrush(Colors.Black);
                }
            }
        }
    }
}
