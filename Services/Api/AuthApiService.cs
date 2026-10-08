using SOLUM_UI.Models.Api;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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

        public string CurrentUserId => ExtractUserIdFromToken(Token)?.ToString() ?? string.Empty;

        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);
        public bool IsAdmin => CurrentRoles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                                                    r.Equals("Administrator", StringComparison.OrdinalIgnoreCase));
        public bool IsEncoder => CurrentRoles.Any(r => r.Equals("Encoder", StringComparison.OrdinalIgnoreCase)) ||
                                 !IsAdmin;

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

        public event Action<string> OnSessionExpired;

        public void ClearSession()
        {
            Token = string.Empty;
            RefreshToken = string.Empty;
            CurrentUserEmail = string.Empty;
            CurrentRoles.Clear();
            ApiClient.Instance.ClearBearerToken();
        }

        public void NotifySessionExpired(string message = "Session expired or revoked. Please log in again.")
        {
            ClearSession();
            OnSessionExpired?.Invoke(message);
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
                ClearSession();
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

        public async Task<BaseResponse<RegisterResponse>> RegisterAsync(string email, string password, string firstName, string lastName, string contactNumber = "")
        {
            var req = new RegisterUserRequest
            {
                Email = email?.Trim() ?? string.Empty,
                Password = password ?? string.Empty,
                FirstName = firstName?.Trim() ?? string.Empty,
                LastName = lastName?.Trim() ?? string.Empty,
                ContactNumber = contactNumber?.Trim() ?? string.Empty
            };

            return await ApiClient.Instance.PostAsync<RegisterUserRequest, RegisterResponse>("api/auth/register", req);
        }

        public async Task<BaseResponse<RegisterResponse>> RegisterAdminAsync(string email, string password, string firstName, string lastName, string contactNumber = "")
        {
            var req = new RegisterUserRequest
            {
                Email = email?.Trim() ?? string.Empty,
                Password = password ?? string.Empty,
                FirstName = firstName?.Trim() ?? string.Empty,
                LastName = lastName?.Trim() ?? string.Empty,
                ContactNumber = contactNumber?.Trim() ?? string.Empty
            };

            return await ApiClient.Instance.PostAsync<RegisterUserRequest, RegisterResponse>("api/auth/register-admin", req);
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

        public static Guid? ExtractUserIdFromToken(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return null;

            string token = rawToken.Trim().Trim('"', '\'');   // strip stray quotes
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token.Substring(7).Trim();

            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return null;

                var value = handler.ReadJwtToken(token).Claims
                    .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier
                                  || c.Type == "nameid"
                                  || c.Type == "sub")?.Value;

                return Guid.TryParse(value, out var id) ? id : (Guid?)null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
