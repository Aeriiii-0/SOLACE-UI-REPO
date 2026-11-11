using SOLUM_UI.Models;
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

        public string CurrentUserBarangay { get; private set; } = string.Empty;
        private string _explicitUserId = string.Empty;
        public string CurrentUserId
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_explicitUserId))
                    return _explicitUserId;
                var raw = ExtractRawUserIdFromToken(Token);
                if (!string.IsNullOrWhiteSpace(raw))
                    return raw;
                return ExtractUserIdFromToken(Token)?.ToString() ?? string.Empty;
            }
        }

        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);
        public bool IsSuperAdmin => CurrentRoles.Any(r => r.Equals(Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase));
        public bool IsAdmin => CurrentRoles.Any(r => r.Equals(Roles.Admin, StringComparison.OrdinalIgnoreCase) ||
                                                    r.Equals(Roles.Administrator, StringComparison.OrdinalIgnoreCase) ||
                                                    r.Equals(Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase));
        public bool IsAdminOrHigher => IsAdmin;
        public bool IsCaseworker => CurrentRoles.Any(r => r.Equals(Roles.Caseworker, StringComparison.OrdinalIgnoreCase) ||
                                                         r.Equals(Roles.Encoder, StringComparison.OrdinalIgnoreCase));
        public bool IsBarangaySecretary => CurrentRoles.Any(r => r.Equals(Roles.BarangaySecretary, StringComparison.OrdinalIgnoreCase) ||
                                                                 r.Equals("BARANGAY SECRETARY", StringComparison.OrdinalIgnoreCase));

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
                if (!string.IsNullOrWhiteSpace(resp.Data.Id))
                    _explicitUserId = resp.Data.Id;
                else if (!string.IsNullOrWhiteSpace(resp.Data.UserId))
                    _explicitUserId = resp.Data.UserId;
                CurrentRoles = resp.Data.Roles != null ? resp.Data.Roles.ToList() : new List<string>();
                ApiClient.Instance.SetBearerToken(Token);
            }
            return resp;
        }

        public event Action<string> OnSessionExpired;

        public void ClearSession()
        {
            _explicitUserId = string.Empty;
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
                if (!string.IsNullOrWhiteSpace(resp.Data.Id))
                    _explicitUserId = resp.Data.Id;
                else if (!string.IsNullOrWhiteSpace(resp.Data.UserId))
                    _explicitUserId = resp.Data.UserId;
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

        public async Task<BaseResponse<RegisterResponse>> RegisterSuperAdminAsync(string email, string password, string firstName, string lastName, string contactNumber = "")
        {
            var req = new RegisterUserRequest
            {
                Email = email?.Trim() ?? string.Empty,
                Password = password ?? string.Empty,
                FirstName = firstName?.Trim() ?? string.Empty,
                LastName = lastName?.Trim() ?? string.Empty,
                ContactNumber = contactNumber?.Trim() ?? string.Empty
            };
            return await ApiClient.Instance.PostAsync<RegisterUserRequest, RegisterResponse>("api/auth/register-superadmin", req);
        }

        public async Task<BaseResponse<RegisterResponse>> RegisterBarangaySecretaryAsync(string email, string password, string firstName, string lastName, string contactNumber, string barangay)
        {
            var req = new RegisterBarangaySecretaryRequest
            {
                Email = email?.Trim() ?? string.Empty,
                Password = password ?? string.Empty,
                FirstName = firstName?.Trim() ?? string.Empty,
                LastName = lastName?.Trim() ?? string.Empty,
                ContactNumber = contactNumber?.Trim() ?? string.Empty,
                Barangay = barangay?.Trim() ?? string.Empty
            };
            return await ApiClient.Instance.PostAsync<RegisterBarangaySecretaryRequest, RegisterResponse>("api/auth/register-barangay-secretary", req);
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

        public static string ExtractRawUserIdFromToken(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return string.Empty;

            string token = rawToken.Trim().Trim('"', '\'');   // strip stray quotes
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token.Substring(7).Trim();
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return string.Empty;
                var claims = handler.ReadJwtToken(token).Claims.ToList();
                var idClaim = claims.FirstOrDefault(c =>
                    string.Equals(c.Type, ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c.Type, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c.Type, "nameid", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c.Type, "id", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c.Type, "uid", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c.Type, "userId", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c.Type, "user_id", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c.Type, "sub", StringComparison.OrdinalIgnoreCase));
                return idClaim?.Value ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        
        public async Task<BaseResponse<bool>> UpdateProfileAsync(UpdateProfileRequest request)
        {
            var resp = await ApiClient.Instance.PutAsync<UpdateProfileRequest, object>("api/auth/profile", request);
            return new BaseResponse<bool>
            {
                Succeeded = resp.Succeeded,
                Errors = resp.Errors,
                Data = resp.Succeeded
            };
        }
        public static Guid? ExtractUserIdFromToken(string rawToken)
        {
            string raw = ExtractRawUserIdFromToken(rawToken);
            if (!string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out var parsed))
                return parsed;
            return null;
        }
    }
}