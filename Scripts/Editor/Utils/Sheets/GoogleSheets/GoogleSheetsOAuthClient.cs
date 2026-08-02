using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Http;
using Newtonsoft.Json.Linq;
using TeamODD.ODDB.Editors.Settings;
using UnityEngine;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    internal static class GoogleSheetsOAuthClient
    {
        private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
        private const string Scope = "https://www.googleapis.com/auth/spreadsheets";
        private const string CallbackPath = "oauth2callback/";
        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly SemaphoreSlim TokenGate = new SemaphoreSlim(1, 1);

        public static async Task<IConfigurableHttpClientInitializer> CreateCredentialAsync(CancellationToken ct)
        {
            var credential = new BearerCredential();
            await credential.EnsureAccessTokenAsync(ct);
            return credential;
        }

        public static async Task SignInAsync(CancellationToken ct = default)
        {
            var oauthClient = GetOAuthClientConfiguration();
            await TokenGate.WaitAsync(ct);
            try
            {
                var token = await AuthorizeInteractivelyAsync(oauthClient, ct);
                SaveToken(token);
            }
            finally
            {
                TokenGate.Release();
            }
        }

        public static Task SignOutAsync()
        {
            return GoogleSheetsUserSettings.ClearAuthorizationAsync();
        }

        internal static string CreateCodeVerifier()
        {
            var bytes = new byte[64];
            using (var random = RandomNumberGenerator.Create())
                random.GetBytes(bytes);
            return Base64Url(bytes);
        }

        internal static string CreateCodeChallenge(string verifier)
        {
            if (string.IsNullOrEmpty(verifier))
                throw new ArgumentException("PKCE verifier is required.", nameof(verifier));

            using (var sha256 = SHA256.Create())
                return Base64Url(sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier)));
        }

        internal static string BuildAuthorizationUrl(
            string clientId,
            string redirectUri,
            string state,
            string codeChallenge)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("OAuth Client ID is required.", nameof(clientId));

            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["redirect_uri"] = redirectUri,
                ["response_type"] = "code",
                ["scope"] = Scope,
                ["access_type"] = "offline",
                ["prompt"] = "consent select_account",
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256",
                ["state"] = state
            };
            return AuthorizationEndpoint + "?" + EncodeForm(parameters);
        }

        private static async Task<string> GetAccessTokenAsync(CancellationToken ct)
        {
            var oauthClient = GetOAuthClientConfiguration();
            await TokenGate.WaitAsync(ct);
            try
            {
                var token = LoadToken();
                if (token == null)
                    throw new InvalidOperationException("Sign in with Google from ODDBEditorSettings first.");

                if (!string.IsNullOrEmpty(token.ClientId)
                    && !string.Equals(token.ClientId, oauthClient.ClientId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The Google OAuth Client ID changed. Sign out, then sign in with Google again.");
                }

                if (!string.IsNullOrEmpty(token.AccessToken)
                    && token.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(1))
                {
                    return token.AccessToken;
                }

                if (string.IsNullOrEmpty(token.RefreshToken))
                    throw new InvalidOperationException("Google authorization expired. Sign out, then sign in again.");

                token = await RefreshAsync(oauthClient, token.RefreshToken, ct);
                SaveToken(token);
                return token.AccessToken;
            }
            finally
            {
                TokenGate.Release();
            }
        }

        private static async Task<GoogleOAuthToken> AuthorizeInteractivelyAsync(
            GoogleOAuthClientConfiguration oauthClient,
            CancellationToken ct)
        {
            var verifier = CreateCodeVerifier();
            var challenge = CreateCodeChallenge(verifier);
            var state = CreateCodeVerifier();
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            try
            {
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                var redirectUri = $"http://127.0.0.1:{port}/{CallbackPath}";
                var authorizationUrl = BuildAuthorizationUrl(oauthClient.ClientId, redirectUri, state, challenge);
                Application.OpenURL(authorizationUrl);

                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
                using (timeout.Token.Register(listener.Stop))
                {
                    timeout.CancelAfter(TimeSpan.FromMinutes(5));
                    OAuthCallback callback;
                    try
                    {
                        callback = await ReceiveCallbackAsync(listener, redirectUri);
                    }
                    catch (Exception) when (timeout.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                        throw new TimeoutException("Google sign-in timed out after 5 minutes.");
                    }

                    if (!string.IsNullOrEmpty(callback.Error))
                        throw new InvalidOperationException("Google authorization failed: " + callback.Error);
                    if (!string.Equals(callback.State, state, StringComparison.Ordinal))
                        throw new InvalidOperationException("Google authorization returned an invalid state value.");
                    if (string.IsNullOrEmpty(callback.Code))
                        throw new InvalidOperationException("Google authorization did not return an authorization code.");

                    return await ExchangeCodeAsync(oauthClient, callback.Code, verifier, redirectUri, timeout.Token);
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private static async Task<OAuthCallback> ReceiveCallbackAsync(TcpListener listener, string redirectUri)
        {
            using (var client = await listener.AcceptTcpClientAsync())
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
            {
                var requestLine = await reader.ReadLineAsync();
                string header;
                do
                {
                    header = await reader.ReadLineAsync();
                } while (!string.IsNullOrEmpty(header));

                var target = ParseRequestTarget(requestLine);
                var uri = new Uri(new Uri(redirectUri), target);
                var values = ParseQuery(uri.Query);
                var callback = new OAuthCallback
                {
                    Code = GetValue(values, "code"),
                    State = GetValue(values, "state"),
                    Error = GetValue(values, "error")
                };

                var success = string.IsNullOrEmpty(callback.Error) && !string.IsNullOrEmpty(callback.Code);
                await WriteBrowserResponseAsync(stream, success);
                return callback;
            }
        }

        private static async Task<GoogleOAuthToken> ExchangeCodeAsync(
            GoogleOAuthClientConfiguration oauthClient,
            string code,
            string verifier,
            string redirectUri,
            CancellationToken ct)
        {
            return await RequestTokenAsync(new Dictionary<string, string>
            {
                ["client_id"] = oauthClient.ClientId,
                ["client_secret"] = oauthClient.ClientSecret,
                ["code"] = code,
                ["code_verifier"] = verifier,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            }, null, oauthClient.ClientId, ct);
        }

        private static async Task<GoogleOAuthToken> RefreshAsync(
            GoogleOAuthClientConfiguration oauthClient,
            string refreshToken,
            CancellationToken ct)
        {
            return await RequestTokenAsync(new Dictionary<string, string>
            {
                ["client_id"] = oauthClient.ClientId,
                ["client_secret"] = oauthClient.ClientSecret,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token"
            }, refreshToken, oauthClient.ClientId, ct);
        }

        private static async Task<GoogleOAuthToken> RequestTokenAsync(
            Dictionary<string, string> parameters,
            string existingRefreshToken,
            string clientId,
            CancellationToken ct)
        {
            using (var content = new FormUrlEncodedContent(parameters))
            using (var response = await HttpClient.PostAsync(TokenEndpoint, content, ct))
            {
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                if (!response.IsSuccessStatusCode)
                {
                    var message = json.Value<string>("error_description") ?? json.Value<string>("error") ?? "unknown_error";
                    throw new InvalidOperationException("Google OAuth token request failed: " + message);
                }

                var accessToken = json.Value<string>("access_token");
                if (string.IsNullOrEmpty(accessToken))
                    throw new InvalidOperationException("Google OAuth token response did not contain an access token.");

                var expiresIn = json.Value<int?>("expires_in") ?? 3600;
                return new GoogleOAuthToken
                {
                    ClientId = clientId,
                    AccessToken = accessToken,
                    RefreshToken = json.Value<string>("refresh_token") ?? existingRefreshToken,
                    ExpiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, expiresIn))
                };
            }
        }

        private static GoogleOAuthToken LoadToken()
        {
            var path = GoogleSheetsUserSettings.TokenFilePath;
            if (!File.Exists(path))
                return null;

            try
            {
                var json = JObject.Parse(File.ReadAllText(path));
                return new GoogleOAuthToken
                {
                    ClientId = json.Value<string>("client_id"),
                    AccessToken = json.Value<string>("access_token"),
                    RefreshToken = json.Value<string>("refresh_token"),
                    ExpiresAtUtc = json.Value<DateTime?>("expires_at_utc") ?? DateTime.MinValue
                };
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("The stored Google authorization is invalid. Sign out, then sign in again.", e);
            }
        }

        private static void SaveToken(GoogleOAuthToken token)
        {
            if (token == null || string.IsNullOrEmpty(token.AccessToken))
                throw new ArgumentException("A valid OAuth token is required.", nameof(token));

            Directory.CreateDirectory(GoogleSheetsUserSettings.TokenStorePath);
            var json = new JObject
            {
                ["client_id"] = token.ClientId,
                ["access_token"] = token.AccessToken,
                ["refresh_token"] = token.RefreshToken,
                ["expires_at_utc"] = token.ExpiresAtUtc
            };
            File.WriteAllText(GoogleSheetsUserSettings.TokenFilePath, json.ToString());
        }

        private static string ParseRequestTarget(string requestLine)
        {
            if (string.IsNullOrEmpty(requestLine))
                throw new InvalidOperationException("Google authorization callback was empty.");

            var parts = requestLine.Split(' ');
            if (parts.Length < 2 || !string.Equals(parts[0], "GET", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Google authorization callback was not a valid HTTP GET request.");
            return parts[1];
        }

        internal static Dictionary<string, string> ParseQuery(string query)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(query))
                return values;

            foreach (var item in query.TrimStart('?').Split('&'))
            {
                if (string.IsNullOrEmpty(item))
                    continue;
                var separator = item.IndexOf('=');
                var key = separator < 0 ? item : item.Substring(0, separator);
                var value = separator < 0 ? string.Empty : item.Substring(separator + 1);
                values[DecodeForm(key)] = DecodeForm(value);
            }
            return values;
        }

        private static string GetValue(Dictionary<string, string> values, string key)
        {
            return values.TryGetValue(key, out var value) ? value : string.Empty;
        }

        private static string EncodeForm(IEnumerable<KeyValuePair<string, string>> values)
        {
            var parts = new List<string>();
            foreach (var pair in values)
                parts.Add(Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value ?? string.Empty));
            return string.Join("&", parts);
        }

        private static string DecodeForm(string value)
        {
            return Uri.UnescapeDataString((value ?? string.Empty).Replace('+', ' '));
        }

        private static string Base64Url(byte[] value)
        {
            return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static GoogleOAuthClientConfiguration GetOAuthClientConfiguration()
        {
            if (!ODDBEditorSettings.Setting.TryGetGoogleOAuthClientConfiguration(
                    out var clientId,
                    out var clientSecret,
                    out var failureReason))
            {
                throw new InvalidOperationException(failureReason);
            }

            return new GoogleOAuthClientConfiguration
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };
        }

        private static async Task WriteBrowserResponseAsync(Stream stream, bool success)
        {
            var title = success ? "ODDB authorization received" : "ODDB authorization failed";
            var message = success
                ? "Return to Unity while ODDB finishes signing in. You can close this window."
                : "Return to Unity for error details. You can close this window.";
            var body = "<!doctype html><html><head><meta charset=\"utf-8\"><title>" + title +
                       "</title></head><body><h2>" + title +
                       "</h2><p>" + message + "</p></body></html>";
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var header = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: " +
                         bodyBytes.Length + "\r\nConnection: close\r\n\r\n";
            var headerBytes = Encoding.ASCII.GetBytes(header);
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
            await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
            await stream.FlushAsync();
        }

        private sealed class BearerCredential : IConfigurableHttpClientInitializer, IHttpExecuteInterceptor
        {
            public void Initialize(ConfigurableHttpClient httpClient)
            {
                httpClient.MessageHandler.AddExecuteInterceptor(this);
            }

            public async Task InterceptAsync(HttpRequestMessage request, CancellationToken ct)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    await GetAccessTokenAsync(ct));
            }

            public Task EnsureAccessTokenAsync(CancellationToken ct)
            {
                return GetAccessTokenAsync(ct);
            }
        }

        private sealed class GoogleOAuthToken
        {
            public string ClientId { get; set; }
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public DateTime ExpiresAtUtc { get; set; }
        }

        private sealed class GoogleOAuthClientConfiguration
        {
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
        }

        private sealed class OAuthCallback
        {
            public string Code { get; set; }
            public string State { get; set; }
            public string Error { get; set; }
        }
    }
}
