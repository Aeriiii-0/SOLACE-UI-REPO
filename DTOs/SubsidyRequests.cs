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
        public int? CycleNumber { get; set; }
        public string Name { get; set; }
        public decimal TotalBudget { get; set; } = 100000.00m;
        public decimal PerGranteeAmount { get; set; } = 1000.00m;
        public int? AllocatedSlots { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; }
    }

    public class UpdateSubsidyCycleRequest
    {
        public int? FiscalYear { get; set; }
        public int? CycleNumber { get; set; }
        public string Name { get; set; }
        public int AllocatedSlots { get; set; }
        public decimal PerGranteeAmount { get; set; }
        public decimal TotalBudget { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; }
    }

    public class RevokeGrantRequest
    {
        public string Reason { get; set; }
        public string Remarks { get; set; }
    }

    public class GetSubsidyRecommendationsRequest
    {
        public Guid? CycleId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "PriorityScore";
        public string SortOrder { get; set; } = "desc";
        public string Search { get; set; }
        public string Barangay { get; set; }
        public string PriorityLevel { get; set; }
        public bool? NeedsCloserReview { get; set; }
        public string EmploymentStatus { get; set; }
        public bool? HasSpecialNeeds { get; set; }
        public decimal? MinIncome { get; set; }
        public decimal? MaxIncome { get; set; }
    }

    public class ShortlistTopQuotaRequest
    {
        public Guid? CycleId { get; set; }
        public int? TopCount { get; set; }
    }

    public class GetSubsidyShortlistRequest
    {
        public Guid? CycleId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string Search { get; set; }
        public string Status { get; set; }
        public string SortBy { get; set; } = "SelectionRank";
        public string SortOrder { get; set; } = "asc";
    }

    public class GetGranteesRequest
    {
        public int? FiscalYear { get; set; }
        public int? CycleNumber { get; set; }
        public Guid? CycleId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string Search { get; set; }
        public string SortBy { get; set; } = "ConfirmedAt";
        public string SortOrder { get; set; } = "desc";
    }
}
