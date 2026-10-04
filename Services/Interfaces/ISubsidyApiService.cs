using System.Collections.Generic;
using System.Threading.Tasks;
using SOLUM_UI.DTOs;
using SOLUM_UI.Models;

namespace SOLUM_UI.Services.Interfaces
{
    public interface ISubsidyApiService
    {
        Task<List<SubsidyRecommendationDto>> GetRecommendationsAsync();
        Task<bool> SubmitShortlistAsync(List<string> spIds);
        Task<bool> RejectOutlierAsync(OutlierRejectionDto rejection);
        Task<List<SelectionRow>> GetShortlistedCandidatesAsync();
        Task<List<SelectionRow>> GetFinalGranteesByYearAsync(int fiscalYear);
        Task<bool> ConfirmFinalGranteesAsync(List<string> spIds, int fiscalYear);
        Task<string> ExportCityEducRosterCsvAsync(List<SelectionRow> items);
        Task<string> ExportFinalLedgerCsvAsync(List<SelectionRow> items, int fiscalYear);
        Task<string> ExportAllQueueCsvAsync(List<SubsidyItem> items);
    }
}
