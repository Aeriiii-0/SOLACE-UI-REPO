using System;

namespace SOLUM_UI.DTOs
{
    public class GranteeDto
    {
        public Guid EvaluationId { get; set; }
        public Guid CycleId { get; set; }
        public Guid SoloParentId { get; set; }
        public int FiscalYear { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string Barangay { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;

        public decimal GranteeAmount { get; set; }
        public decimal PriorityScore { get; set; }
        public string PredictedPriority { get; set; } = string.Empty;

        public decimal MonthlyIncome { get; set; }
        public int ChildrenTotal { get; set; }

        public DateTime? ConfirmedAt { get; set; }
        public string ReviewedBy { get; set; }
    }
}
