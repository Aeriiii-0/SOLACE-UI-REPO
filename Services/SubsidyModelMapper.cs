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
            ModelVersion = d.ModelVersion, Children0To6Count = d.Children0To6, Children7To22Count = d.Children7To22,
            ChildrenDetails = d.Children?.Select(c => new ChildDetailItem { Name = c.Name, Age = c.Age }).ToList() ?? new List<ChildDetailItem>()
        };

        public static SelectionRow ToRow(SubsidyRecommendationDto d, int rk) => new SelectionRow
        {
            Rank = rk, EvaluationId = d.EvaluationId, CycleId = d.CycleId, SoloParentId = d.SoloParentId,
            SpId = d.SpId, Name = d.Name, Barangay = d.Barangay, Priority = NormPrio(d.Priority), Score = d.Score,
            MonthlyIncome = d.MonthlyIncome, IncomePerCapita = CalcCapita(d.MonthlyIncome, d.Dependants, d.IncomePerCapita), Dependants = d.Dependants,
            MinorDependentsCount = d.MinorDependentsCount, ToddlersUnder5Count = d.ToddlersUnder5Count,
            Circumstance = d.Circumstance, Sex = d.Sex, CivilStatus = d.CivilStatus, ContactNumber = d.ContactNumber,
            HasCollegeAgeDependent = d.HasCollegeAgeDependent, CollegeAgeDependentsCount = d.CollegeAgeDependentsCount,
            IsPantawidBeneficiary = d.IsPantawidBeneficiary, IsFinalGrantee = d.Status == "FinalGrantee",
            IsWaitlisted = d.IsWaitlisted || d.Status == "Waitlisted",
            ModelVersion = d.ModelVersion, Children0To6Count = d.Children0To6, Children7To22Count = d.Children7To22,
            OtherIncomeSource = d.OtherIncomeSource, NeedsAndProblems = d.NeedsAndProblems,
            ChildrenDetails = d.Children?.Select(c => new ChildDetailItem { Name = c.Name, Age = c.Age }).ToList() ?? new List<ChildDetailItem>()
        };

        public static SelectionRow GranteeToRow(GranteeDto g, int rk) => new SelectionRow
        {
            Rank = rk, EvaluationId = g.EvaluationId, CycleId = g.CycleId, SoloParentId = g.SoloParentId,
            SpId = g.SoloParentId != Guid.Empty ? "SP-" + g.SoloParentId.ToString().Substring(0, 8).ToUpper() : $"SP-{g.FiscalYear}-{rk:D4}",
            Name = g.FullName, Barangay = g.Barangay, ContactNumber = g.ContactNumber, Priority = NormPrio(g.PredictedPriority), Score = (double)g.PriorityScore,
            MonthlyIncome = (double)g.MonthlyIncome, IncomePerCapita = CalcCapita((double)g.MonthlyIncome, g.ChildrenTotal, 0),
            Dependants = g.ChildrenTotal, MinorDependentsCount = g.ChildrenTotal,
            IsFinalGrantee = true, IsWaitlisted = false
        };

        private static readonly HashSet<string> CircumstanceCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Birth from rape", "Death of Spouse", "Detention of Spouse", "Physical & Mental Incapacity of Spouse",
            "Legal/de facto Separation", "Declaration of Nullity/Annulment", "Abandonment of Spouse",
            "OFW-related Solo Parent", "Unmarried Mother/Father", "Legal Guardian / Adoptive Parent",
            "Relative within 4th civil degree", "Pregnant Woman", "Solo Parent", "Adolescent Parent",
            "Abandonment (A2)", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "B", "C", "D", "E", "F"
        };

        private static readonly Dictionary<string, string> CodeToCircumstance = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "A1", "Birth from rape" }, { "A2", "Death of Spouse" }, { "A3", "Detention of Spouse" },
            { "A4", "Physical & Mental Incapacity of Spouse" }, { "A5", "Legal/de facto Separation" },
            { "A6", "Declaration of Nullity/Annulment" }, { "A7", "Abandonment of Spouse" },
            { "B",  "OFW-related Solo Parent" }, { "C", "Unmarried Mother/Father" },
            { "D",  "Legal Guardian / Adoptive Parent" }, { "E", "Relative within 4th degree / Adolescent Parent" },
            { "F",  "Pregnant Woman" }
        };

        private static string ResolveCircumstance(SOLUM_UI.Models.Api.ProblemPresentedDetails prob)
        {
            if (prob == null) return string.Empty;
            string name = prob.Name?.Trim() ?? string.Empty;
            string num = prob.ProblemPresentedNumber?.Trim() ?? string.Empty;
            if (CircumstanceCategories.Contains(name))
                return !string.IsNullOrEmpty(num) ? $"{name} ({num})" : name;
            if (!string.IsNullOrEmpty(num) && CodeToCircumstance.TryGetValue(num, out var mapped))
                return $"{mapped} ({num})";
            return !string.IsNullOrEmpty(name) ? name : num;
        }

        private static string ResolveNeedsAndProblems(SOLUM_UI.Models.Api.ProblemPresentedDetails prob)
        {
            if (prob == null) return string.Empty;
            string name = prob.Name?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(name) && !CircumstanceCategories.Contains(name))
                return name;
            if (!string.IsNullOrEmpty(prob.TypeOfDisability))
                return $"Disability: {prob.TypeOfDisability.Trim()}";
            if (!string.IsNullOrEmpty(prob.CauseOfDeath))
                return $"Deceased spouse: {prob.CauseOfDeath.Trim()}";
            if (!string.IsNullOrEmpty(prob.PeriodOfSeparation))
                return $"Separation: {prob.PeriodOfSeparation.Trim()}";
            if (prob.LengthOfAbroad.HasValue && prob.LengthOfAbroad.Value > 0)
                return $"Abroad: {prob.LengthOfAbroad.Value} yrs";
            return string.Empty;
        }

        public static void EnrichItemFromRecord(SubsidyItem item, SOLUM_UI.Models.Api.SoloParentDto dto)
        {
            if (item == null || dto == null) return;
            string circ = ResolveCircumstance(dto.ProblemPresented);
            if (!string.IsNullOrEmpty(circ)) item.Circumstance = circ;
            string needs = ResolveNeedsAndProblems(dto.ProblemPresented);
            if (!string.IsNullOrEmpty(needs)) item.NeedsAndProblems = needs;

            if (dto.FamilyMembers != null && dto.FamilyMembers.Count > 0)
            {
                var children = dto.FamilyMembers
                    .Where(f => !f.IsDeleted && (string.IsNullOrEmpty(f.Relationship) || f.Relationship.IndexOf("child", StringComparison.OrdinalIgnoreCase) >= 0 || f.Age <= 22))
                    .Select(f => new ChildDetailItem { Name = f.Name, Age = f.Age }).ToList();
                if (children.Count > 0)
                {
                    item.ChildrenDetails = children;
                    item.Children0To6Count = children.Count(c => c.Age >= 0 && c.Age <= 6);
                    item.Children7To22Count = children.Count(c => c.Age >= 7 && c.Age <= 22);
                    item.Dependants = children.Count;
                    item.MinorDependentsCount = item.Children0To6Count + item.Children7To22Count;
                }
            }
            if (!string.IsNullOrWhiteSpace(dto.Employment?.EmploymentStatus) && string.IsNullOrWhiteSpace(item.EmploymentStatus))
                item.EmploymentStatus = dto.Employment.EmploymentStatus;
            if (!string.IsNullOrWhiteSpace(dto.PersonalInfo?.CivilStatus) && string.IsNullOrWhiteSpace(item.CivilStatus))
                item.CivilStatus = dto.PersonalInfo.CivilStatus;
        }

        public static void EnrichRowFromRecord(SelectionRow row, SOLUM_UI.Models.Api.SoloParentDto dto)
        {
            if (row == null || dto == null) return;
            string circ = ResolveCircumstance(dto.ProblemPresented);
            if (!string.IsNullOrEmpty(circ)) row.Circumstance = circ;
            string needs = ResolveNeedsAndProblems(dto.ProblemPresented);
            if (!string.IsNullOrEmpty(needs)) row.NeedsAndProblems = needs;

            if (string.IsNullOrEmpty(row.ContactNumber)) row.ContactNumber = dto.ContactDetails?.ApplicantContactNumber;
            if (string.IsNullOrEmpty(row.Address)) row.Address = dto.AddressDetails?.Address;
            row.EmergencyContactName = dto.EmergencyContact?.EmergencyPersonName;
            row.EmergencyContactNumber = dto.EmergencyContact?.EmergencyPersonContactNumber;

            if (dto.FamilyMembers != null && dto.FamilyMembers.Count > 0)
            {
                var children = dto.FamilyMembers
                    .Where(f => !f.IsDeleted && (string.IsNullOrEmpty(f.Relationship) || f.Relationship.IndexOf("child", StringComparison.OrdinalIgnoreCase) >= 0 || f.Age <= 22))
                    .Select(f => new ChildDetailItem { Name = f.Name, Age = f.Age }).ToList();
                if (children.Count > 0)
                {
                    row.ChildrenDetails = children;
                    row.Children0To6Count = children.Count(c => c.Age >= 0 && c.Age <= 6);
                    row.Children7To22Count = children.Count(c => c.Age >= 7 && c.Age <= 22);
                    row.Dependants = children.Count;
                    row.MinorDependentsCount = row.Children0To6Count + row.Children7To22Count;
                }
            }
        }
    }
}
