using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SOLUM_UI.DTOs;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;

namespace SOLUM_UI.Services.Interfaces
{
    public interface ISubsidyApiService
    {
        Task<BaseResponse<SubsidyCycleDto>> GetActiveCycleAsync();
        Task<BaseResponse<List<SubsidyCycleDto>>> GetCyclesAsync();
        Task<BaseResponse<SubsidyCycleDto>> CreateCycleAsync(CreateSubsidyCycleRequest request);
        Task<BaseResponse<int>> GenerateRecommendationsAsync(Guid? cycleId = null);
        Task<BaseResponse<List<SubsidyRecommendationDto>>> GetRecommendationsAsync(Guid? cycleId = null);
        Task<BaseResponse<List<SubsidyRecommendationDto>>> GetShortlistAsync(Guid? cycleId = null);
        Task<BaseResponse<bool>> ShortlistCandidatesAsync(Guid? cycleId, List<Guid> evaluationIds);
        Task<BaseResponse<SubsidyRecommendationDto>> RejectOutlierAsync(RejectOutlierRequest request);
        Task<BaseResponse<int>> ConfirmGranteesAsync(Guid? cycleId, List<Guid> evaluationIds);
        Task<BaseResponse<List<GranteeDto>>> GetGranteesByFiscalYearAsync(int fiscalYear);
        Task<BaseResponse<bool>> RevokeGrantAsync(Guid evaluationId, string reason = null, string notes = null);

        // CSV Roster Exporters
        Task<string> ExportCityEducRosterCsvAsync(List<SelectionRow> items);
        Task<string> ExportPantawid4PsRosterCsvAsync(List<SelectionRow> items);
        Task<string> ExportFinalLedgerCsvAsync(List<SelectionRow> items, int fiscalYear);
        Task<string> ExportAllQueueCsvAsync(List<SubsidyItem> items);
    }
}
