using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SOLUM_UI.Models.Api;

namespace SOLUM_UI.Services.Api
{
    public class AuthApiService
    {
        private static readonly Lazy<AuthApiService> _instance =
            new Lazy<AuthApiService>(() => new AuthApiService());
        public static AuthApiService Instance => _instance.Value;

        public string CurrentUserEmail { get; private set; } = string.Empty;
        public string Token { get; private set; } = string.Empty;
        public string RefreshToken { get; private set; } = string.Empty;
        public List<string> CurrentRoles { get; private set; } = new List<string>();

        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);
        public bool IsAdmin => CurrentRoles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                                                    r.Equals("Administrator", StringComparison.OrdinalIgnoreCase));
        public bool IsEncoder => CurrentRoles.Any(r => r.Equals("Encoder", StringComparison.OrdinalIgnoreCase));

        private AuthApiService() { }

        public async Task<BaseResponse<LoginResponse>> LoginAsync(string email, string password)
        {
            var req = new LoginRequest
            {
                Email = email?.Trim() ?? string.Empty,
                Password = password ?? string.Empty
            };

            var resp = await ApiClient.Instance.PostAsync<LoginRequest, LoginResponse>("api/auth/login", req);

            if (resp.Succeeded && resp.Data != null && !string.IsNullOrWhiteSpace(resp.Data.Token))
            {
                Token = resp.Data.Token;
                RefreshToken = resp.Data.RefreshToken;
                CurrentUserEmail = resp.Data.Email;
                CurrentRoles = resp.Data.Roles != null ? resp.Data.Roles.ToList() : new List<string>();

                ApiClient.Instance.SetBearerToken(Token);
            }

            return resp;
        }

        public async Task<BaseResponse<bool>> LogoutAsync()
        {
            try
            {
                if (IsAuthenticated)
                {
                    await ApiClient.Instance.PostAsync<object, object>("api/auth/logout", new { });
                }
            }
            finally
            {
                Token = string.Empty;
                RefreshToken = string.Empty;
                CurrentUserEmail = string.Empty;
                CurrentRoles.Clear();
                ApiClient.Instance.ClearBearerToken();
            }

            return new BaseResponse<bool> { Succeeded = true, Data = true };
        }

        public async Task<BaseResponse<LoginResponse>> RefreshTokenAsync()
        {
            if (string.IsNullOrWhiteSpace(Token) || string.IsNullOrWhiteSpace(RefreshToken))
            {
                return BaseResponse<LoginResponse>.Fail("No token available to refresh.");
            }

            var req = new TokenRequest
            {
                ExpiredToken = Token,
                RefreshToken = RefreshToken
            };

            var resp = await ApiClient.Instance.PostAsync<TokenRequest, LoginResponse>("api/auth/refresh-token", req);

            if (resp.Succeeded && resp.Data != null && !string.IsNullOrWhiteSpace(resp.Data.Token))
            {
                Token = resp.Data.Token;
                RefreshToken = resp.Data.RefreshToken;
                CurrentUserEmail = resp.Data.Email;
                CurrentRoles = resp.Data.Roles != null ? resp.Data.Roles.ToList() : new List<string>();

                ApiClient.Instance.SetBearerToken(Token);
            }

            return resp;
        }

        public async Task<BaseResponse<RegisterResponse>> RegisterAsync(string email, string password, string firstName, string lastName)
        {
            var req = new RegisterUserRequest
            {
                Email = email?.Trim() ?? string.Empty,
                Password = password ?? string.Empty,
                FirstName = firstName?.Trim() ?? string.Empty,
                LastName = lastName?.Trim() ?? string.Empty
            };

            return await ApiClient.Instance.PostAsync<RegisterUserRequest, RegisterResponse>("api/auth/register", req);
        }

        public async Task<BaseResponse<string>> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            var req = new ChangePasswordRequest
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            };

            var resp = await ApiClient.Instance.PostAsync<ChangePasswordRequest, object>("api/auth/change-password", req);
            return new BaseResponse<string>
            {
                Succeeded = resp.Succeeded,
                Errors = resp.Errors,
                Data = resp.Succeeded ? "Password Changed Successfully." : null
            };
        }
    }
}
