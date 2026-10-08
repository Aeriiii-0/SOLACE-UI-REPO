using System;
using System.Threading.Tasks;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;

namespace SOLUM_UI.Services.Interfaces
{
    /// <summary>
    /// Contract for all solo-parent API operations. Implementations live in
    /// <see cref="SOLUM_UI.Services.Api.SoloParentApiService"/>.
    /// </summary>
    public interface ISoloParentApiService
    {
        // ── Queries ─────────────────────────────────────────────────────────
        Task<BaseResponse<PagedResult<SoloParentSummaryDto>>> GetSoloParentsAsync(GetSoloParentRequest request);
        Task<BaseResponse<SoloParentDto>> GetSoloParentByIdAsync(Guid id);

        // ── Commands ─────────────────────────────────────────────────────────
        Task<BaseResponse<Guid>> CreateSoloParentAsync(CreateSoloParentRequest request);
        Task<BaseResponse<bool>> UpdateSoloParentAsync(Guid id, UpdateSoloParentRequest request);
        Task<BaseResponse<bool>> DeleteSoloParentAsync(Guid id);
        Task<BaseResponse<bool>> RenewSoloParentRecordAsync(Guid id);

        // ── Mapping helpers ──────────────────────────────────────────────────
        SoloParentRecord MapToRecord(SoloParentDto dto);
        CreateSoloParentRequest MapToCreateRequest(SoloParentRecord record);
        UpdateSoloParentRequest MapToUpdateRequest(Guid id, SoloParentRecord record, SoloParentDto existingDto = null);
    }
}
