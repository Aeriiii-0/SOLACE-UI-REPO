using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SOLUM_UI.Models.Api;

namespace SOLUM_UI.Services.Api
{
    public class ApiClient
    {
        private static readonly Lazy<ApiClient> _instance = new Lazy<ApiClient>(() => new ApiClient());
        public static ApiClient Instance => _instance.Value;

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerSettings _jsonSettings;
        public string BaseUrl { get; }

        private ApiClient()
        {
            string configUrl = ConfigurationManager.AppSettings["ApiBaseUrl"];
            if (string.IsNullOrWhiteSpace(configUrl))
            {
                configUrl = "https://solum-api-176072351423.asia-east1.run.app";
            }
            if (!configUrl.EndsWith("/"))
            {
                configUrl += "/";
            }
            System.Net.ServicePointManager.DefaultConnectionLimit = 64;
            System.Net.ServicePointManager.SecurityProtocol |=
                System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;
            BaseUrl = configUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
                // No DateFormatString — let Newtonsoft handle ISO 8601 / DateTimeOffset natively
            };
        }

        private readonly System.Threading.SemaphoreSlim _refreshLock = new System.Threading.SemaphoreSlim(1, 1);

        public void SetBearerToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ClearBearerToken();
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public void ClearBearerToken()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<BaseResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                return await HandleResponseAsync<T>(response, endpoint, () => GetAsync<T>(endpoint));
            }
            catch (Exception ex)
            {
                return ConnectionError<T>(ex);
            }
        }

        public async Task<BaseResponse<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload)
        {
            try
            {
                string json = JsonConvert.SerializeObject(payload, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);
                return await HandleResponseAsync<TResponse>(response, endpoint, () => PostAsync<TRequest, TResponse>(endpoint, payload));
            }
            catch (Exception ex)
            {
                return ConnectionError<TResponse>(ex);
            }
        }

        public async Task<BaseResponse<TResponse>> PutAsync<TRequest, TResponse>(string endpoint, TRequest payload)
        {
            try
            {
                string json = JsonConvert.SerializeObject(payload, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(endpoint, content);
                return await HandleResponseAsync<TResponse>(response, endpoint, () => PutAsync<TRequest, TResponse>(endpoint, payload));
            }
            catch (Exception ex)
            {
                return ConnectionError<TResponse>(ex);
            }
        }

        public async Task<BaseResponse<TResponse>> PatchAsync<TRequest, TResponse>(string endpoint, TRequest payload)
        {
            try
            {
                string json = JsonConvert.SerializeObject(payload, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), endpoint) { Content = content };
                var response = await _httpClient.SendAsync(request);
                return await HandleResponseAsync<TResponse>(response, endpoint, () => PatchAsync<TRequest, TResponse>(endpoint, payload));
            }
            catch (Exception ex)
            {
                return ConnectionError<TResponse>(ex);
            }
        }

        public async Task<BaseResponse<TResponse>> DeleteAsync<TResponse>(string endpoint)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                return await HandleResponseAsync<TResponse>(response, endpoint, () => DeleteAsync<TResponse>(endpoint));
            }
            catch (Exception ex)
            {
                return ConnectionError<TResponse>(ex);
            }
        }

        private async Task<BaseResponse<T>> HandleResponseAsync<T>(
            HttpResponseMessage response,
            string endpoint = null,
            Func<Task<BaseResponse<T>>> retryFunc = null,
            bool isRetry = false)
        {
            string body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                if (string.IsNullOrWhiteSpace(body))
                {
                    return new BaseResponse<T> { Succeeded = true };
                }
                try
                {
                    var baseResp = JsonConvert.DeserializeObject<BaseResponse<T>>(body, _jsonSettings);
                    if (baseResp != null && baseResp.Data != null)
                    {
                        baseResp.StatusCode = (int)response.StatusCode;
                        return baseResp;
                    }
                    var rawData = JsonConvert.DeserializeObject<T>(body, _jsonSettings);
                    return new BaseResponse<T>
                    {
                        Succeeded = baseResp?.Succeeded ?? true,
                        Errors = baseResp?.Errors ?? new List<string>(),
                        Data = rawData
                    };
                }
                catch
                {
                    if (typeof(T) == typeof(string))
                    {
                        return (BaseResponse<T>)(object)BaseResponse<string>.Success(body);
                    }
                    try
                    {
                        var jToken = JToken.Parse(body);
                        if (jToken is JObject jObj)
                        {
                            var dataToken = jObj["data"] ?? jObj["Data"];
                            bool succeeded = jObj["succeeded"]?.Value<bool>() ?? jObj["Succeeded"]?.Value<bool>() ?? true;
                            var errors = jObj["errors"]?.ToObject<List<string>>() ?? jObj["Errors"]?.ToObject<List<string>>() ?? new List<string>();
                            // Scenario A: Caller expects List<TItem> or IEnumerable, but backend returned PagedResult with "items" inside data
                            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(typeof(T)) && typeof(T) != typeof(string))
                            {
                                var itemsToken = dataToken?["items"] ?? dataToken?["Items"] ?? jObj["items"] ?? jObj["Items"];
                                if (itemsToken != null)
                                {
                                    var items = itemsToken.ToObject<T>(JsonSerializer.Create(_jsonSettings));
                                    return new BaseResponse<T> { Succeeded = succeeded, Errors = errors, Data = items };
                                }
                            }
                            // Scenario B: Caller expects PagedResult<TItem>, but backend returned a flat list or array in data
                            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(PagedResult<>))
                            {
                                if (dataToken is JArray jArr)
                                {
                                    var itemType = typeof(T).GetGenericArguments()[0];
                                    var listType = typeof(List<>).MakeGenericType(itemType);
                                    var itemsList = jArr.ToObject(listType, JsonSerializer.Create(_jsonSettings));
                                    var pagedInstance = Activator.CreateInstance(typeof(T));
                                    typeof(T).GetProperty("Items")?.SetValue(pagedInstance, itemsList);
                                    int count = jArr.Count;
                                    typeof(T).GetProperty("TotalCount")?.SetValue(pagedInstance, count);
                                    typeof(T).GetProperty("Page")?.SetValue(pagedInstance, 1);
                                    typeof(T).GetProperty("PageSize")?.SetValue(pagedInstance, Math.Max(1, count));
                                    return new BaseResponse<T> { Succeeded = succeeded, Errors = errors, Data = (T)pagedInstance };
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Fall through to empty response
                    }
                    return new BaseResponse<T> { Succeeded = true };
                }
            }
            var errorResult = new BaseResponse<T> { Succeeded = false, StatusCode = (int)response.StatusCode };
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                bool isAuthEndpoint = !string.IsNullOrEmpty(endpoint) &&
                    (endpoint.IndexOf("api/auth/login", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     endpoint.IndexOf("api/auth/refresh-token", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     endpoint.IndexOf("api/auth/logout", StringComparison.OrdinalIgnoreCase) >= 0);
                if (!isRetry && retryFunc != null && !isAuthEndpoint &&
                    !string.IsNullOrWhiteSpace(AuthApiService.Instance.Token) &&
                    !string.IsNullOrWhiteSpace(AuthApiService.Instance.RefreshToken))
                {
                    bool lockAcquired = false;
                    string tokenBeforeLock = AuthApiService.Instance.Token;
                    try
                    {
                        // Use a longer timeout to avoid race conditions that trigger false session expirations
                        lockAcquired = await _refreshLock.WaitAsync(TimeSpan.FromSeconds(30));
                        if (lockAcquired)
                        {
                            // Another concurrent request already refreshed the token while we waited
                            if (!string.IsNullOrWhiteSpace(AuthApiService.Instance.Token) &&
                                !string.Equals(AuthApiService.Instance.Token, tokenBeforeLock, StringComparison.Ordinal))
                            {
                                return await retryFunc();
                            }
                            var refreshResult = await AuthApiService.Instance.RefreshTokenAsync();
                            if (refreshResult != null && refreshResult.Succeeded && refreshResult.Data != null && !string.IsNullOrWhiteSpace(refreshResult.Data.Token))
                            {
                                return await retryFunc();
                            }
                            else
                            {
                                // Only notify session expired if refresh explicitly failed (not a timeout)
                                AuthApiService.Instance.NotifySessionExpired("Session expired or revoked. Please log in again.");
                                return BaseResponse<T>.Fail("Session expired or revoked. Please log in again.");
                            }
                        }
                        else
                        {
                            // Lock timeout - don't trigger session expiration, just fail gracefully
                            return errorResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        AuthApiService.Instance.NotifySessionExpired("Session expired or revoked. Please log in again.");
                        return BaseResponse<T>.Fail("Session expired or revoked. Please log in again.", ex.Message);
                    }
                    finally
                    {
                        if (lockAcquired)
                        {
                            _refreshLock.Release();
                        }
                    }
                }
                else if (!isAuthEndpoint && response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Only notify session expired on actual 401, not on every error
                    AuthApiService.Instance.NotifySessionExpired("Session expired or revoked. Please log in again.");
                }
                errorResult.Errors.Add("Unauthorized access. Please log in again.");
                return errorResult;
            }
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                errorResult.Errors.Add("You do not have permission to perform this action.");
                return errorResult;
            }
            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    var errObj = JsonConvert.DeserializeObject<BaseResponse<T>>(body, _jsonSettings);
                    if (errObj?.Errors != null && errObj.Errors.Count > 0)
                    {
                        errorResult.Errors.AddRange(errObj.Errors);
                        return errorResult;
                    }
                    var listErrors = JsonConvert.DeserializeObject<List<string>>(body, _jsonSettings);
                    if (listErrors != null && listErrors.Count > 0)
                    {
                        errorResult.Errors.AddRange(listErrors);
                        return errorResult;
                    }
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(body, _jsonSettings);
                    if (dict != null)
                    {
                        if (dict.ContainsKey("errors") && dict["errors"] != null)
                        {
                            var errsStr = dict["errors"].ToString();
                            var subErrors = JsonConvert.DeserializeObject<List<string>>(errsStr);
                            if (subErrors != null && subErrors.Count > 0)
                            {
                                errorResult.Errors.AddRange(subErrors);
                                return errorResult;
                            }
                        }
                        if (dict.ContainsKey("message") && dict["message"] != null)
                        {
                            errorResult.Errors.Add(dict["message"].ToString());
                            return errorResult;
                        }
                    }
                }
                catch
                {
                    errorResult.Errors.Add(body.Trim('"'));
                    return errorResult;
                }
            }
            errorResult.Errors.Add($"Server responded with {(int)response.StatusCode} ({response.ReasonPhrase}).");
            return errorResult;
        }

        public static bool IsNetworkOrConnectionError(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            string lower = message.ToLowerInvariant();
            return lower.Contains("internet") ||
                   lower.Contains("network") ||
                   lower.Contains("socket") ||
                   lower.Contains("unable to connect") ||
                   lower.Contains("connection refused") ||
                   lower.Contains("connection reset") ||
                   lower.Contains("unreachable") ||
                   lower.Contains("no such host") ||
                   lower.Contains("name resolution") ||
                   lower.Contains("offline") ||
                   lower.Contains("timed out") ||
                   lower.Contains("timeout");
        }

        public static string HumanizeErrorMessage(string rawError)
        {
            if (string.IsNullOrWhiteSpace(rawError))
                return "An unexpected issue occurred. Please try again.";

            string lower = rawError.ToLowerInvariant();
            if (lower.Contains("no internet") || lower.Contains("network is unreachable") || lower.Contains("no such host"))
                return "No internet connection detected. Please check your network and try again.";
            if (lower.Contains("timed out") || lower.Contains("timeout"))
                return "The connection timed out. Please check your network and try again.";
            if (lower.Contains("unable to connect") || lower.Contains("connection refused") || lower.Contains("connection reset"))
                return "Unable to connect to the server. Please ensure you have internet access and the server is online.";

            return rawError;
        }

        private BaseResponse<T> ConnectionError<T>(Exception ex)
        {
            // Check if it's specifically a network connectivity issue
            if (IsNoInternetError(ex))
            {
                return BaseResponse<T>.Fail("No internet connection detected. Please check your network and try again.");
            }

            if (ex is TaskCanceledException || ex is TimeoutException)
            {
                return BaseResponse<T>.Fail("The request timed out. Please check your network and try again.");
            }

            string msg = "Unable to connect to the server. Please check your internet connection and ensure the server is online.";
            return BaseResponse<T>.Fail(msg);
        }

        private bool IsNoInternetError(Exception ex)
        {
            if (ex == null) return false;

            try
            {
                if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    return true;
            }
            catch { }

            Exception current = ex;
            while (current != null)
            {
                string typeName = current.GetType().Name;
                string msg = current.Message ?? string.Empty;

                if (typeName.Contains("SocketException") ||
                    typeName.Contains("NetworkInformationException") ||
                    typeName.Contains("WebException") ||
                    typeName.Contains("HttpRequestException") ||
                    typeName.Contains("IOException"))
                {
                    if (msg.IndexOf("unable to reach", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        msg.IndexOf("no such host", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        msg.IndexOf("name resolution", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        msg.IndexOf("connection refused", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        msg.IndexOf("connection reset", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        msg.IndexOf("network is unreachable", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        msg.IndexOf("the host is down", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        msg.IndexOf("no internet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        msg.IndexOf("an error occurred while sending the request", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                current = current.InnerException;
            }

            return false;
        }
    }
}