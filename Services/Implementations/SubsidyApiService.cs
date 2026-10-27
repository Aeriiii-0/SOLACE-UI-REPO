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

            int currentYear = DateTime.Now.Year;
            var list = new List<SubsidyCycleDto>();

            // Concurrently fetch active cycle and past historical grantee counts
            var activeTask = GetActiveCycleAsync();
            var y1Task = GetGranteesByFiscalYearAsync(currentYear - 1);
            var y2Task = GetGranteesByFiscalYearAsync(currentYear - 2);

            await Task.WhenAll(activeTask, y1Task, y2Task);

            var activeResp = await activeTask;
            if (activeResp?.Data != null)
            {
                list.Add(activeResp.Data);
            }

            if (!list.Any(c => c.FiscalYear == currentYear))
            {
                list.Add(new SubsidyCycleDto
                {
                    FiscalYear = currentYear,
                    Status = "Active",
                    AllocatedSlots = 100,
                    TotalBudget = 100000m,
                    PerGranteeAmount = 1000m
                });
            }

            var gr1 = await y1Task;
            int count1 = gr1?.Data?.Count ?? 0;
            list.Add(new SubsidyCycleDto
            {
                FiscalYear = currentYear - 1,
                Status = "Finalized",
                TotalFinalGrantees = count1,
                AllocatedSlots = Math.Max(100, count1),
                TotalBudget = Math.Max(100000m, count1 * 1000m),
                PerGranteeAmount = 1000m
            });

            var gr2 = await y2Task;
            int count2 = gr2?.Data?.Count ?? 0;
            list.Add(new SubsidyCycleDto
            {
                FiscalYear = currentYear - 2,
                Status = "Finalized",
                TotalFinalGrantees = count2,
                AllocatedSlots = Math.Max(100, count2),
                TotalBudget = Math.Max(100000m, count2 * 1000m),
                PerGranteeAmount = 1000m
            });

            _cachedCycles = list.OrderByDescending(c => c.FiscalYear).ToList();
            _cyclesCachedAt = DateTime.UtcNow;

            return BaseResponse<List<SubsidyCycleDto>>.Success(_cachedCycles);
        }

        public async Task<BaseResponse<SubsidyCycleDto>> CreateCycleAsync(CreateSubsidyCycleRequest request)
        {
            _cachedCycles = null;
            return await ApiClient.Instance.PostAsync<CreateSubsidyCycleRequest, SubsidyCycleDto>("api/subsidy/cycles", request);
        }

        public async Task<BaseResponse<SubsidyCycleDto>> UpdateCycleAsync(Guid cycleId, UpdateSubsidyCycleRequest request)
        {
            _cachedCycles = null;
            return await ApiClient.Instance.PutAsync<UpdateSubsidyCycleRequest, SubsidyCycleDto>($"api/subsidy/cycles/{cycleId}", request);
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
                        sb.AppendLine($"{++count},\"{r.Name}\",\"{child.Name}\",{child.Age},\"{edu}\"");
                    }
                }
                else
                {
                    sb.AppendLine($"{++count},\"{r.Name}\",\"Dependent (Age 17–24)\",18,\"College Level\"");
                }
            }
            return sb.ToString();
        }

        public Task<string> ExportPantawid4PsRosterCsvAsync(List<SelectionRow> items)
        {
            var sb = new StringBuilder("\uFEFF");
            sb.AppendLine("Rank,SP ID,Solo Parent Name,Barangay,Monthly Income,Income Per Capita,Total Dependents,Minor Dependents,Circumstance,4Ps Status,Export Timestamp");
            foreach (var r in items.Where(i => i.IsPantawidBeneficiary))
                sb.AppendLine($"{r.Rank},{r.SpId},\"{r.Name}\",{r.Barangay},\"{r.IncomeLabel}\",\"{r.PerCapitaLabel}\",{r.Dependants},{r.MinorDependentsCount},\"{r.Circumstance}\",Enrolled (DSWD 4Ps),{DateTime.Now:yyyy-MM-dd HH:mm}");
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
                sb.AppendLine($"{++count},\"{r.Name}\",\"{contact}\",\"{r.Barangay}\",\"{addr}\",\"{bday}\",\"{status}\",\"₱1,000.00\"");
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
