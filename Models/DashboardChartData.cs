using System;
using System.Collections.Generic;
using System.Linq;

namespace SOLUM_UI.Models
{
    /// <summary>
    /// Comprehensive model for all dashboard chart data.
    /// Contains aggregated statistics from solo parent records.
    /// </summary>
    public class DashboardChartData
    {
        // Age Breakdown
        public int AgeBelow19 { get; set; } = 0;
        public int Age20To39 { get; set; } = 118;
        public int Age40To59 { get; set; } = 287;
        public int Age60Plus { get; set; } = 20;

        // Civil Status Distribution
        public int CivilStatusSingle { get; set; } = 175;
        public int CivilStatusMarried { get; set; } = 105;
        public int CivilStatusWidowed { get; set; } = 137;
        public int CivilStatusSeparatedAnnulled { get; set; } = 8;

        // Top Solo Parent Categories (in descending order)
        public List<CategoryCount> TopCategories { get; set; } = new List<CategoryCount>
        {
            new CategoryCount { Category = "Unmarried Person", Count = 159 },
            new CategoryCount { Category = "Widow/Widower", Count = 148 },
            new CategoryCount { Category = "Abandoned", Count = 100 },
            new CategoryCount { Category = "Spouse of PDL", Count = 5 },
            new CategoryCount { Category = "Spouse of PWD", Count = 4 }
        };

        // Number of Children/Dependents
        public int ChildrenBelow6 { get; set; } = 78;
        public int Children7To22 { get; set; } = 630;
        public int ChildrenAbove22 { get; set; } = 71;

        // Solo Parents by Sex (2025 Data)
        public int SexFemale { get; set; } = 380;
        public int SexMale { get; set; } = 45;

        // Monthly Income vs. Employment (grouped data)
        public List<IncomeEmploymentGroup> IncomeEmploymentData { get; set; } = new List<IncomeEmploymentGroup>
        {
            new IncomeEmploymentGroup { IncomeRange = "Not Employed, <Min Wage", Count = 150 },
            new IncomeEmploymentGroup { IncomeRange = "Not Employed, Min-20k", Count = 120 },
            new IncomeEmploymentGroup { IncomeRange = "Not Employed, >20k", Count = 30 },
            new IncomeEmploymentGroup { IncomeRange = "Employed, <Min Wage", Count = 45 },
            new IncomeEmploymentGroup { IncomeRange = "Employed, Min-20k", Count = 180 },
            new IncomeEmploymentGroup { IncomeRange = "Employed, >20k", Count = 95 },
            new IncomeEmploymentGroup { IncomeRange = "Self-Employed, <Min Wage", Count = 60 },
            new IncomeEmploymentGroup { IncomeRange = "Self-Employed, Min-20k", Count = 110 },
            new IncomeEmploymentGroup { IncomeRange = "Self-Employed, >20k", Count = 125 }
        };

        // Yearly Applications (2021-2025) for line chart
        public List<YearlyApplicationCount> YearlyApplications { get; set; } = new List<YearlyApplicationCount>
        {
            new YearlyApplicationCount { Year = 2021, Count = 220 },
            new YearlyApplicationCount { Year = 2022, Count = 280 },
            new YearlyApplicationCount { Year = 2023, Count = 310 },
            new YearlyApplicationCount { Year = 2024, Count = 290 },
            new YearlyApplicationCount { Year = 2025, Count = 325 }
        };

        // Calculated properties
        public int TotalSoloParents => AgeBelow19 + Age20To39 + Age40To59 + Age60Plus;
        public int TotalCivilStatus => CivilStatusSingle + CivilStatusMarried + CivilStatusWidowed + CivilStatusSeparatedAnnulled;
        public int TotalChildren => ChildrenBelow6 + Children7To22 + ChildrenAbove22;
        public int TotalByGender => SexFemale + SexMale;
    }

    /// <summary>Category and count pair</summary>
    public class CategoryCount
    {
        public string Category { get; set; }
        public int Count { get; set; }
    }

    /// <summary>Income-Employment cross-reference group</summary>
    public class IncomeEmploymentGroup
    {
        public string IncomeRange { get; set; }
        public int Count { get; set; }
    }

    /// <summary>Yearly application count for trends</summary>
    public class YearlyApplicationCount
    {
        public int Year { get; set; }
        public int Count { get; set; }
    }
}
