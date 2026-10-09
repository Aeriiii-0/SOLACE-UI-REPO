using System;
using System.Collections.Generic;
using System.Linq;
using SOLUM_UI.DTOs;
using SOLUM_UI.Models;

namespace SOLUM_UI.Services
{
    public static class SubsidyModelMapper
    {
        private static string NormPrio(string p) => string.Equals(p, "HIGH", StringComparison.OrdinalIgnoreCase) ? "High" : (string.Equals(p, "LOW", StringComparison.OrdinalIgnoreCase) ? "Low" : "Medium");
        private static double CalcCapita(double inc, int dep, double existing) => existing > 0 ? existing : ((dep + 1) > 0 ? Math.Round(inc / (dep + 1), 2) : inc);

        public static SubsidyItem ToItem(SubsidyRecommendationDto d) => new SubsidyItem
        {
            EvaluationId = d.EvaluationId, CycleId = d.CycleId, SoloParentId = d.SoloParentId,
            SpId = d.SpId, Name = d.Name, Barangay = d.Barangay, SubsidyType = d.SubsidyType, Priority = NormPrio(d.Priority),
            Score = d.Score, Confidence = d.Confidence, MonthlyIncome = d.MonthlyIncome, IncomePerCapita = CalcCapita(d.MonthlyIncome, d.Dependants, d.IncomePerCapita),
            EmploymentStatus = d.EmploymentStatus, Dependants = d.Dependants, MinorDependentsCount = d.MinorDependentsCount,
            ToddlersUnder5Count = d.ToddlersUnder5Count, CollegeAgeDependentsCount = d.CollegeAgeDependentsCount,
            Circumstance = d.Circumstance, CivilStatus = d.CivilStatus, Sex = d.Sex, OtherIncomeSource = d.OtherIncomeSource,
            NeedsAndProblems = d.NeedsAndProblems, EconomicStrainScore = d.EconomicStrainScore, DependencyBurdenScore = d.DependencyBurdenScore,
            CareBurdenScore = d.CareBurdenScore, ResourceAdequacyScore = d.ResourceAdequacyScore, IsPantawidBeneficiary = d.IsPantawidBeneficiary,
            ModelVersion = d.ModelVersion,
            ChildrenDetails = d.Children?.Select(c => new ChildDetailItem { Name = c.Name, Age = c.Age }).ToList() ?? new List<ChildDetailItem>()
        };

        public static SelectionRow ToRow(SubsidyRecommendationDto d, int rk) => new SelectionRow
        {
            Rank = rk, EvaluationId = d.EvaluationId, CycleId = d.CycleId, SoloParentId = d.SoloParentId,
            SpId = d.SpId, Name = d.Name, Barangay = d.Barangay, Priority = NormPrio(d.Priority), Score = d.Score,
            MonthlyIncome = d.MonthlyIncome, IncomePerCapita = CalcCapita(d.MonthlyIncome, d.Dependants, d.IncomePerCapita), Dependants = d.Dependants,
            MinorDependentsCount = d.MinorDependentsCount, ToddlersUnder5Count = d.ToddlersUnder5Count,
            Circumstance = d.Circumstance, Sex = d.Sex, CivilStatus = d.CivilStatus,
            HasCollegeAgeDependent = d.HasCollegeAgeDependent, CollegeAgeDependentsCount = d.CollegeAgeDependentsCount,
            IsPantawidBeneficiary = d.IsPantawidBeneficiary, IsFinalGrantee = d.Status == "FinalGrantee",
            IsWaitlisted = d.IsWaitlisted || d.Status == "Waitlisted",
            ModelVersion = d.ModelVersion
        };

        public static SelectionRow GranteeToRow(GranteeDto g, int rk) => new SelectionRow
        {
            Rank = rk, EvaluationId = g.EvaluationId, CycleId = g.CycleId, SoloParentId = g.SoloParentId,
            SpId = g.SoloParentId != Guid.Empty ? "SP-" + g.SoloParentId.ToString().Substring(0, 8).ToUpper() : $"SP-{g.FiscalYear}-{rk:D4}",
            Name = g.FullName, Barangay = g.Barangay, Priority = NormPrio(g.PredictedPriority), Score = (double)g.PriorityScore,
            MonthlyIncome = (double)g.MonthlyIncome, IncomePerCapita = CalcCapita((double)g.MonthlyIncome, g.ChildrenTotal, 0),
            Dependants = g.ChildrenTotal, MinorDependentsCount = g.ChildrenTotal,
            IsFinalGrantee = true, IsWaitlisted = false
        };
    }
}
