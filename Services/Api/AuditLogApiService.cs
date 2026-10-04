using System;
using System.Text;
using System.Threading.Tasks;
using SOLUM_UI.Models.Api;

namespace SOLUM_UI.Services.Api
{
    /// <summary>
    /// Wraps the GET api/AuditLogs endpoint with cursor-based pagination and filtering.
    /// </summary>
    public class AuditLogApiService
    {
        private static readonly Lazy<AuditLogApiService> _instance =
            new Lazy<AuditLogApiService>(() => new AuditLogApiService());
        public static AuditLogApiService Instance => _instance.Value;

        private AuditLogApiService() { }

        /// <summary>
        /// Fetches a page of audit logs from the API.
        /// </summary>
        /// <param name="pageSize">Number of records to request (max 100).</param>
        /// <param name="userId">Optional user ID filter.</param>
        /// <param name="actionType">Optional action type filter (e.g. "Created", "Updated").</param>
        /// <param name="startDate">Optional start date filter.</param>
        /// <param name="endDate">Optional end date filter.</param>
        /// <param name="cursorTimestamp">Cursor for the next page (from previous response).</param>
        /// <param name="cursorId">Cursor ID for the next page (from previous response).</param>
        public async Task<BaseResponse<PagedAuditLogsResponse>> GetLogsAsync(
            int            pageSize         = 10,
            string         userId           = null,
            string         actionType       = null,
            DateTimeOffset? startDate       = null,
            DateTimeOffset? endDate         = null,
            DateTimeOffset? cursorTimestamp = null,
            string          cursorId        = null)
        {
            var sb = new StringBuilder("api/AuditLogs?");
            sb.Append($"pageSize={pageSize}");

            if (!string.IsNullOrWhiteSpace(userId))
                sb.Append($"&userId={Uri.EscapeDataString(userId.Trim())}");

            if (!string.IsNullOrWhiteSpace(actionType) &&
                !actionType.Equals("All", StringComparison.OrdinalIgnoreCase))
                sb.Append($"&actionType={Uri.EscapeDataString(actionType.Trim())}");

            if (startDate.HasValue)
                sb.Append($"&startDate={Uri.EscapeDataString(startDate.Value.ToString("O"))}");

            if (endDate.HasValue)
                sb.Append($"&endDate={Uri.EscapeDataString(endDate.Value.ToString("O"))}");

            if (cursorTimestamp.HasValue && !string.IsNullOrEmpty(cursorId))
            {
                sb.Append($"&cursorTimestamp={Uri.EscapeDataString(cursorTimestamp.Value.ToString("O"))}");
                sb.Append($"&cursorId={Uri.EscapeDataString(cursorId)}");
            }

            return await ApiClient.Instance.GetAsync<PagedAuditLogsResponse>(sb.ToString());
        }
    }
}
