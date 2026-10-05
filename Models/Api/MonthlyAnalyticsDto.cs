using System;
using System.Collections.Generic;

namespace SOLUM_UI.Models.Api
{
    public class MonthlyAnalyticsDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Barangay { get; set; } = "ALL";
        public int TotalSoloParents { get; set; }
        public int ActiveSoloParents { get; set; }
        public int InactiveSoloParents { get; set; }
        public int NewRegistrations { get; set; }
        public int Renewals { get; set; }

        public Dictionary<string, int> Sex { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> CivilStatus { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> EmploymentStatus { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> AgeBrackets { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> MonthlyIncomeBrackets { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> MonthlyIncome
        {
            get => MonthlyIncomeBrackets;
            set => MonthlyIncomeBrackets = value;
        }
        public Dictionary<string, int> DependentsAgeBrackets { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> Categories { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ClassificationCircumstance
        {
            get => Categories;
            set => Categories = value;
        }
        public Dictionary<string, int> EducationalAttainment { get; set; } = new Dictionary<string, int>();

        // Sector breakdowns
        public Dictionary<string, int> Sectors { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> Lgbt { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> LgbtBreakdown
        {
            get => Lgbt;
            set => Lgbt = value;
        }
        public Dictionary<string, int> PantawidBeneficiary { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> PantawidBreakdown
        {
            get => PantawidBeneficiary;
            set => PantawidBeneficiary = value;
        }
        public Dictionary<string, int> Indigenous { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> IndigenousBreakdown
        {
            get => Indigenous;
            set => Indigenous = value;
        }
    }

    public class DashboardAnalyticsDto
    {
        public int TotalRegistered { get; set; }
        public int ActiveRecords { get; set; }
        public int InactiveRecords { get; set; }
        public int RenewalsDueThisMonth { get; set; }
        public int RegisteredThisMonth { get; set; }
        public int RenewalsThisMonth { get; set; }
        public int RenewalsCompletedToday { get; set; }
        public int ActiveChangeFromLastMonth { get; set; }
    }
}