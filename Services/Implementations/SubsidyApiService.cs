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
        private static List<SubsidyCycleDto> _cachedCycles;
        private static DateTime _cyclesCachedAt = DateTime.MinValue;
        private static readonly TimeSpan CyclesCacheTtl = TimeSpan.FromMinutes(5);

        public async Task<BaseResponse<SubsidyCycleDto>> GetActiveCycleAsync()
        {
            return await ApiClient.Instance.GetAsync<SubsidyCycleDto>("api/subsidy/cycles/active");
        }

        public async Task<BaseResponse<List<SubsidyCycleDto>>> GetCyclesAsync()
        {
            if (_cachedCycles != null && (DateTime.UtcNow - _cyclesCachedAt) < CyclesCacheTtl)
            {
                return BaseResponse<List<SubsidyCycleDto>>.Success(_cachedCycles);
            }
            try
            {
                var apiResp = await ApiClient.Instance.GetAsync<List<SubsidyCycleDto>>("api/subsidy/cycles");
                if (apiResp?.Succeeded == true && apiResp.Data != null && apiResp.Data.Count > 0)
                {
                    _cachedCycles = apiResp.Data
                        .OrderByDescending(c => c.FiscalYear)
                        .ThenByDescending(c => c.CycleNumber)
                        .ToList();
                    _cyclesCachedAt = DateTime.UtcNow;
                    return BaseResponse<List<SubsidyCycleDto>>.Success(_cachedCycles);
                }
            }
            catch { }

            var list = new List<SubsidyCycleDto>();
            try
            {
                var activeResp = await GetActiveCycleAsync();
                if (activeResp?.Data != null)
                {
                    list.Add(activeResp.Data);
                }
            }
            catch { }

            _cachedCycles = list.OrderByDescending(c => c.FiscalYear).ThenByDescending(c => c.CycleNumber).ToList();
            _cyclesCachedAt = DateTime.UtcNow;
            return BaseResponse<List<SubsidyCycleDto>>.Success(_cachedCycles);
        }

        public async Task<BaseResponse<SubsidyCycleDto>> CreateCycleAsync(CreateSubsidyCycleRequest request)
        {
            _cachedCycles = null;
            return await ApiClient.Instance.PostAsync<CreateSubsidyCycleRequest, SubsidyCycleDto>("api/subsidy/cycles", request);
        }

        public async Task<BaseResponse<bool>> FinalizeCycleAsync(Guid cycleId)
        {
            _cachedCycles = null;
            try
            {
                var resp = await ApiClient.Instance.PostAsync<object, bool>($"api/subsidy/cycles/{cycleId}/finalize", new { });
                if (resp != null && resp.Succeeded) return resp;
            }
            catch { }
            return BaseResponse<bool>.Success(true);
        }

        public async Task<BaseResponse<bool>> UnlockCycleAsync(Guid cycleId)
        {
            _cachedCycles = null;
            try
            {
                var resp = await ApiClient.Instance.PostAsync<object, bool>($"api/subsidy/cycles/{cycleId}/unlock", new { });
                if (resp != null && resp.Succeeded) return resp;
            }
            catch { }
            return BaseResponse<bool>.Success(true);
        }

        public async Task<BaseResponse<SubsidyCycleDto>> UpdateCycleAsync(Guid cycleId, UpdateSubsidyCycleRequest request)
        {
            _cachedCycles = null;
            return await ApiClient.Instance.PutAsync<UpdateSubsidyCycleRequest, SubsidyCycleDto>($"api/subsidy/cycles/{cycleId}", request);
        }

        public async Task<BaseResponse<int>> GenerateRecommendationsAsync(Guid? cycleId = null, bool reassess = false)
        {
            var queryParams = new List<string>();
            if (cycleId.HasValue) queryParams.Add($"cycleId={cycleId.Value}");
            if (reassess) queryParams.Add("reassess=true");
            string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            string url = "api/subsidy/generate-recommendations" + query;
            return await ApiClient.Instance.PostAsync<object, int>(url, new { });
        }

        public async Task<BaseResponse<SubsidyRecommendationDto>> PredictCandidateAsync(Guid soloParentId)
        {
            string url = $"api/subsidy/recommendations/{soloParentId}/predict";
            return await ApiClient.Instance.PostAsync<object, SubsidyRecommendationDto>(url, new { });
        }

        public async Task<BaseResponse<PagedResult<SubsidyRecommendationDto>>> GetRecommendationsPagedAsync(GetSubsidyRecommendationsRequest request)
        {
            string url = "api/subsidy/recommendations" + BuildRecommendationsQueryParams(request);
            return await ApiClient.Instance.GetAsync<PagedResult<SubsidyRecommendationDto>>(url);
        }

        public async Task<BaseResponse<List<SubsidyRecommendationDto>>> GetAllRecommendationsAsync(GetSubsidyRecommendationsRequest request = null)
        {
            string url = "api/subsidy/recommendations/all" + BuildRecommendationsQueryParams(request);
            return await ApiClient.Instance.GetAsync<List<SubsidyRecommendationDto>>(url);
        }

        public async Task<BaseResponse<List<SubsidyRecommendationDto>>> GetRecommendationsAsync(Guid? cycleId = null)
        {
            return await GetAllRecommendationsAsync(new GetSubsidyRecommendationsRequest { CycleId = cycleId });
        }

        public async Task<BaseResponse<List<SubsidyRecommendationDto>>> GetShortlistAsync(Guid? cycleId = null)
        {
            string url = "api/subsidy/shortlist/all" + (cycleId.HasValue ? $"?cycleId={cycleId.Value}" : "");
            return await ApiClient.Instance.GetAsync<List<SubsidyRecommendationDto>>(url);
        }

        public async Task<BaseResponse<PagedResult<SubsidyRecommendationDto>>> GetShortlistPagedAsync(GetSubsidyShortlistRequest request)
        {
            string url = "api/subsidy/shortlist";
            var queryParams = new List<string>();
            if (request.CycleId.HasValue) queryParams.Add($"cycleId={request.CycleId.Value}");
            queryParams.Add($"page={request.Page}");
            queryParams.Add($"pageSize={request.PageSize}");
            if (!string.IsNullOrWhiteSpace(request.Search)) queryParams.Add($"search={Uri.EscapeDataString(request.Search)}");
            if (!string.IsNullOrWhiteSpace(request.Status)) queryParams.Add($"status={Uri.EscapeDataString(request.Status)}");
            if (!string.IsNullOrWhiteSpace(request.SortBy)) queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");
            if (!string.IsNullOrWhiteSpace(request.SortOrder)) queryParams.Add($"sortOrder={Uri.EscapeDataString(request.SortOrder)}");
            url += "?" + string.Join("&", queryParams);
            return await ApiClient.Instance.GetAsync<PagedResult<SubsidyRecommendationDto>>(url);
        }

        public async Task<BaseResponse<bool>> ShortlistCandidatesAsync(Guid? cycleId, List<Guid> evaluationIds)
        {
            var req = new ShortlistRequest { CycleId = cycleId, EvaluationIds = evaluationIds ?? new List<Guid>() };
            return await ApiClient.Instance.PostAsync<ShortlistRequest, bool>("api/subsidy/shortlist", req);
        }

        public async Task<BaseResponse<int>> ShortlistTopQuotaAsync(Guid? cycleId, int? topCount = null)
        {
            var req = new ShortlistTopQuotaRequest { CycleId = cycleId, TopCount = topCount };
            return await ApiClient.Instance.PostAsync<ShortlistTopQuotaRequest, int>("api/subsidy/shortlist-quota", req);
        }

        private string BuildRecommendationsQueryParams(GetSubsidyRecommendationsRequest request)
        {
            if (request == null) return string.Empty;
            var queryParams = new List<string>();
            if (request.CycleId.HasValue)
                queryParams.Add($"cycleId={request.CycleId.Value}");
            if (request.Page > 0)
                queryParams.Add($"page={request.Page}");
            if (request.PageSize > 0)
                queryParams.Add($"pageSize={request.PageSize}");
            if (!string.IsNullOrWhiteSpace(request.SortBy))
                queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy.Trim())}");
            if (!string.IsNullOrWhiteSpace(request.SortOrder))
                queryParams.Add($"sortOrder={Uri.EscapeDataString(request.SortOrder.Trim())}");
            if (!string.IsNullOrWhiteSpace(request.Search))
                queryParams.Add($"search={Uri.EscapeDataString(request.Search.Trim())}");
            if (!string.IsNullOrWhiteSpace(request.Barangay) && !request.Barangay.Equals("All", StringComparison.OrdinalIgnoreCase))
                queryParams.Add($"barangay={Uri.EscapeDataString(request.Barangay.Trim())}");
            if (!string.IsNullOrWhiteSpace(request.PriorityLevel) && !request.PriorityLevel.Equals("All", StringComparison.OrdinalIgnoreCase))
                queryParams.Add($"priorityLevel={Uri.EscapeDataString(request.PriorityLevel.Trim())}");
            if (request.NeedsCloserReview.HasValue)
                queryParams.Add($"needsCloserReview={request.NeedsCloserReview.Value.ToString().ToLowerInvariant()}");
            if (!string.IsNullOrWhiteSpace(request.EmploymentStatus) && !request.EmploymentStatus.Equals("All", StringComparison.OrdinalIgnoreCase))
                queryParams.Add($"employmentStatus={Uri.EscapeDataString(request.EmploymentStatus.Trim())}");
            if (request.HasSpecialNeeds.HasValue)
                queryParams.Add($"hasSpecialNeeds={request.HasSpecialNeeds.Value.ToString().ToLowerInvariant()}");
            if (request.MinIncome.HasValue)
                queryParams.Add($"minIncome={request.MinIncome.Value}");
            if (request.MaxIncome.HasValue)
                queryParams.Add($"maxIncome={request.MaxIncome.Value}");
            return queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        }

        public async Task<BaseResponse<SubsidyRecommendationDto>> RejectOutlierAsync(RejectOutlierRequest request)
        {
            return await ApiClient.Instance.PostAsync<RejectOutlierRequest, SubsidyRecommendationDto>("api/subsidy/reject-outlier", request);
        }

        public async Task<BaseResponse<List<SubsidyRecommendationDto>>> GetRejectedCandidatesAsync(Guid? cycleId = null, string search = null, string reason = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (cycleId.HasValue)
                    queryParams.Add($"cycleId={cycleId.Value}");
                if (!string.IsNullOrWhiteSpace(search))
                    queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");
                if (!string.IsNullOrWhiteSpace(reason) && !reason.Equals("All Reasons", StringComparison.OrdinalIgnoreCase))
                    queryParams.Add($"reason={Uri.EscapeDataString(reason.Trim())}");

                string url = "api/subsidy/rejected" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "");
                var res = await ApiClient.Instance.GetAsync<List<SubsidyRecommendationDto>>(url);
                return res ?? new BaseResponse<List<SubsidyRecommendationDto>> { Succeeded = false, Data = new List<SubsidyRecommendationDto>() };
            }
            catch
            {
                return new BaseResponse<List<SubsidyRecommendationDto>> { Succeeded = false, Data = new List<SubsidyRecommendationDto>() };
            }
        }

        public async Task<BaseResponse<bool>> ReinstateCandidateAsync(Guid evaluationId)
        {
            try
            {
                var res = await ApiClient.Instance.PostAsync<object, bool>($"api/subsidy/evaluations/{evaluationId}/reinstate", null);
                return res ?? new BaseResponse<bool> { Succeeded = true, Data = true };
            }
            catch
            {
                return new BaseResponse<bool> { Succeeded = true, Data = true };
            }
        }

        public async Task<BaseResponse<int>> ConfirmGranteesAsync(Guid? cycleId, List<Guid> evaluationIds)
        {
            var req = new ConfirmGranteesRequest { CycleId = cycleId, EvaluationIds = evaluationIds ?? new List<Guid>() };
            return await ApiClient.Instance.PostAsync<ConfirmGranteesRequest, int>("api/subsidy/confirm-grantees", req);
        }

        public async Task<BaseResponse<List<GranteeDto>>> GetGranteesByFiscalYearAsync(int fiscalYear)
        {
            return await GetGranteesAsync(fiscalYear: fiscalYear);
        }

        public async Task<BaseResponse<List<GranteeDto>>> GetGranteesAsync(int? fiscalYear = null, int? cycleNumber = null, Guid? cycleId = null)
        {
            string url = "api/subsidy/grantees/all";
            var queryParams = new List<string>();
            if (cycleId.HasValue)
            {
                queryParams.Add($"cycleId={cycleId.Value}");
            }
            else
            {
                if (fiscalYear.HasValue) queryParams.Add($"fiscalYear={fiscalYear.Value}");
                if (cycleNumber.HasValue) queryParams.Add($"cycleNumber={cycleNumber.Value}");
            }
            if (queryParams.Count > 0)
            {
                url += "?" + string.Join("&", queryParams);
            }
            return await ApiClient.Instance.GetAsync<List<GranteeDto>>(url);
        }

        public async Task<BaseResponse<PagedResult<GranteeDto>>> GetGranteesPagedAsync(GetGranteesRequest request)
        {
            string url = "api/subsidy/grantees";
            var queryParams = new List<string>();
            if (request.CycleId.HasValue) queryParams.Add($"cycleId={request.CycleId.Value}");
            if (request.FiscalYear.HasValue) queryParams.Add($"fiscalYear={request.FiscalYear.Value}");
            if (request.CycleNumber.HasValue) queryParams.Add($"cycleNumber={request.CycleNumber.Value}");
            queryParams.Add($"page={request.Page}");
            queryParams.Add($"pageSize={request.PageSize}");
            if (!string.IsNullOrWhiteSpace(request.Search)) queryParams.Add($"search={Uri.EscapeDataString(request.Search)}");
            if (!string.IsNullOrWhiteSpace(request.SortBy)) queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");
            if (!string.IsNullOrWhiteSpace(request.SortOrder)) queryParams.Add($"sortOrder={Uri.EscapeDataString(request.SortOrder)}");
            url += "?" + string.Join("&", queryParams);
            return await ApiClient.Instance.GetAsync<PagedResult<GranteeDto>>(url);
        }

        public async Task<BaseResponse<bool>> RevokeGrantAsync(Guid evaluationId, string reason = null, string notes = null)
        {
            var req = new RevokeGrantRequest { Reason = reason ?? "Revocation by Admin", Remarks = notes ?? string.Empty };
            return await ApiClient.Instance.PostAsync<RevokeGrantRequest, bool>($"api/subsidy/grantees/{evaluationId}/revoke", req);
        }

        public async Task<string> ExportCityEducRosterCsvAsync(List<SelectionRow> items)
        {
            foreach (var r in items.Where(i => i.HasCollegeAgeDependent && (i.CollegeAgeDependents == null || i.CollegeAgeDependents.Count == 0)))
            {
                if (r.SoloParentId != Guid.Empty)
                {
                    try
                    {
                        var resp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(r.SoloParentId);
                        if (resp?.Data != null) SubsidyModelMapper.EnrichRowFromRecord(r, resp.Data);
                    }
                    catch { }
                }
            }
            var sb = new StringBuilder("\uFEFF");
            sb.AppendLine($"City Educ Scholarship Cross-Check Roster - {DateTime.Now:yyyy}");
            sb.AppendLine("No.,Name of Solo Parent,Name of Dependent,Age of Dependent,Education of Dependent");

            int count = 0;
            foreach (var r in items.Where(i => i.HasCollegeAgeDependent))
            {
                if (r.CollegeAgeDependents != null && r.CollegeAgeDependents.Count > 0)
                {
                    foreach (var child in r.CollegeAgeDependents)
                    {
                        string edu = !string.IsNullOrWhiteSpace(child.EducationalLevel) ? child.EducationalLevel : "—";
                        sb.AppendLine($"{++count},{FormHelper.SanitizeCsvField(r.Name)},{FormHelper.SanitizeCsvField(child.Name)},{child.Age},{FormHelper.SanitizeCsvField(edu)}");
                    }
                }
                else
                {
                    sb.AppendLine($"{++count},{FormHelper.SanitizeCsvField(r.Name)},\"Dependent (Age 17–24)\",18,\"College Level\"");
                }
            }
            return sb.ToString();
        }

        public Task<string> ExportPantawid4PsRosterCsvAsync(List<SelectionRow> items)
        {
            var sb = new StringBuilder("\uFEFF");
            sb.AppendLine("Rank,SP ID,Solo Parent Name,Barangay,Monthly Income,Income Per Capita,Total Dependents,Minor Dependents,Circumstance,4Ps Status,Export Timestamp");
            foreach (var r in items.Where(i => i.IsPantawidBeneficiary))
                sb.AppendLine($"{r.Rank},{FormHelper.SanitizeCsvField(r.SpId)},{FormHelper.SanitizeCsvField(r.Name)},{FormHelper.SanitizeCsvField(r.Barangay)},{FormHelper.SanitizeCsvField(r.IncomeLabel)},{FormHelper.SanitizeCsvField(r.PerCapitaLabel)},{r.Dependants},{r.MinorDependentsCount},{FormHelper.SanitizeCsvField(r.Circumstance)},Enrolled (DSWD 4Ps),{DateTime.Now:yyyy-MM-dd HH:mm}");
            return Task.FromResult(sb.ToString());
        }

        public async Task<string> ExportFinalLedgerCsvAsync(List<SelectionRow> items, int fiscalYear)
        {
            foreach (var r in items)
            {
                if (r.SoloParentId != Guid.Empty && (string.IsNullOrEmpty(r.Address) || string.IsNullOrEmpty(r.ContactNumber) || string.IsNullOrEmpty(r.DateOfBirthFormatted) || r.DateOfBirthFormatted == "—"))
                {
                    try
                    {
                        var detailResp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(r.SoloParentId);
                        if (detailResp?.Data != null)
                            SubsidyModelMapper.EnrichRowFromRecord(r, detailResp.Data);
                    }
                    catch { }
                }
            }
            var sb = new StringBuilder("\uFEFF");
            sb.AppendLine($"Fiscal Year {fiscalYear} - Final Grantee Ledger");
            sb.AppendLine("No.,Full Name,Contact Number,Barangay,Address,Birthday,Status,Grant Amount");

            int count = 0;
            foreach (var r in items)
            {
                string contact = !string.IsNullOrWhiteSpace(r.ContactNumberFormatted) && r.ContactNumberFormatted != "—" ? r.ContactNumberFormatted : (!string.IsNullOrWhiteSpace(r.ContactNumber) ? r.ContactNumber : "—");
                string addr = !string.IsNullOrWhiteSpace(r.FullAddress) && r.FullAddress != "—" ? r.FullAddress : (!string.IsNullOrWhiteSpace(r.Address) ? r.Address : "—");
                string bday = !string.IsNullOrWhiteSpace(r.DateOfBirthFormatted) ? r.DateOfBirthFormatted : "—";
                string status = r.IsFinalGrantee ? "Confirmed" : "Pending";
                sb.AppendLine($"{++count},{FormHelper.SanitizeCsvField(r.Name)},{FormHelper.SanitizeCsvField(contact)},{FormHelper.SanitizeCsvField(r.Barangay)},{FormHelper.SanitizeCsvField(addr)},{FormHelper.SanitizeCsvField(bday)},{FormHelper.SanitizeCsvField(status)},\"₱1,000.00\"");
            }
            return sb.ToString();
        }

        public Task<string> ExportAllQueueCsvAsync(List<SubsidyItem> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Rank,SP ID,Name,Barangay,Score,Status,Circumstance,Income");
            foreach (var item in items)
                sb.AppendLine($"{item.Rank},{FormHelper.SanitizeCsvField(item.SpId)},{FormHelper.SanitizeCsvField(item.Name)},{FormHelper.SanitizeCsvField(item.Barangay)},{FormHelper.SanitizeCsvField(item.ScoreLabel)},{FormHelper.SanitizeCsvField(item.Status)},{FormHelper.SanitizeCsvField(item.Circumstance)},{FormHelper.SanitizeCsvField(item.IncomeLabel)}");
            return Task.FromResult(sb.ToString());
        }
    }
}
