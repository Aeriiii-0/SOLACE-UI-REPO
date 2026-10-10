using System;
using SOLUM_UI.Core;

namespace SOLUM_UI.Models
{
    public class FiscalCycleOption : ObservableObject
    {
        public Guid? CycleId { get; set; }
        public int FiscalYear { get; set; }
        public string Status { get; set; } = "Archived";
        public int AllocatedSlots { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal PerGranteeAmount { get; set; } = 1000m;

        public bool IsActive => string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase);

        public string DisplayName => IsActive ? $"{FiscalYear} (Active Cycle)" : $"{FiscalYear} (Archived)";

        public override string ToString() => DisplayName;
    }
}
