using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AuthProfiles.Domain;
using AuthProfiles.Application.Dtos;

namespace AuthProfiles.Infrastructure.Services
{
    public class AuthTokenService : Application.Services.IAuthTokenService
    {
        private readonly HttpClient _http;

        public AuthTokenService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<TestAuthResult> AcquireAsync(AuthProfile profile, IReadOnlyDictionary<string, string> resolvedSecrets, CancellationToken ct)
        {
            if (profile.Type != AuthType.OAuth2ClientCredentials)
                return new TestAuthResult(false, "unsupported_grant_type", "Only client_credentials supported by this implementation", null, null, null, null, null, null);

            if (string.IsNullOrWhiteSpace(profile.TokenUrl))
                return new TestAuthResult(false, "invalid_config", "TokenUrl missing", null, null, null, null, null, null);

            // Resolve required secrets (CLIENT_ID, CLIENT_SECRET) from resolvedSecrets
            if (!resolvedSecrets.TryGetValue("CLIENT_ID", out var clientId) || string.IsNullOrWhiteSpace(clientId))
                return new TestAuthResult(false, "missing_secret", "CLIENT_ID is required", null, null, null, null, null, null);

            if (!resolvedSecrets.TryGetValue("CLIENT_SECRET", out var clientSecret) || string.IsNullOrWhiteSpace(clientSecret))
                return new TestAuthResult(false, "missing_secret", "CLIENT_SECRET is required", null, null, null, null, null, null);

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            };

            if (!string.IsNullOrWhiteSpace(profile.Audience)) form["audience"] = profile.Audience;

            var req = new HttpRequestMessage(HttpMethod.Post, profile.TokenUrl)
            {
                Content = new FormUrlEncodedContent(form)
            };

            try
            {
                var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
                var payload = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                if (!res.IsSuccessStatusCode)
                    return new TestAuthResult(false, "invalid_client", "Token endpoint returned error: " + res.StatusCode, null, null, null, null, null, null);

                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                var accessToken = root.GetProperty("access_token").GetString();
                var tokenType = root.TryGetProperty("token_type", out var tt) ? tt.GetString() : "Bearer";
                var expiresIn = root.TryGetProperty("expires_in", out var ex) && ex.TryGetInt32(out var secs) ? secs : 3600;
                var exp = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

                var headers = new Dictionary<string, string>
                {
                    ["Authorization"] = $"{tokenType} {accessToken}"
                };

                return new TestAuthResult(true, "ok", null, accessToken, accessToken, tokenType, exp, headers, null);
            }
            catch (HttpRequestException ex)
            {
                return new TestAuthResult(false, "network_error", ex.Message, null, null, null, null, null, null);
            }
        }
    }
}
