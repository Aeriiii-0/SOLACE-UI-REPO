using SOLUM_UI.Models.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOLUM_UI.Services.Api
{
    public class AnalyticsApiService
    {
        private static readonly Lazy<AnalyticsApiService> _instance =
            new Lazy<AnalyticsApiService>(() => new AnalyticsApiService());
        public static AnalyticsApiService Instance => _instance.Value;

        private AnalyticsApiService() { }

        public async Task<BaseResponse<MonthlyAnalyticsDto>> GetMonthlyAnalyticsAsync(
            int year, int month, string barangay = null,
            DateTime? startDate = null, DateTime? endDate = null)
        {
            string url = $"api/analytics/monthly/{year}/{month}";

            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(barangay) && !barangay.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                queryParams.Add($"barangay={Uri.EscapeDataString(barangay.Trim())}");
            }

            if (startDate.HasValue)
            {
                queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
            }

            if (endDate.HasValue)
            {
                queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
            }

            if (queryParams.Count > 0)
            {
                url += "?" + string.Join("&", queryParams);
            }

            return await ApiClient.Instance.GetAsync<MonthlyAnalyticsDto>(url);
        }

        public async Task<BaseResponse<DashboardAnalyticsDto>> GetDashboardAnalyticsAsync()
        {
            string url = "api/analytics/dashboard";
            return await ApiClient.Instance.GetAsync<DashboardAnalyticsDto>(url);
        }

        public async Task<BaseResponse<BarangayAnalyticsResponseDto>> GetBarangayBreakdownAsync(
            int? quarter = null,
            int? year = null,
            string barangay = null,
            string type = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var queryParams = new List<string>();

            if (quarter.HasValue)
            {
                queryParams.Add($"quarter={quarter.Value}");
            }

            if (year.HasValue)
            {
                queryParams.Add($"year={year.Value}");
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                queryParams.Add($"type={Uri.EscapeDataString(type.Trim())}");
            }

            if (startDate.HasValue)
            {
                queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
            }

            if (endDate.HasValue)
            {
                queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
            }

            if (!string.IsNullOrWhiteSpace(barangay) && !barangay.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                queryParams.Add($"barangay={Uri.EscapeDataString(barangay.Trim())}");
            }
            else if (!string.IsNullOrWhiteSpace(barangay))
            {
                queryParams.Add("barangay=ALL");
            }

            string url = "api/analytics/barangay-breakdown" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "");
            return await ApiClient.Instance.GetAsync<BarangayAnalyticsResponseDto>(url);
        }
    }
}
