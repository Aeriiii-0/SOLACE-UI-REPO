using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

            BaseUrl = configUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            _jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatString = "yyyy-MM-ddTHH:mm:ss"
            };
        }

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
                return await HandleResponseAsync<T>(response);
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
                return await HandleResponseAsync<TResponse>(response);
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
                return await HandleResponseAsync<TResponse>(response);
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
                return await HandleResponseAsync<TResponse>(response);
            }
            catch (Exception ex)
            {
                return ConnectionError<TResponse>(ex);
            }
        }

        private async Task<BaseResponse<T>> HandleResponseAsync<T>(HttpResponseMessage response)
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
                    return new BaseResponse<T> { Succeeded = true };
                }
            }

            var errorResult = new BaseResponse<T> { Succeeded = false };

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
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

        private BaseResponse<T> ConnectionError<T>(Exception ex)
        {
            string msg = $"Unable to communicate with the API server at {BaseUrl}. Please ensure the Solum.API backend is running.";
            if (ex is TaskCanceledException)
            {
                msg = "The request to the server timed out. Please try again.";
            }
            return BaseResponse<T>.Fail(msg, ex.Message);
        }
    }
}
