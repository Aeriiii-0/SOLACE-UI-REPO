using System;
using SOLUM_UI.Core;

namespace SOLUM_UI.Models
{
    public class FiscalCycleOption : ObservableObject
    {
        public Guid? CycleId { get; set; }
        public int FiscalYear { get; set; }
        public int CycleNumber { get; set; } = 1;
        public int Semester => CycleNumber;
        public string Name { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        private string _status = "Archived";
        public string Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(IsActive));
                    OnPropertyChanged(nameof(IsFinalized));
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }
        public int AllocatedSlots { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal PerGranteeAmount { get; set; } = 1000m;

        public bool IsActive => string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase);
        public bool IsFinalized => string.Equals(Status, "Finalized", StringComparison.OrdinalIgnoreCase);

        public string SemesterLabel => CycleNumber == 1 ? "1st Sem" : CycleNumber == 2 ? "2nd Sem" : $"Cycle {CycleNumber}";

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    return $"{Name} ({(IsActive ? "Active" : Status)})";
                }
                return IsActive ? $"{FiscalYear} - {SemesterLabel} (Active)" :
                       IsFinalized ? $"{FiscalYear} - {SemesterLabel} (Finalized)" :
                       $"{FiscalYear} - {SemesterLabel} (Archived)";
            }
        }

        public override string ToString() => DisplayName;

        public override bool Equals(object obj)
        {
            if (obj is FiscalCycleOption other)
            {
                if (CycleId.HasValue && other.CycleId.HasValue)
                {
                    return CycleId.Value == other.CycleId.Value;
                }
                return FiscalYear == other.FiscalYear && CycleNumber == other.CycleNumber;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (CycleId?.GetHashCode() ?? (FiscalYear.GetHashCode() ^ CycleNumber.GetHashCode()));
        }
    }
}
