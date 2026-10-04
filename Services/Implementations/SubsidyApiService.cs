using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SOLUM_UI.DTOs;
using SOLUM_UI.Models;
using SOLUM_UI.Services.Interfaces;

namespace SOLUM_UI.Services.Implementations
{
    public class SubsidyApiService : ISubsidyApiService
    {
        public Task<List<SubsidyRecommendationDto>> GetRecommendationsAsync()
        {
            var list = new List<SubsidyRecommendationDto>
            {
                new SubsidyRecommendationDto { SpId = "SP-2026-0042", Name = "Martinez, Rosa Cruz", Barangay = "San Roque", Priority = "High", Score = 95, Confidence = 0.94, MonthlyIncome = 2800, IncomePerCapita = 560, EmploymentStatus = "Unemployed", Dependants = 4, MinorDependentsCount = 4, ToddlersUnder5Count = 2, Circumstance = "Abandonment (A2)", CivilStatus = "Separated", OtherIncomeSource = "None", NeedsAndProblems = "Food Subsidy & Child Daycare", EconomicStrainScore = 0.97, DependencyBurdenScore = 0.92, CareBurdenScore = 0.95, ResourceAdequacyScore = 0.88, IsPantawidBeneficiary = true, Children = new List<ChildDetailDto> { new ChildDetailDto { Name = "Martinez, Joshua", Age = 2 }, new ChildDetailDto { Name = "Martinez, Angel", Age = 4 }, new ChildDetailDto { Name = "Martinez, Bea", Age = 7 }, new ChildDetailDto { Name = "Martinez, John", Age = 11 } } },
                new SubsidyRecommendationDto { SpId = "SP-2026-0104", Name = "Santos, Maria Lim", Barangay = "San Roque", Priority = "High", Score = 92, Confidence = 0.91, MonthlyIncome = 3500, IncomePerCapita = 1167, EmploymentStatus = "Self-Employed (Vendor)", Dependants = 2, MinorDependentsCount = 2, ToddlersUnder5Count = 1, Circumstance = "Death of Spouse (A1)", CivilStatus = "Widowed", OtherIncomeSource = "None", NeedsAndProblems = "Livelihood & Educational Cash", EconomicStrainScore = 0.94, DependencyBurdenScore = 0.82, CareBurdenScore = 0.88, ResourceAdequacyScore = 0.76, Children = new List<ChildDetailDto> { new ChildDetailDto { Name = "Santos, Miguel", Age = 1 }, new ChildDetailDto { Name = "Santos, Sofia", Age = 6 } } },
                new SubsidyRecommendationDto { SpId = "SP-2026-0088", Name = "Dela Cruz, Juan Reyes Jr.", Barangay = "Poblacion", Priority = "High", Score = 88, Confidence = 0.89, MonthlyIncome = 4200, IncomePerCapita = 1050, EmploymentStatus = "Contractual / Daily Wage", Dependants = 3, MinorDependentsCount = 2, ToddlersUnder5Count = 0, CollegeAgeDependentsCount = 1, Circumstance = "Spouse Incarcerated (A5)", CivilStatus = "Married", OtherIncomeSource = "Relief aid from relatives", NeedsAndProblems = "College Tuition & Legal Support", EconomicStrainScore = 0.89, DependencyBurdenScore = 0.84, CareBurdenScore = 0.65, ResourceAdequacyScore = 0.70, Children = new List<ChildDetailDto> { new ChildDetailDto { Name = "Dela Cruz, Carlo", Age = 19 }, new ChildDetailDto { Name = "Dela Cruz, Patricia", Age = 12 }, new ChildDetailDto { Name = "Dela Cruz, Kevin", Age = 8 } } },
                new SubsidyRecommendationDto { SpId = "SP-2026-0215", Name = "Bautista, Carlos Ocampo", Barangay = "Poblacion", Priority = "High", Score = 84, Confidence = 0.87, MonthlyIncome = 4800, IncomePerCapita = 1200, EmploymentStatus = "Tricycle Driver", Dependants = 3, MinorDependentsCount = 3, ToddlersUnder5Count = 1, Circumstance = "Death of Spouse (A1)", CivilStatus = "Widowed", OtherIncomeSource = "None", NeedsAndProblems = "Fuel Cost Subsidy & Daycare", EconomicStrainScore = 0.85, DependencyBurdenScore = 0.81, CareBurdenScore = 0.74, ResourceAdequacyScore = 0.68, Children = new List<ChildDetailDto> { new ChildDetailDto { Name = "Bautista, Gabriel", Age = 3 }, new ChildDetailDto { Name = "Bautista, Daniel", Age = 8 }, new ChildDetailDto { Name = "Bautista, Chloe", Age = 10 } } },
                new SubsidyRecommendationDto { SpId = "SP-2026-0155", Name = "Flores, Ana Belen", Barangay = "San Isidro", Priority = "High", Score = 81, Confidence = 0.86, MonthlyIncome = 5000, IncomePerCapita = 1250, EmploymentStatus = "Domestic Worker", Dependants = 3, MinorDependentsCount = 3, ToddlersUnder5Count = 1, Circumstance = "Unmarried Mother (A4)", CivilStatus = "Single", OtherIncomeSource = "None", NeedsAndProblems = "Housing & Child Maintenance", EconomicStrainScore = 0.82, DependencyBurdenScore = 0.80, CareBurdenScore = 0.78, ResourceAdequacyScore = 0.65, Children = new List<ChildDetailDto> { new ChildDetailDto { Name = "Flores, Leo", Age = 2 }, new ChildDetailDto { Name = "Flores, Sarah", Age = 7 }, new ChildDetailDto { Name = "Flores, Mark", Age = 9 } } },
                new SubsidyRecommendationDto { SpId = "SP-2026-0312", Name = "Aquino, Marites Villanueva", Barangay = "Maligaya", Priority = "High", Score = 78, Confidence = 0.85, MonthlyIncome = 5200, IncomePerCapita = 1300, EmploymentStatus = "Laundry / Labandera", Dependants = 3, MinorDependentsCount = 3, ToddlersUnder5Count = 0, Circumstance = "Abandonment (A2)", CivilStatus = "Separated", OtherIncomeSource = "None", NeedsAndProblems = "School Supplies & Medical Aid", EconomicStrainScore = 0.80, DependencyBurdenScore = 0.76, CareBurdenScore = 0.70, ResourceAdequacyScore = 0.64, IsPantawidBeneficiary = true, Children = new List<ChildDetailDto> { new ChildDetailDto { Name = "Aquino, Jayson", Age = 9 }, new ChildDetailDto { Name = "Aquino, Jenny", Age = 11 }, new ChildDetailDto { Name = "Aquino, Kyle", Age = 14 } } },
                new SubsidyRecommendationDto { SpId = "SP-2026-0099", Name = "Reyes, Elena Mendoza", Barangay = "San Roque", Priority = "High", Score = 76, Confidence = 0.83, MonthlyIncome = 5500, IncomePerCapita = 1375, EmploymentStatus = "Sari-Sari Store Helper", Dependants = 3, MinorDependentsCount = 2, ToddlersUnder5Count = 0, CollegeAgeDependentsCount = 1, Circumstance = "Spouse Physical Disability (A6)", CivilStatus = "Married", OtherIncomeSource = "Spouse disability allowance (irregular)", NeedsAndProblems = "Medical Supplies & Maintenance", EconomicStrainScore = 0.77, DependencyBurdenScore = 0.75, CareBurdenScore = 0.72, ResourceAdequacyScore = 0.60, Children = new List<ChildDetailDto> { new ChildDetailDto { Name = "Reyes, Christian", Age = 18 }, new ChildDetailDto { Name = "Reyes, Sarah", Age = 14 } } },
                new SubsidyRecommendationDto { SpId = "SP-2026-0188", Name = "Garcia, Roberto Tan", Barangay = "Poblacion", Priority = "Medium", Score = 73, Confidence = 0.82, MonthlyIncome = 6000, IncomePerCapita = 1500, EmploymentStatus = "Carpenter", Dependants = 3, MinorDependentsCount = 3, ToddlersUnder5Count = 0, Circumstance = "Death of Spouse (A1)", CivilStatus = "Widowed", OtherIncomeSource = "Occasional sibling remittance", NeedsAndProblems = "School Uniforms & Tools", EconomicStrainScore = 0.74, DependencyBurdenScore = 0.72, CareBurdenScore = 0.62, ResourceAdequacyScore = 0.58, Children = new List<ChildDetailDto> { new ChildDetailDto { Name = "Garcia, Lucas", Age = 8 }, new ChildDetailDto { Name = "Garcia, Mateo", Age = 11 }, new ChildDetailDto { Name = "Garcia, Hannah", Age = 13 } } },
                new SubsidyRecommendationDto { SpId = "SP-2026-0240", Name = "Torres, Jocelyn Ramos", Barangay = "San Isidro", Priority = "Medium", Score = 70, Confidence = 0.80, MonthlyIncome = 6200, IncomePerCapita = 1550, EmploymentStatus = "Sewing / Dressmaker", Dependants = 3, MinorDependentsCount = 2, ToddlersUnder5Count = 0, CollegeAgeDependentsCount = 1, Circumstance = "Abandonment (A2)", CivilStatus = "Separated", OtherIncomeSource = "None", NeedsAndProblems = "Sewing Machine Capital", EconomicStrainScore = 0.71, DependencyBurdenScore = 0.69, CareBurdenScore = 0.58, ResourceAdequacyScore = 0.55, Children = new List<ChildDetailDto> { new ChildDetailDto { Name = "Torres, Kimberly", Age = 20 }, new ChildDetailDto { Name = "Torres, Ryan", Age = 15 } } },
                new SubsidyRecommendationDto { SpId = "SP-2026-0305", Name = "Villanueva, Remedios Castro", Barangay = "Maligaya", Priority = "Medium", Score = 67, Confidence = 0.81, MonthlyIncome = 6800, IncomePerCapita = 1700, EmploymentStatus = "Canteen Cook", Dependants = 3, MinorDependentsCount = 3, ToddlersUnder5Count = 1, Circumstance = "Unmarried Mother (A4)", CivilStatus = "Single", OtherIncomeSource = "None", NeedsAndProblems = "Child Daycare Assistance", EconomicStrainScore = 0.68, DependencyBurdenScore = 0.66, CareBurdenScore = 0.60, ResourceAdequacyScore = 0.52, Children = new List<ChildDetailDto> { new ChildDetailDto { Name = "Villanueva, Ethan", Age = 3 }, new ChildDetailDto { Name = "Villanueva, Liam", Age = 9 }, new ChildDetailDto { Name = "Villanueva, Mia", Age = 12 } } },
                new SubsidyRecommendationDto { SpId = "SP-2026-0077", Name = "Mendoza, Corazon Diaz", Barangay = "San Roque", Priority = "Medium", Score = 64, Confidence = 0.79, MonthlyIncome = 7200, IncomePerCapita = 1800, EmploymentStatus = "Online Reseller", Dependants = 3, MinorDependentsCount = 3, ToddlersUnder5Count = 0, Circumstance = "Legal Separation (A3)", CivilStatus = "Separated", OtherIncomeSource = "Online shop", NeedsAndProblems = "Working capital", EconomicStrainScore = 0.64, DependencyBurdenScore = 0.63, CareBurdenScore = 0.52, ResourceAdequacyScore = 0.48, Children = new List<ChildDetailDto> { new ChildDetailDto { Name = "Mendoza, Jacob", Age = 7 }, new ChildDetailDto { Name = "Mendoza, Nicole", Age = 10 }, new ChildDetailDto { Name = "Mendoza, Zoe", Age = 15 } } },
                new SubsidyRecommendationDto { SpId = "SP-2026-0410", Name = "Lim, Ferdinand Go", Barangay = "Poblacion", Priority = "Medium", Score = 61, Confidence = 0.78, MonthlyIncome = 7800, IncomePerCapita = 1950, EmploymentStatus = "Delivery Courier", Dependants = 3, MinorDependentsCount = 2, ToddlersUnder5Count = 0, CollegeAgeDependentsCount = 1, Circumstance = "Death of Spouse (A1)", CivilStatus = "Widowed", OtherIncomeSource = "Motorcycle gig", NeedsAndProblems = "Vehicle maintenance", EconomicStrainScore = 0.61, DependencyBurdenScore = 0.60, CareBurdenScore = 0.48, ResourceAdequacyScore = 0.45, Children = new List<ChildDetailDto> { new ChildDetailDto { Name = "Lim, Justin", Age = 19 }, new ChildDetailDto { Name = "Lim, Andrea", Age = 13 } } },
                new SubsidyRecommendationDto { SpId = "SP-2026-0520", Name = "Castillo, Gloria Panganiban", Barangay = "San Isidro", Priority = "Low", Score = 48, Confidence = 0.76, MonthlyIncome = 9500, IncomePerCapita = 2375, EmploymentStatus = "Office Clerk", Dependants = 3, MinorDependentsCount = 3, ToddlersUnder5Count = 0, Circumstance = "Legal Separation (A3)", CivilStatus = "Separated", OtherIncomeSource = "Corporate salary", NeedsAndProblems = "Educational loan", EconomicStrainScore = 0.48, DependencyBurdenScore = 0.50, CareBurdenScore = 0.42, ResourceAdequacyScore = 0.38, Children = new List<ChildDetailDto> { new ChildDetailDto { Name = "Castillo, Beatrice", Age = 10 }, new ChildDetailDto { Name = "Castillo, Dominic", Age = 14 }, new ChildDetailDto { Name = "Castillo, Paula", Age = 16 } } },
                new SubsidyRecommendationDto { SpId = "SP-2026-0618", Name = "Navarro, Eduardo Ramos", Barangay = "Maligaya", Priority = "Low", Score = 42, Confidence = 0.75, MonthlyIncome = 11000, IncomePerCapita = 2750, EmploymentStatus = "Registered Nurse", Dependants = 3, MinorDependentsCount = 3, ToddlersUnder5Count = 0, Circumstance = "Death of Spouse (A1)", CivilStatus = "Widowed", OtherIncomeSource = "Hospital staff salary", NeedsAndProblems = "None reported", EconomicStrainScore = 0.40, DependencyBurdenScore = 0.45, CareBurdenScore = 0.38, ResourceAdequacyScore = 0.30, Children = new List<ChildDetailDto> { new ChildDetailDto { Name = "Navarro, Samantha", Age = 11 }, new ChildDetailDto { Name = "Navarro, Adrian", Age = 13 }, new ChildDetailDto { Name = "Navarro, Nathan", Age = 15 } } }
            };

            return Task.FromResult(list);
        }

        public Task<bool> SubmitShortlistAsync(List<string> spIds) => Task.FromResult(true);
        public Task<bool> RejectOutlierAsync(OutlierRejectionDto rejection) => Task.FromResult(true);
        public Task<List<SelectionRow>> GetShortlistedCandidatesAsync() => Task.FromResult(new List<SelectionRow>());

        public Task<List<SelectionRow>> GetFinalGranteesByYearAsync(int fiscalYear)
        {
            var list = new List<SelectionRow>();
            if (fiscalYear == 2025)
            {
                list.Add(new SelectionRow { Rank = 1, SpId = "SP-2025-0019", Name = "Aquino, Corazon Santos", Barangay = "Poblacion", Priority = "High", Score = 96, IsFinalGrantee = true });
                list.Add(new SelectionRow { Rank = 2, SpId = "SP-2025-0044", Name = "Ramos, Fidelina Cruz", Barangay = "San Roque", Priority = "High", Score = 93, IsFinalGrantee = true });
            }
            else if (fiscalYear == 2024)
            {
                list.Add(new SelectionRow { Rank = 1, SpId = "SP-2024-0011", Name = "Estrada, Luisa Ejercito", Barangay = "San Isidro", Priority = "High", Score = 94, IsFinalGrantee = true });
            }
            return Task.FromResult(list);
        }

        public Task<bool> ConfirmFinalGranteesAsync(List<string> spIds, int fiscalYear) => Task.FromResult(true);

        public Task<string> ExportCityEducRosterCsvAsync(List<SelectionRow> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Rank,SP ID,Candidate Name,Barangay,College-Age Dependents (17-24 yrs),Audit Status,Export Timestamp");
            foreach (var r in items.Where(i => i.HasCollegeAgeDependent))
                sb.AppendLine($"{r.Rank},{r.SpId},\"{r.Name}\",{r.Barangay},{r.CollegeAgeDependentsCount},Requires City Educ Scholarship Verification,{DateTime.Now:yyyy-MM-dd HH:mm}");
            return Task.FromResult(sb.ToString());
        }

        public Task<string> ExportFinalLedgerCsvAsync(List<SelectionRow> items, int fiscalYear)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Fiscal Year {fiscalYear} - Final Grantee Confirmation Ledger");
            sb.AppendLine("Rank,SP ID,Name,Barangay,Score,Priority,Allowance Type,Confirmation Status");
            foreach (var r in items)
                sb.AppendLine($"{r.Rank},{r.SpId},\"{r.Name}\",{r.Barangay},{r.ScoreLabel},{r.Priority},{r.SubsidyType},{(r.IsFinalGrantee ? "Granted" : "Pending Confirmation")}");
            return Task.FromResult(sb.ToString());
        }

        public Task<string> ExportAllQueueCsvAsync(List<SubsidyItem> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Rank,SP ID,Name,Barangay,Score,Status,Circumstance,Income");
            foreach (var item in items)
                sb.AppendLine($"{item.Rank},{item.SpId},\"{item.Name}\",{item.Barangay},{item.ScoreLabel},{item.Status},\"{item.Circumstance}\",\"{item.IncomeLabel}\"");
            return Task.FromResult(sb.ToString());
        }
    }
}
