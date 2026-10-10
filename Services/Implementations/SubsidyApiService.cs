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

        public async Task<BaseResponse<List<SubsidyCycleDto>>> GetCyclesAsync()
        {
            var resp = await ApiClient.Instance.GetAsync<List<SubsidyCycleDto>>("api/subsidy/cycles");
            if (resp != null && resp.Succeeded && resp.Data != null && resp.Data.Count > 0)
            {
                return resp;
            }

            var list = new List<SubsidyCycleDto>();
            var activeResp = await GetActiveCycleAsync();
            int activeYear = DateTime.Now.Year;
            if (activeResp?.Data != null)
            {
                list.Add(activeResp.Data);
                activeYear = activeResp.Data.FiscalYear;
            }

            for (int y = activeYear - 1; y >= activeYear - 2; y--)
            {
                if (!list.Any(c => c.FiscalYear == y))
                {
                    var gr = await GetGranteesByFiscalYearAsync(y);
                    if (gr?.Data != null && gr.Data.Count > 0)
                    {
                        list.Add(new SubsidyCycleDto
                        {
                            FiscalYear = y,
                            Status = "Finalized",
                            TotalFinalGrantees = gr.Data.Count,
                            AllocatedSlots = Math.Max(100, gr.Data.Count),
                            TotalBudget = Math.Max(100000m, gr.Data.Count * 1000m)
                        });
                    }
                }
            }

            return BaseResponse<List<SubsidyCycleDto>>.Success(list.OrderByDescending(c => c.FiscalYear).ToList());
        }

        public async Task<BaseResponse<SubsidyCycleDto>> CreateCycleAsync(CreateSubsidyCycleRequest request)
        {
            return await ApiClient.Instance.PostAsync<CreateSubsidyCycleRequest, SubsidyCycleDto>("api/subsidy/cycles", request);
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

        public async Task<string> ExportFinalLedgerCsvAsync(List<SelectionRow> items, int fiscalYear)
        {
            foreach (var r in items)
            {
                if (r.SoloParentId != Guid.Empty && string.IsNullOrEmpty(r.EmergencyContactNumber))
                {
                    try
                    {
                        var detailResp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(r.SoloParentId);
                        if (detailResp?.Data != null)
                        {
                            if (string.IsNullOrEmpty(r.ContactNumber)) r.ContactNumber = detailResp.Data.ContactDetails?.ApplicantContactNumber;
                            if (string.IsNullOrEmpty(r.Address)) r.Address = detailResp.Data.AddressDetails?.Address;
                            r.EmergencyContactName = detailResp.Data.EmergencyContact?.EmergencyPersonName;
                            r.EmergencyContactNumber = detailResp.Data.EmergencyContact?.EmergencyPersonContactNumber;
                        }
                    }
                    catch { }
                }
            }
            var sb = new StringBuilder();
            sb.AppendLine($"Fiscal Year {fiscalYear} - Final Grantee Contact & Confirmation Ledger");
            sb.AppendLine("Rank,SP ID,Full Name,Contact Number,Barangay,Residential Address,Emergency Contact Person,Emergency Contact Number,Status,Grant Amount");
            foreach (var r in items)
            {
                string contact = !string.IsNullOrWhiteSpace(r.ContactNumber) ? r.ContactNumber : "—";
                string addr = !string.IsNullOrWhiteSpace(r.Address) ? r.Address : "—";
                string emName = !string.IsNullOrWhiteSpace(r.EmergencyContactName) ? r.EmergencyContactName : "—";
                string emPhone = !string.IsNullOrWhiteSpace(r.EmergencyContactNumber) ? r.EmergencyContactNumber : "—";
                string status = r.IsFinalGrantee ? "Confirmed Grantee" : "Pending Confirmation";
                sb.AppendLine($"{r.Rank},{r.SpId},\"{r.Name}\",\"{contact}\",\"{r.Barangay}\",\"{addr}\",\"{emName}\",\"{emPhone}\",\"{status}\",₱1000.00");
            }
            return sb.ToString();
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
