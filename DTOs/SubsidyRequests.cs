using System;
using System.Collections.Generic;

namespace SOLUM_UI.DTOs
{
    public class ShortlistRequest
    {
        public Guid? CycleId { get; set; }
        public List<Guid> EvaluationIds { get; set; } = new List<Guid>();
    }

    public class RejectOutlierRequest
    {
        public Guid EvaluationId { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
        public string RejectionNotes { get; set; }
        public bool AutoPromote { get; set; } = true;
    }

    public class ConfirmGranteesRequest
    {
        public Guid? CycleId { get; set; }
        public List<Guid> EvaluationIds { get; set; } = new List<Guid>();
    }

    public class CreateSubsidyCycleRequest
    {
        public int FiscalYear { get; set; }
        public decimal TotalBudget { get; set; } = 100000.00m;
        public decimal PerGranteeAmount { get; set; } = 1000.00m;
        public int? AllocatedSlots { get; set; }
    }

    public class RevokeGrantRequest
    {
        public string Reason { get; set; }
        public string Remarks { get; set; }
    }
}
