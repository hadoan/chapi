using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Chapi.AI.Dto;

namespace Chapi.AI.Services.Auth
{
    internal record TokenCacheEntry(string Token, DateTimeOffset ExpiresAt);

    public class TokenGenerator : ITokenGenerator
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TokenGenerator> _logger;

        // Simple in-memory cache
        private readonly ConcurrentDictionary<string, TokenCacheEntry> _cache = new();

        public TokenGenerator(IHttpClientFactory httpClientFactory, ILogger<TokenGenerator> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string?> GetAccessTokenAsync(AuthProfile profile, CancellationToken ct = default)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var type = profile.Type ?? string.Empty;
            if (type is not ("OIDC_CLIENT_CREDENTIALS" or "OIDC_PASSWORD" or "CUSTOM_SCRIPT"))
            {
                _logger.LogDebug("No access token required for auth type {AuthType}", type);
                return null;
            }

            var key = BuildCacheKey(profile);
            if (_cache.TryGetValue(key, out var existing))
            {
                if (existing.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(60))
                {
                    _logger.LogDebug("Returning cached token for key {Key}, expires at {ExpiresAt}", key, existing.ExpiresAt);
                    return existing.Token;
                }
                else
                {
                    _logger.LogDebug("Cached token for key {Key} is expiring soon or expired", key);
                    _cache.TryRemove(key, out _);
                }
            }

            return type switch
            {
                "OIDC_CLIENT_CREDENTIALS" => await RequestClientCredentialsAsync(profile, key, ct).ConfigureAwait(false),
                "OIDC_PASSWORD" => await RequestPasswordGrantAsync(profile, key, ct).ConfigureAwait(false),
                "CUSTOM_SCRIPT" => await RunCustomScriptAsync(profile, key, ct).ConfigureAwait(false),
                _ => null
            };
        }

        private static string BuildCacheKey(AuthProfile profile)
        {
            var cfg = profile.Config ?? new Dictionary<string, string>();
            cfg.TryGetValue("token_url", out var tokenUrl);
            cfg.TryGetValue("TOKEN_URL", out var tokenUrl2);
            var url = tokenUrl ?? tokenUrl2 ?? string.Empty;

            cfg.TryGetValue("client_id", out var clientId);
            cfg.TryGetValue("CLIENT_ID", out var clientId2);
            var cid = clientId ?? clientId2 ?? string.Empty;

            cfg.TryGetValue("username", out var username);
            cfg.TryGetValue("USERNAME", out var username2);
            var user = username ?? username2 ?? string.Empty;

            cfg.TryGetValue("scope", out var scope);
            cfg.TryGetValue("audience", out var audience);

            return string.Join('|', profile.Type, url, cid, user, scope ?? string.Empty, audience ?? string.Empty);
        }

        private async Task<string?> RequestClientCredentialsAsync(AuthProfile profile, string cacheKey, CancellationToken ct)
        {
            var cfg = profile.Config ?? new Dictionary<string, string>();

            var tokenUrl = GetConfigValue(cfg, "TOKEN_URL", "token_url");
            var clientId = GetConfigValue(cfg, "CLIENT_ID", "client_id");
            var clientSecret = GetConfigValue(cfg, "CLIENT_SECRET", "client_secret");

            if (string.IsNullOrEmpty(tokenUrl) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                throw new InvalidOperationException("OIDC_CLIENT_CREDENTIALS requires TOKEN_URL, CLIENT_ID and CLIENT_SECRET secrets/config.");

            var scope = cfg.ContainsKey("scope") ? "scope" : (cfg.ContainsKey("SCOPE") ? "SCOPE" : null);
            var audience = cfg.ContainsKey("audience") ? "audience" : (cfg.ContainsKey("AUDIENCE") ? "AUDIENCE" : null);

            _logger.LogDebug("Requesting client_credentials token, optional fields: scope={HasScope}, audience={HasAudience}", scope != null, audience != null);

            var client = _httpClientFactory.CreateClient("auth");

            var form = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "client_credentials"),
                new("client_id", "{{CLIENT_ID}}"),
                new("client_secret", "{{CLIENT_SECRET}}")
            };

            if (scope != null) form.Add(new("scope", "{{SCOPE|api}}"));
            if (audience != null) form.Add(new("audience", "{{AUDIENCE|}}"));

            using var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
            {
                Content = new FormUrlEncodedContent(form)
            };
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage resp;
            try
            {
                resp = await client.SendAsync(req, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request for client credentials token failed");
                throw;
            }

            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Token endpoint returned status {Status}. Body (truncated): {Body}", (int)resp.StatusCode, Truncate(body, 1000));
                throw new InvalidOperationException($"Token endpoint returned {(int)resp.StatusCode}");
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("access_token", out var at))
                throw new InvalidOperationException("Token response did not contain access_token");

            var token = at.GetString()!;

            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var ei) && ei.TryGetInt32(out var secs) ? secs : 3600;
            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

            _cache[cacheKey] = new TokenCacheEntry(token, expiresAt);
            _logger.LogInformation("Acquired token via client_credentials, expires at {ExpiresAt}", expiresAt);

            return token;
        }

        private async Task<string?> RequestPasswordGrantAsync(AuthProfile profile, string cacheKey, CancellationToken ct)
        {
            var cfg = profile.Config ?? new Dictionary<string, string>();

            var tokenUrl = GetConfigValue(cfg, "TOKEN_URL", "token_url");
            var clientId = GetConfigValue(cfg, "CLIENT_ID", "client_id");
            var clientSecret = GetConfigValue(cfg, "CLIENT_SECRET", "client_secret");
            var username = GetConfigValue(cfg, "USERNAME", "username");
            var password = GetConfigValue(cfg, "PASSWORD", "password");

            if (string.IsNullOrEmpty(tokenUrl) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                throw new InvalidOperationException("OIDC_PASSWORD requires TOKEN_URL, CLIENT_ID, CLIENT_SECRET, USERNAME and PASSWORD secrets/config.");

            var cfgScope = cfg.ContainsKey("scope") || cfg.ContainsKey("SCOPE");
            _logger.LogDebug("Requesting password grant token, has scope: {HasScope}", cfgScope);

            var client = _httpClientFactory.CreateClient("auth");

            var form = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "password"),
                new("client_id", "{{CLIENT_ID}}"),
                new("client_secret", "{{CLIENT_SECRET}}"),
                new("username", "{{USERNAME}}"),
                new("password", "{{PASSWORD}}")
            };

            if (cfgScope) form.Add(new("scope", "{{SCOPE|}}"));

            using var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
            {
                Content = new FormUrlEncodedContent(form)
            };
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage resp;
            try
            {
                resp = await client.SendAsync(req, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request for password grant token failed");
                throw;
            }

            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Token endpoint returned status {Status}. Body (truncated): {Body}", (int)resp.StatusCode, Truncate(body, 1000));
                throw new InvalidOperationException($"Token endpoint returned {(int)resp.StatusCode}");
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("access_token", out var at))
                throw new InvalidOperationException("Token response did not contain access_token");

            var token = at.GetString()!;
            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var ei) && ei.TryGetInt32(out var secs) ? secs : 3600;
            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
            _cache[cacheKey] = new TokenCacheEntry(token, expiresAt);
            _logger.LogInformation("Acquired token via password grant, expires at {ExpiresAt}", expiresAt);

            return token;
        }

        private async Task<string?> RunCustomScriptAsync(AuthProfile profile, string cacheKey, CancellationToken ct)
        {
            var cfg = profile.Config ?? new Dictionary<string, string>();
            var scriptPath = GetConfigValue(cfg, "TOKEN_SCRIPT_PATH", "token_script_path");
            if (string.IsNullOrEmpty(scriptPath))
                throw new InvalidOperationException("CUSTOM_SCRIPT requires TOKEN_SCRIPT_PATH in config/secrets.");

            _logger.LogDebug("Running custom token script {ScriptPath}", scriptPath);

            var psi = new ProcessStartInfo
            {
                FileName = scriptPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi)!;
            if (proc == null)
                throw new InvalidOperationException("Failed to start token script");

            var stdout = await proc.StandardOutput.ReadLineAsync().ConfigureAwait(false);
            var stderr = await proc.StandardError.ReadToEndAsync().ConfigureAwait(false);
            await proc.WaitForExitAsync(ct).ConfigureAwait(false);

            if (proc.ExitCode != 0)
            {
                _logger.LogError("Token script {Script} exited {Code}. Stderr (truncated): {Stderr}", scriptPath, proc.ExitCode, Truncate(stderr, 1000));
                throw new InvalidOperationException($"Token script exited with code {proc.ExitCode}");
            }

            if (string.IsNullOrWhiteSpace(stdout))
            {
                throw new InvalidOperationException("Token script produced no output");
            }

            // Assume the script prints the token on the first line
            var token = stdout.Trim();
            // Cache for a small default time (5 minutes)
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
            _cache[cacheKey] = new TokenCacheEntry(token, expiresAt);
            _logger.LogInformation("Acquired token from custom script, expires at {ExpiresAt}", expiresAt);

            return token;
        }

        private static string GetConfigValue(IDictionary<string, string> cfg, string primary, string secondary)
        {
            if (cfg.TryGetValue(primary, out var v) && !string.IsNullOrEmpty(v)) return v;
            if (cfg.TryGetValue(secondary, out var v2) && !string.IsNullOrEmpty(v2)) return v2;
            return string.Empty;
        }

        private static string Truncate(string? s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }
    }
}
