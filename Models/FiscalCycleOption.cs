using System;
using SOLUM_UI.Core;

namespace SOLUM_UI.Models
{
    public class FiscalCycleOption : ObservableObject
    {
        public Guid? CycleId { get; set; }
        public int FiscalYear { get; set; }
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

        public string DisplayName =>
            IsActive ? $"{FiscalYear} (Active Cycle)" :
            IsFinalized ? $"{FiscalYear} (Finalized)" :
            $"{FiscalYear} (Archived)";

        public override string ToString() => DisplayName;

        public override bool Equals(object obj)
        {
            if (obj is FiscalCycleOption other)
            {
                return FiscalYear == other.FiscalYear &&
                       ((CycleId == null && other.CycleId == null) ||
                        (CycleId != null && other.CycleId != null && CycleId.Value == other.CycleId.Value));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return FiscalYear.GetHashCode() ^ (CycleId?.GetHashCode() ?? 0);
        }
    }
}
