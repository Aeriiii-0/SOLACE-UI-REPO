using SOLUM_UI.Models.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOLUM_UI.Services.Api
{
    public class UserApiService
    {
        private static readonly Lazy<UserApiService> _instance =
            new Lazy<UserApiService>(() => new UserApiService());
        public static UserApiService Instance => _instance.Value;

        private UserApiService() { }

        public async Task<BaseResponse<PagedResult<ApplicationUserDTO>>> GetApplicationUsersAsync(GetApplicationUserRequest request)
        {
            var queryParams = new List<string>();

            if (request.Id != Guid.Empty)
                queryParams.Add($"id={request.Id}");

            if (!string.IsNullOrWhiteSpace(request.Role))
                queryParams.Add($"role={Uri.EscapeDataString(request.Role.Trim())}");

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm.Trim())}");

            queryParams.Add($"isActive={request.IsActive.ToString().ToLowerInvariant()}");
            queryParams.Add($"page={request.Page}");
            queryParams.Add($"pageSize={request.PageSize}");

            string url = "api/user?" + string.Join("&", queryParams);
            return await ApiClient.Instance.GetAsync<PagedResult<ApplicationUserDTO>>(url);
        }

        public async Task<BaseResponse<bool>> UpdateApplicationUserAsync(UpdateApplicationUserRequest request)
        {
            return await ApiClient.Instance.PutAsync<UpdateApplicationUserRequest, bool>("api/user", request);
        }

        public async Task<BaseResponse<bool>> DeleteApplicationUserAsync(Guid id)
        {
            return await ApiClient.Instance.DeleteAsync<bool>($"api/user/{id}");
        }
    }
}
