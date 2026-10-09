using System.Collections.Generic;

namespace SOLUM_UI.DTOs
{
    public class SubsidyRecommendationDto
    {
        public string SpId { get; set; }
        public string Name { get; set; }
        public string Barangay { get; set; }
        public string SubsidyType { get; set; } = "Allowance";
        public string Priority { get; set; }
        public double Score { get; set; }
        public double Confidence { get; set; } = 0.85;

        public double MonthlyIncome { get; set; }
        public double IncomePerCapita { get; set; }
        public string EmploymentStatus { get; set; } = "Unemployed";
        public int Dependants { get; set; }
        public int MinorDependentsCount { get; set; }
        public int ToddlersUnder5Count { get; set; }
        public int CollegeAgeDependentsCount { get; set; }
        public string HousingTenure { get; set; } = "Rented";
        public string Circumstance { get; set; } = "Abandonment (A2)";
        public string CivilStatus { get; set; }
        public string Sex { get; set; } = "Female";
        public string OtherIncomeSource { get; set; } = "None";
        public string NeedsAndProblems { get; set; } = "Financial & Livelihood Aid";
        public List<ChildDetailDto> Children { get; set; } = new List<ChildDetailDto>();

        public double EconomicStrainScore { get; set; } = 0.50;
        public double DependencyBurdenScore { get; set; } = 0.50;
        public double CareBurdenScore { get; set; } = 0.50;
        public double ResourceAdequacyScore { get; set; } = 0.50;

        public List<string> TopFactors { get; set; } = new List<string>();
        public bool HasCollegeAgeDependent => CollegeAgeDependentsCount > 0;
        public bool IsPantawidBeneficiary { get; set; }
    }

    public class ChildDetailDto
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
