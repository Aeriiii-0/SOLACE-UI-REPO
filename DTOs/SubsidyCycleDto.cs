using System;

namespace SOLUM_UI.DTOs
{
    public class SubsidyCycleDto
    {
        public Guid Id { get; set; }
        public int FiscalYear { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal PerGranteeAmount { get; set; }
        public int AllocatedSlots { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public int TotalEvaluated { get; set; }
        public int TotalShortlisted { get; set; }
        public int TotalWaitlisted { get; set; }
        public int TotalFinalGrantees { get; set; }
        public int RemainingSlots => Math.Max(0, AllocatedSlots - TotalShortlisted - TotalFinalGrantees);
    }
}
