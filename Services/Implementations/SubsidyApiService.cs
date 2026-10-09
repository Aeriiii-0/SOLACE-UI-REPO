using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SOLUM_UI.DTOs;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Services.Interfaces;

namespace SOLUM_UI.Services.Implementations
{
    public class SubsidyApiService : ISubsidyApiService
    {
        public async Task<BaseResponse<SubsidyCycleDto>> GetActiveCycleAsync()
        {
            return await ApiClient.Instance.GetAsync<SubsidyCycleDto>("api/subsidy/cycles/active");
        }

        public async Task<BaseResponse<int>> GenerateRecommendationsAsync(Guid? cycleId = null)
        {
            string url = "api/subsidy/generate-recommendations" + (cycleId.HasValue ? $"?cycleId={cycleId.Value}" : "");
            return await ApiClient.Instance.PostAsync<object, int>(url, new { });
        }

        public async Task<BaseResponse<List<SubsidyRecommendationDto>>> GetRecommendationsAsync(Guid? cycleId = null)
        {
            string url = "api/subsidy/recommendations" + (cycleId.HasValue ? $"?cycleId={cycleId.Value}" : "");
            return await ApiClient.Instance.GetAsync<List<SubsidyRecommendationDto>>(url);
        }

        public async Task<BaseResponse<List<SubsidyRecommendationDto>>> GetShortlistAsync(Guid? cycleId = null)
        {
            string url = "api/subsidy/shortlist" + (cycleId.HasValue ? $"?cycleId={cycleId.Value}" : "");
            return await ApiClient.Instance.GetAsync<List<SubsidyRecommendationDto>>(url);
        }

        public async Task<BaseResponse<bool>> ShortlistCandidatesAsync(Guid? cycleId, List<Guid> evaluationIds)
        {
            var req = new ShortlistRequest { CycleId = cycleId, EvaluationIds = evaluationIds ?? new List<Guid>() };
            return await ApiClient.Instance.PostAsync<ShortlistRequest, bool>("api/subsidy/shortlist", req);
        }

        public async Task<BaseResponse<SubsidyRecommendationDto>> RejectOutlierAsync(RejectOutlierRequest request)
        {
            return await ApiClient.Instance.PostAsync<RejectOutlierRequest, SubsidyRecommendationDto>("api/subsidy/reject-outlier", request);
        }

        public async Task<BaseResponse<int>> ConfirmGranteesAsync(Guid? cycleId, List<Guid> evaluationIds)
        {
            var req = new ConfirmGranteesRequest { CycleId = cycleId, EvaluationIds = evaluationIds ?? new List<Guid>() };
            return await ApiClient.Instance.PostAsync<ConfirmGranteesRequest, int>("api/subsidy/confirm-grantees", req);
        }

        public async Task<BaseResponse<List<GranteeDto>>> GetGranteesByFiscalYearAsync(int fiscalYear)
        {
            return await ApiClient.Instance.GetAsync<List<GranteeDto>>($"api/subsidy/grantees?fiscalYear={fiscalYear}");
        }

        public async Task<BaseResponse<bool>> RevokeGrantAsync(Guid evaluationId, string reason = null, string notes = null)
        {
            var req = new RevokeGrantRequest { Reason = reason ?? "Revocation by Admin", Remarks = notes ?? string.Empty };
            return await ApiClient.Instance.PostAsync<RevokeGrantRequest, bool>($"api/subsidy/grantees/{evaluationId}/revoke", req);
        }

        public Task<string> ExportCityEducRosterCsvAsync(List<SelectionRow> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Item #,College Student Name,Age,Category,Parent SP ID,Solo Parent Name,Barangay,Household Income,Audit Status,Export Timestamp");
            int count = 0;
            foreach (var r in items.Where(i => i.HasCollegeAgeDependent))
            {
                if (r.CollegeAgeDependents != null && r.CollegeAgeDependents.Count > 0)
                {
                    foreach (var child in r.CollegeAgeDependents)
                        sb.AppendLine($"{++count},\"{child.Name}\",{child.Age},College-Age (17–24y),{r.SpId},\"{r.Name}\",{r.Barangay},\"{r.IncomeLabel}\",Pending City Educ Verification,{DateTime.Now:yyyy-MM-dd HH:mm}");
                }
                else
                {
                    sb.AppendLine($"{++count},\"College-Age Dependent\",{r.CollegeAgeDependentsCount},College-Age (17–24y),{r.SpId},\"{r.Name}\",{r.Barangay},\"{r.IncomeLabel}\",Pending City Educ Verification,{DateTime.Now:yyyy-MM-dd HH:mm}");
                }
            }
            return Task.FromResult(sb.ToString());
        }

        public Task<string> ExportPantawid4PsRosterCsvAsync(List<SelectionRow> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Rank,SP ID,Solo Parent Name,Barangay,Monthly Income,Income Per Capita,Total Dependents,Minor Dependents,Circumstance,4Ps Status,Export Timestamp");
            foreach (var r in items.Where(i => i.IsPantawidBeneficiary))
                sb.AppendLine($"{r.Rank},{r.SpId},\"{r.Name}\",{r.Barangay},\"{r.IncomeLabel}\",\"{r.PerCapitaLabel}\",{r.Dependants},{r.MinorDependentsCount},\"{r.Circumstance}\",Enrolled (DSWD 4Ps),{DateTime.Now:yyyy-MM-dd HH:mm}");
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
