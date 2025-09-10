using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using AuthProfiles.Application.Dtos;
using AuthProfiles.Application.Services;

namespace AuthProfiles.Infrastructure.Services
{
    public class AuthProfileTester : IAuthProfileTester
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ISecretStore _secrets;

        public AuthProfileTester(IHttpClientFactory httpFactory, ISecretStore secrets)
        {
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
            _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        }

        public async Task<TestAuthResult> TestAsync(TestAuthRequest request, CancellationToken ct)
        {
            // Resolve inline or fetch profile; For now expect inline profile in request
            var profile = request.ProfileInline;
            if (profile == null) return new TestAuthResult(false, "error", "No profile provided", null, null, null, null, null, null);

            // Extract scheme
            var type = profile.Type;

            // resolve secrets via OverrideSecretValues first
            var overrides = request.OverrideSecretValues ?? new Dictionary<string, string>();

            try
            {
                if (type == AuthProfiles.Domain.AuthType.OAuth2Password || type == AuthProfiles.Domain.AuthType.OAuth2ClientCredentials)
                {
                    var tokenUrl = profile.TokenUrl ?? (profile.Params != null && profile.Params.TryGetValue("TokenUrl", out var tv) ? tv?.ToString() : null);
                    if (string.IsNullOrEmpty(tokenUrl)) return new TestAuthResult(false, "error", "TokenUrl missing", null, null, null, null, null, null);
                    var form = new List<KeyValuePair<string, string>>();
                    form.Add(new KeyValuePair<string, string>("grant_type", type == AuthProfiles.Domain.AuthType.OAuth2Password ? "password" : "client_credentials"));
                    if (profile.Params != null && profile.Params.TryGetValue("ClientId", out var cid) && cid != null) form.Add(new KeyValuePair<string, string>("client_id", cid.ToString()!));
                    if (profile.Params != null && profile.Params.TryGetValue("ClientSecretRef", out var csr) && csr != null)
                    {
                        var csrKey = csr.ToString()!;
                        var secret = overrides.ContainsKey(csrKey) ? overrides[csrKey] : await _secrets.GetAsync(csrKey, ct);
                        if (!string.IsNullOrEmpty(secret)) form.Add(new KeyValuePair<string, string>("client_secret", secret));
                    }
                    if (type == AuthProfiles.Domain.AuthType.OAuth2Password)
                    {
                        if (profile.Params != null && profile.Params.TryGetValue("UsernameRef", out var ur) && ur != null)
                        {
                            var urKey = ur.ToString()!;
                            var u = overrides.ContainsKey(urKey) ? overrides[urKey] : await _secrets.GetAsync(urKey, ct);
                            if (string.IsNullOrEmpty(u)) return new TestAuthResult(false, "error", "Failed to resolve username", null, null, null, null, null, null);
                            form.Add(new KeyValuePair<string, string>("username", u!));
                        }
                        if (profile.Params != null && profile.Params.TryGetValue("PasswordRef", out var pr) && pr != null)
                        {
                            var prKey = pr.ToString()!;
                            var p = overrides.ContainsKey(prKey) ? overrides[prKey] : await _secrets.GetAsync(prKey, ct);
                            if (string.IsNullOrEmpty(p)) return new TestAuthResult(false, "error", "Failed to resolve password", null, null, null, null, null, null);
                            form.Add(new KeyValuePair<string, string>("password", p!));
                        }
                    }
                    var client = _httpFactory.CreateClient();
                    var res = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(form), ct).ConfigureAwait(false);
                    var body = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    // try parse json access_token
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("access_token", out var at))
                        {
                            var token = at.GetString();
                            var prefix = token?.Length >= 8 ? token.Substring(0, 8) : token;
                            return new TestAuthResult(true, "ok", null, prefix, null, null, null, new Dictionary<string, string> { { "Authorization", "Bearer " + prefix } }, null);
                        }
                    }
                    catch { }
                    return new TestAuthResult(false, "error", "Failed to parse token", null, null, null, null, null, null);
                }

                if (type == AuthProfiles.Domain.AuthType.Basic)
                {
                    // Basic: just ensure username/password resolve
                    if (profile.Params == null) return new TestAuthResult(false, "error", "Missing params", null, null, null, null, null, null);
                    var ur = profile.Params.ContainsKey("UsernameRef") ? profile.Params["UsernameRef"]?.ToString() : null;
                    var pr = profile.Params.ContainsKey("PasswordRef") ? profile.Params["PasswordRef"]?.ToString() : null;
                    var u = overrides.ContainsKey(ur!) ? overrides[ur!] : await _secrets.GetAsync(ur!, ct);
                    var p = overrides.ContainsKey(pr!) ? overrides[pr!] : await _secrets.GetAsync(pr!, ct);
                    if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p)) return new TestAuthResult(false, "error", "Failed to resolve basic credentials", null, null, null, null, null, null);
                    var raw = Convert.ToBase64String(Encoding.UTF8.GetBytes(u + ":" + p));
                    var prefix = raw.Length >= 8 ? raw.Substring(0, 8) : raw;
                    return new TestAuthResult(true, "ok", null, prefix, null, null, null, new Dictionary<string, string> { { "Authorization", "Basic " + prefix } }, null);
                }

                if (type == AuthProfiles.Domain.AuthType.CustomLogin)
                {
                    if (profile.Params == null) return new TestAuthResult(false, "error", "Missing params", null, null, null, null, null, null);
                    var url = profile.Params.ContainsKey("CustomLoginUrl") ? profile.Params["CustomLoginUrl"]?.ToString() : null;
                    var userKey = profile.Params.ContainsKey("CustomUserKey") ? profile.Params["CustomUserKey"]?.ToString() : "username";
                    var passKey = profile.Params.ContainsKey("CustomPassKey") ? profile.Params["CustomPassKey"]?.ToString() : "password";
                    var bodyType = profile.Params.ContainsKey("CustomBodyType") ? profile.Params["CustomBodyType"]?.ToString() : "json";
                    var ur = profile.Params.ContainsKey("UsernameRef") ? profile.Params["UsernameRef"]?.ToString() : null;
                    var pr = profile.Params.ContainsKey("PasswordRef") ? profile.Params["PasswordRef"]?.ToString() : null;
                    var u = overrides.ContainsKey(ur!) ? overrides[ur!] : await _secrets.GetAsync(ur!, ct);
                    var p = overrides.ContainsKey(pr!) ? overrides[pr!] : await _secrets.GetAsync(pr!, ct);
                    var client = _httpFactory.CreateClient();
                    HttpResponseMessage resp;
                    if (bodyType == "form")
                    {
                        if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p)) return new TestAuthResult(false, "error", "Failed to resolve login credentials", null, null, null, null, null, null);
                        var form = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>(userKey!, u!), new KeyValuePair<string, string>(passKey!, p!) };
                        resp = await client.PostAsync(url, new FormUrlEncodedContent(form), ct).ConfigureAwait(false);
                    }
                    else
                    {
                        var obj = new Dictionary<string, string> { { userKey!, u! }, { passKey!, p! } };
                        var json = JsonSerializer.Serialize(obj);
                        resp = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(false);
                    }
                    var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("access_token", out var at))
                        {
                            var token = at.GetString();
                            var prefix = token?.Length >= 8 ? token.Substring(0, 8) : token;
                            return new TestAuthResult(true, "ok", null, prefix, null, null, null, new Dictionary<string, string> { { "Authorization", "Bearer " + prefix } }, null);
                        }
                    }
                    catch { }
                    return new TestAuthResult(false, "error", "Failed to parse token from login response", null, null, null, null, null, null);
                }

                return new TestAuthResult(false, "error", "Unsupported scheme for test", null, null, null, null, null, null);
            }
            catch (Exception ex)
            {
                return new TestAuthResult(false, "error", ex.Message, null, null, null, null, null, null);
            }
        }
    }
}
