using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BeanModManager.Helpers;

namespace BeanModManager.Services
{
    public class EpicSession
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string AccountId { get; set; }
    }

    public class EpicApiService
    {
        private const string OAUTH_HOST = "account-public-service-prod03.ol.epicgames.com";
        private const string LAUNCHER_CLIENT_ID = "34a02cf8f4414e29b15921876da36f9a";
        private const string LAUNCHER_CLIENT_SECRET = "daafbccc737745039dffe53d94fc76cf";
        private const string USER_AGENT = "UELauncher/11.0.1-14907503+++Portal+Release-Live Windows/10.0.19041.1.256.64bit";

        private static readonly HttpClient _httpClient;

        static EpicApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
        }

        public static string GetAuthUrl()
        {
            var redirect = $"https://www.epicgames.com/id/api/redirect?clientId={LAUNCHER_CLIENT_ID}&responseType=code";
            var encodedRedirect = Uri.EscapeDataString(redirect);
            return $"https://www.epicgames.com/id/login?redirectUrl={encodedRedirect}";
        }

        public async Task<EpicSession> LoginWithAuthCodeAsync(string code)
        {
            code = code.Trim().Replace("\"", "");
            return await OAuthRequestAsync(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "token_type", "eg1" }
            });
        }

        public async Task<EpicSession> RefreshSessionAsync(string refreshToken)
        {
            return await OAuthRequestAsync(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
                { "token_type", "eg1" }
            });
        }

        private async Task<EpicSession> OAuthRequestAsync(Dictionary<string, string> parameters)
        {
            var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{LAUNCHER_CLIENT_ID}:{LAUNCHER_CLIENT_SECRET}"));
            var url = $"https://{OAUTH_HOST}/account/api/oauth/token";

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuth);
                request.Content = new FormUrlEncodedContent(parameters);

                using (var response = await _httpClient.SendAsync(request).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        throw new Exception($"OAuth request failed ({response.StatusCode}): {errorBody}");
                    }

                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var result = JsonHelper.Deserialize<EpicOAuthResponse>(json);

                    if (result == null || string.IsNullOrEmpty(result.access_token))
                    {
                        throw new Exception("Failed to parse OAuth response");
                    }

                    return new EpicSession
                    {
                        AccessToken = result.access_token,
                        RefreshToken = result.refresh_token,
                        AccountId = result.account_id
                    };
                }
            }
        }

        public async Task<string> GetGameTokenAsync(EpicSession session)
        {
            var url = $"https://{OAUTH_HOST}/account/api/oauth/exchange";

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.AccessToken);

                using (var response = await _httpClient.SendAsync(request).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        throw new Exception($"Game token request failed ({response.StatusCode}): {errorBody}");
                    }

                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var result = JsonHelper.Deserialize<EpicGameTokenResponse>(json);

                    if (result == null || string.IsNullOrEmpty(result.code))
                    {
                        throw new Exception("Failed to parse game token response");
                    }

                    return result.code;
                }
            }
        }

        private class EpicOAuthResponse
        {
            public string access_token { get; set; }
            public string refresh_token { get; set; }
            public string account_id { get; set; }
        }

        private class EpicGameTokenResponse
        {
            public string code { get; set; }
        }
    }
}