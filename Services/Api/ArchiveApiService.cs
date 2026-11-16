using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SOLUM_UI.DTOs.Archive;
using SOLUM_UI.Models.Api;

namespace SOLUM_UI.Services.Api
{
    public class ArchiveApiService
    {
        private static readonly Lazy<ArchiveApiService> _instance =
            new Lazy<ArchiveApiService>(() => new ArchiveApiService());
        public static ArchiveApiService Instance => _instance.Value;

        private ArchiveApiService() { }

        /// <summary>
        /// Retrieves records in the 30-day deletion grace period eligible for reinstatement.
        /// </summary>
        public async Task<BaseResponse<List<RecycleBinItemDto>>> GetRecycleBinAsync(string barangay = null)
        {
            string url = "api/soloparent/recycle-bin";
            if (!string.IsNullOrWhiteSpace(barangay) && !barangay.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                url += $"?barangay={Uri.EscapeDataString(barangay.Trim())}";
            }
            return await ApiClient.Instance.GetAsync<List<RecycleBinItemDto>>(url);
        }

        /// <summary>
        /// Reinstates a soft-deleted record back to active within its 30-day grace period.
        /// </summary>
        public async Task<BaseResponse<bool>> ReinstateFromRecycleBinAsync(Guid id)
        {
            string url = $"api/soloparent/{id}/reinstate";
            return await ApiClient.Instance.PostAsync<object, bool>(url, new { });
        }

        /// <summary>
        /// Searches historical archived records with pagination and multi-parameter filters.
        /// </summary>
        public async Task<BaseResponse<PagedResult<ArchivedSoloParentSummaryDto>>> SearchArchivesAsync(ArchiveSearchRequest request)
        {
            var queryParams = new List<string>();

            if (request != null)
            {
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    queryParams.Add($"SearchTerm={Uri.EscapeDataString(request.SearchTerm.Trim())}");
                }
                if (!string.IsNullOrWhiteSpace(request.Barangay) && !request.Barangay.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    queryParams.Add($"Barangay={Uri.EscapeDataString(request.Barangay.Trim())}");
                }
                if (!string.IsNullOrWhiteSpace(request.ArchiveReason) && !request.ArchiveReason.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    queryParams.Add($"ArchiveReason={Uri.EscapeDataString(request.ArchiveReason.Trim())}");
                }
                if (request.YearArchived.HasValue && request.YearArchived.Value > 0)
                {
                    queryParams.Add($"YearArchived={request.YearArchived.Value}");
                }
                if (request.IncludeShredded.HasValue)
                {
                    queryParams.Add($"IncludeShredded={request.IncludeShredded.Value.ToString().ToLowerInvariant()}");
                }
                queryParams.Add($"PageNumber={Math.Max(1, request.PageNumber)}");
                queryParams.Add($"PageSize={Math.Max(1, request.PageSize)}");
            }

            string url = "api/archive/soloparents";
            if (queryParams.Count > 0)
            {
                url += "?" + string.Join("&", queryParams);
            }

            return await ApiClient.Instance.GetAsync<PagedResult<ArchivedSoloParentSummaryDto>>(url);
        }

        /// <summary>
        /// Retrieves the complete read-only decrypted historical dossier for an archived record.
        /// </summary>
        public async Task<BaseResponse<ArchivedSoloParentDetailDto>> GetArchivedDossierAsync(Guid id)
        {
            string url = $"api/archive/soloparents/{id}";
            return await ApiClient.Instance.GetAsync<ArchivedSoloParentDetailDto>(url);
        }

        /// <summary>
        /// Reactivates an archived solo parent (due to 5-year inactivity), issuing a fresh 1-year card term.
        /// </summary>
        public async Task<BaseResponse<bool>> ReactivateCitizenAsync(Guid id, ReactivateArchivedRecordRequest request)
        {
            string url = $"api/archive/soloparents/{id}/reactivate";
            return await ApiClient.Instance.PostAsync<ReactivateArchivedRecordRequest, bool>(url, request ?? new ReactivateArchivedRecordRequest());
        }

        /// <summary>
        /// Administrative override restoration for a record whose 30-day deletion grace period expired.
        /// </summary>
        public async Task<BaseResponse<bool>> AdminRestoreArchiveAsync(Guid id, AdminRestoreArchiveRequest request)
        {
            string url = $"api/archive/soloparents/{id}/restore";
            return await ApiClient.Instance.PostAsync<AdminRestoreArchiveRequest, bool>(url, request ?? new AdminRestoreArchiveRequest());
        }

        /// <summary>
        /// Triggers the background automated lifecycle retention and shredding pipeline.
        /// </summary>
        public async Task<BaseResponse<LifecyclePipelineResultDto>> RunLifecyclePipelineAsync()
        {
            string url = "api/archive/maintenance/run-lifecycle";
            return await ApiClient.Instance.PostAsync<object, LifecyclePipelineResultDto>(url, new { });
        }
    }
}
