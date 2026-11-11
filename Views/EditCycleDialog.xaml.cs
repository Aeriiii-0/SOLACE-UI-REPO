using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using SOLUM_UI.DTOs;

namespace SOLUM_UI.Views
{
    public partial class EditCycleDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private SubsidyCycleDto _cycleData;
        private int _targetGrantees;
        private decimal _grantAmountPerRecipient;
        private decimal _totalBudget;

        public int TargetGrantees
        {
            get => _targetGrantees;
            set
            {
                if (_targetGrantees != value)
                {
                    _targetGrantees = value;
                    OnPropertyChanged();
                    CalculateTotalBudget();
                }
            }
        }

        public decimal GrantAmountPerRecipient
        {
            get => _grantAmountPerRecipient;
            set
            {
                if (_grantAmountPerRecipient != value)
                {
                    _grantAmountPerRecipient = value;
                    OnPropertyChanged();
                    CalculateTotalBudget();
                }
            }
        }

        public decimal TotalBudget
        {
            get => _totalBudget;
            private set
            {
                if (_totalBudget != value)
                {
                    _totalBudget = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalBudgetFormatted));
                }
            }
        }

        public string TotalBudgetFormatted => TotalBudget.ToString("N2", CultureInfo.InvariantCulture);

        public bool IsSaved { get; private set; }

        public EditCycleDialog(SubsidyCycleDto cycleData)
        {
            InitializeComponent();
            DataContext = this;

            _cycleData = cycleData ?? throw new ArgumentNullException(nameof(cycleData));

            // Initialize fields with current cycle data
            string semLabel = _cycleData.CycleNumber == 1 ? "1st Semester" : _cycleData.CycleNumber == 2 ? "2nd Semester" : $"Cycle {_cycleData.CycleNumber}";
            TxtFiscalYear.Text = !string.IsNullOrWhiteSpace(_cycleData.Name) ? _cycleData.Name : $"Fiscal Year {_cycleData.FiscalYear} - {semLabel}";
            TargetGrantees = _cycleData.AllocatedSlots;
            GrantAmountPerRecipient = _cycleData.PerGranteeAmount;

            // Set initial values to TextBoxes
            TxtTargetGrantees.Text = TargetGrantees.ToString();
            TxtGrantAmount.Text = GrantAmountPerRecipient.ToString("F2");
        }

        private void CalculateTotalBudget()
        {
            TotalBudget = TargetGrantees * GrantAmountPerRecipient;
        }

        private void InputField_TextChanged(object sender, TextChangedEventArgs e)
        {
            TxtValidation.Text = string.Empty;

            // Parse Target Grantees
            if (sender == TxtTargetGrantees)
            {
                if (int.TryParse(TxtTargetGrantees.Text, out int slots))
                {
                    TargetGrantees = slots;
                }
            }
            // Parse Grant Amount
            else if (sender == TxtGrantAmount)
            {
                if (decimal.TryParse(TxtGrantAmount.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                {
                    GrantAmountPerRecipient = amount;
                }
            }
        }

        private bool ValidateInputs()
        {
            // Validate Target Grantees
            if (!int.TryParse(TxtTargetGrantees.Text, out int slots) || slots <= 0)
            {
                TxtValidation.Text = "Target grantees must be a positive number.";
                return false;
            }

            // Validate Grant Amount
            if (!decimal.TryParse(TxtGrantAmount.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
            {
                TxtValidation.Text = "Grant amount must be a positive number.";
                return false;
            }

            // Check for reasonable limits
            if (slots > 10000)
            {
                TxtValidation.Text = "Target grantees seems too high. Please verify.";
                return false;
            }

            if (amount > 1000000)
            {
                TxtValidation.Text = "Grant amount per recipient seems too high. Please verify.";
                return false;
            }

            return true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
            {
                return;
            }

            // Update the cycle data
            _cycleData.AllocatedSlots = TargetGrantees;
            _cycleData.PerGranteeAmount = GrantAmountPerRecipient;
            _cycleData.TotalBudget = TotalBudget;

            IsSaved = true;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsSaved = false;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, false);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            IsSaved = false;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, false);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
