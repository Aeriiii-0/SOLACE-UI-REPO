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

        public async Task<BaseResponse<MonthlyAnalyticsDto>> GetMonthlyAnalyticsAsync(int year, int month, string barangay = null)
        {
            string url = $"api/analytics/monthly/{year}/{month}";

            if (!string.IsNullOrWhiteSpace(barangay) && !barangay.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                url += $"?barangay={Uri.EscapeDataString(barangay.Trim())}";
            }

            return await ApiClient.Instance.GetAsync<MonthlyAnalyticsDto>(url);
        }
    }
}
