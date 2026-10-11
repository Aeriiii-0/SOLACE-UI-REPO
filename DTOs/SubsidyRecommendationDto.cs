using System;
using System.Collections.Generic;

namespace SOLUM_UI.DTOs
{
    public class SubsidyRecommendationDto
    {
        private string _spId;
        private string _name;
        private string _priority;
        private double _score;
        private double _confidence = 0.85;
        private int _dependants;
        private int _minorDependentsCount;
        private int _toddlersUnder5Count;
        private int _collegeAgeDependentsCount;

        // Backend Primary Identifiers
        public Guid EvaluationId { get; set; }
        public Guid CycleId { get; set; }
        public Guid SoloParentId { get; set; }

        public string FullName { get; set; }
        public string Barangay { get; set; }
        public string ContactNumber { get; set; }
        public string SubsidyType { get; set; } = "Allowance";

        // ML Inference Results
        public string PredictedPriority { get; set; }
        public decimal PriorityScore { get; set; }
        public decimal ModelConfidence { get; set; }
        public bool NeedsCloserReview { get; set; }
        public decimal ProbabilityLow { get; set; }
        public decimal ProbabilityMedium { get; set; }
        public decimal ProbabilityHigh { get; set; }
        public string ModelVersion { get; set; }
        public DateTime ScoredAt { get; set; }

        // Demographic & Economics
        public double MonthlyIncome { get; set; }
        public double IncomePerCapita { get; set; }
        public string EmploymentStatus { get; set; } = "Unemployed";
        public int ChildrenTotal { get; set; }
        public int Children0To6 { get; set; }
        public int Children7To22 { get; set; }
        public int Children23Plus { get; set; }
        public string HousingTenure { get; set; } = "Rented";
        public string Circumstance { get; set; } = string.Empty;
        public string CivilStatus { get; set; }
        public string Sex { get; set; } = "Female";
        public string OtherIncomeSource { get; set; } = string.Empty;
        public string NeedsAndProblems { get; set; } = string.Empty;
        public List<ChildDetailDto> Children { get; set; } = new List<ChildDetailDto>();

        // Explainability Scores
        public double EconomicStrainScore { get; set; } = 0.50;
        public double DependencyBurdenScore { get; set; } = 0.50;
        public double CareBurdenScore { get; set; } = 0.50;
        public double ResourceAdequacyScore { get; set; } = 0.50;

        // Lifecycle & Audit Flags
        public string Status { get; set; }
        public int? SelectionRank { get; set; }
        public bool IsWaitlisted { get; set; }
        public bool HasCollegeDependent { get; set; }
        public int CollegeDependentsCount { get; set; }
        public bool IsPantawidBeneficiary { get; set; }
        public string AuditStatus { get; set; }
        public string RejectionReason { get; set; }
        public string RejectionNotes { get; set; }
        public string ReviewedBy { get; set; }
        public DateTime? ConfirmedAt { get; set; }

        // UI Compatibility Fallbacks
        public string SpId { get => _spId ?? (SoloParentId != Guid.Empty ? "SP-" + SoloParentId.ToString().Substring(0, 8).ToUpper() : string.Empty); set => _spId = value; }
        public string Name { get => !string.IsNullOrEmpty(FullName) ? FullName : _name; set => _name = value; }
        public string Priority { get => !string.IsNullOrEmpty(PredictedPriority) ? PredictedPriority : _priority; set => _priority = value; }
        public double Score { get => PriorityScore > 0 ? (double)PriorityScore : _score; set => _score = value; }
        public double Confidence { get => ModelConfidence > 0 ? (double)ModelConfidence : _confidence; set => _confidence = value; }
        public int Dependants { get => ChildrenTotal > 0 ? ChildrenTotal : (_dependants > 0 ? _dependants : (Children0To6 + Children7To22)); set => _dependants = value; }
        public int MinorDependentsCount { get => Children0To6 + Children7To22 > 0 ? Children0To6 + Children7To22 : _minorDependentsCount; set => _minorDependentsCount = value; }
        public int ToddlersUnder5Count { get => Children0To6 > 0 ? Children0To6 : _toddlersUnder5Count; set => _toddlersUnder5Count = value; }
        public int CollegeAgeDependentsCount { get => CollegeDependentsCount > 0 ? CollegeDependentsCount : (HasCollegeDependent ? 1 : _collegeAgeDependentsCount); set => _collegeAgeDependentsCount = value; }
        public bool HasCollegeAgeDependent => HasCollegeDependent || CollegeAgeDependentsCount > 0;
        public List<string> TopFactors { get; set; } = new List<string>();
    }

    public class ChildDetailDto
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
