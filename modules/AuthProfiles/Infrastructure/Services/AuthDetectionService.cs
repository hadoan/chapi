using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using AuthProfiles.Application.Dtos;
using Chapi.ApiSpecs.Domain;
using AuthProfiles.Application.Services;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.Extensions.Logging;

namespace AuthProfiles.Infrastructure.Services;

public sealed class AuthDetectionService : IAuthDetectionService
{
    // e.g. /connect/token, /oauth/token, /auth/token, /token
    private static readonly Regex TokenPathRegex = new(
        @"(?i)(^|/)(connect|oauth|auth)(/|_)?token($|/)|(^|/)token($|/)|(^|/)access_token($|/)",
        RegexOptions.Compiled);

    private readonly Chapi.ApiSpecs.Domain.IApiSpecRepository _specRepo;
    private readonly Microsoft.Extensions.Logging.ILogger<AuthDetectionService> _logger;

    public AuthDetectionService(Chapi.ApiSpecs.Domain.IApiSpecRepository specRepo, Microsoft.Extensions.Logging.ILogger<AuthDetectionService> logger)
    {
        _specRepo = specRepo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DetectionCandidateDto>> DetectAsync(DetectTokenRequest req, CancellationToken ct)
    {
        var candidates = new List<DetectionCandidateDto>();

        // If a ProjectId is provided, fetch stored API specs (raw JSON) and run detectors on them
        if (req.ProjectId.HasValue)
        {
            try
            {
                var raws = await _specRepo.GetRawJsonByProjectAsync(req.ProjectId.Value, ct).ConfigureAwait(false);
                foreach (var raw in raws)
                {
                    try { candidates.AddRange(DetectFromOpenApi(raw, req.BaseUrl)); } catch { }
                    try { candidates.AddRange(DetectFromPostman(raw)); } catch { }
                }
            }
            catch (Exception ex)
            {
                // Log and continue — detection should not fail the whole request if specs are missing/corrupt
                _logger?.LogWarning(ex, "Failed to fetch ApiSpecs for project {ProjectId} while running auth detection", req.ProjectId);
            }
        }

        // No inline OpenApi/Postman JSONs are expected on the request anymore.

        // Deduplicate by (GrantType/Type, normalized token path/endpoint, injection)
        var distinct = candidates
            .GroupBy(c => $"{c.Form?.GrantType ?? c.Type}|{TryGetPath(c.TokenUrl ?? c.Endpoint).ToLowerInvariant()}|{c.Injection.Mode}|{c.Injection.Name}")
                    .Select(g =>
                    {
                        var best = g.OrderByDescending(x => x.Confidence).First();
                        // Merge confidence bumps if multiple sources confirmed the same candidate
                        var boost = Math.Min(0.1 * (g.Count() - 1), 0.1);
                        return best with { Confidence = Math.Min(best.Confidence + boost, 0.95) };
                    })
                    .OrderByDescending(c => c.Confidence)
                    .ToList();
        return distinct;
    }

  
    // Backwards-compatible wrapper for existing consumers that expect AuthDetectionCandidateDto
    public async Task<IReadOnlyList<AuthDetectionCandidateDto>> DetectAsync(Guid projectId, Guid serviceId, CancellationToken ct)
    {
        var req = new DetectTokenRequest(projectId, serviceId, null);
        var res = await DetectAsync(req, ct).ConfigureAwait(false);

        // Map internal detection candidates to the public DTO; use safe defaults for injection details
        var list = res.Select(d => new AuthDetectionCandidateDto(
            MapDetectedType(d.Type),
            d.TokenUrl,
            null,
            null,
            AuthProfiles.Domain.InjectionMode.Header,
            string.Empty,
            string.Empty,
            d.Source,
            d.Confidence,
            // Keep backward compatibility: Form may be null
            d.Form)).ToList();

        return list;
    }

    // Uses public DetectionCandidateDto from Application.Dtos

    private static AuthProfiles.Domain.AuthType MapDetectedType(string? detected)
    {
        if (string.IsNullOrWhiteSpace(detected)) return AuthProfiles.Domain.AuthType.OAuth2ClientCredentials;
        detected = detected.Trim().ToLowerInvariant();
        return detected switch
        {
            "oauth2_client_credentials" => AuthProfiles.Domain.AuthType.OAuth2ClientCredentials,
            "oauth2_client" => AuthProfiles.Domain.AuthType.OAuth2ClientCredentials,
            "client_credentials" => AuthProfiles.Domain.AuthType.OAuth2ClientCredentials,
            "oauth2_password" => AuthProfiles.Domain.AuthType.OAuth2Password,
            "password" => AuthProfiles.Domain.AuthType.OAuth2Password,
            "basic" => AuthProfiles.Domain.AuthType.Basic,
            "http_basic" => AuthProfiles.Domain.AuthType.Basic,
            "bearer_static" => AuthProfiles.Domain.AuthType.BearerStatic,
            "api_key_header" => AuthProfiles.Domain.AuthType.ApiKeyHeader,
            "session_cookie" => AuthProfiles.Domain.AuthType.SessionCookie,
            "custom_login" => AuthProfiles.Domain.AuthType.CustomLogin,
            _ => AuthProfiles.Domain.AuthType.OAuth2ClientCredentials,
        };
    }

    private static IEnumerable<DetectionCandidateDto> DetectFromOpenApi(string openApiJson, string? baseUrl)
    {
        var reader = new OpenApiStringReader();
        var doc = reader.Read(openApiJson, out var diag);
        // If servers list is present, prefer that for absolute TokenUrl
        var serverBase = doc.Servers?.FirstOrDefault()?.Url?.TrimEnd('/');

        // securitySchemes → strong signals
        if (doc.Components?.SecuritySchemes is not null)
        {
            foreach (var kv in doc.Components.SecuritySchemes)
            {
                var scheme = kv.Value;
                if (scheme.Type == SecuritySchemeType.OAuth2 && scheme.Flows is not null)
                {
                    // handle each flow explicitly so password can emit full form hints
                    // ClientCredentials flow
                    if (scheme.Flows.ClientCredentials is not null)
                    {
                        var tokenUrl = scheme.Flows.ClientCredentials.TokenUrl?.ToString();
                        if (!string.IsNullOrWhiteSpace(tokenUrl))
                        {
                            var absolute = AbsoluteUrl(tokenUrl!, baseUrl, serverBase);
                            var form = new TokenFormHints("client_credentials", null);
                            yield return OAuthCandidate(absolute, "openapi", 0.85, form);
                        }
                    }

                    // Password flow
                    if (scheme.Flows.Password is not null)
                    {
                        var tokenUrl = scheme.Flows.Password.TokenUrl?.ToString();
                        if (!string.IsNullOrWhiteSpace(tokenUrl))
                        {
                            var absolute = AbsoluteUrl(tokenUrl!, baseUrl, serverBase);
                            var passwordFields = new Dictionary<string, string?>
                            {
                                ["grant_type"] = "password",
                                ["username"] = "{{USERNAME}}",
                                ["password"] = "{{PASSWORD}}",
                                ["client_id"] = "{{CLIENT_ID}}",
                                ["client_secret"] = "{{CLIENT_SECRET}}",
                                ["scope"] = "{{SCOPE|api}}"
                            };
                            yield return OAuthCandidate(absolute, "openapi", 0.85, new TokenFormHints("password", passwordFields));
                        }
                    }

                    // AuthorizationCode / Implicit - treat as client_credentials signal for detection purposes
                    if (scheme.Flows.AuthorizationCode is not null)
                    {
                        var tokenUrl = scheme.Flows.AuthorizationCode.TokenUrl?.ToString();
                        if (!string.IsNullOrWhiteSpace(tokenUrl))
                        {
                            var absolute = AbsoluteUrl(tokenUrl!, baseUrl, serverBase);
                            yield return OAuthCandidate(absolute, "openapi", 0.75, new TokenFormHints("client_credentials", null));
                        }
                    }

                    if (scheme.Flows.Implicit is not null)
                    {
                        var tokenUrl = scheme.Flows.Implicit.TokenUrl?.ToString();
                        if (!string.IsNullOrWhiteSpace(tokenUrl))
                        {
                            var absolute = AbsoluteUrl(tokenUrl!, baseUrl, serverBase);
                            yield return OAuthCandidate(absolute, "openapi", 0.65, new TokenFormHints("client_credentials", null));
                        }
                    }
                }
                else if (scheme.Type == SecuritySchemeType.ApiKey && scheme.In == ParameterLocation.Header)
                {
                    yield return new DetectionCandidateDto(
                        "api_key_header",
                        "",
                        null,
                        new InjectionPreview("header", scheme.Name ?? "X-API-Key", "{{API_KEY}}"),
                        "openapi",
                        0.60
                    );
                }
                else if (scheme.Type == SecuritySchemeType.Http && string.Equals(scheme.Scheme, "bearer", StringComparison.OrdinalIgnoreCase))
                {
                    yield return new DetectionCandidateDto(
                        "bearer_static",
                        "",
                        null,
                        new InjectionPreview("header", "Authorization", "Bearer {{access_token}}"),
                        "openapi",
                        0.40
                    );
                }
            }
        }

        // Paths with token-like names → medium signal
        foreach (var p in doc.Paths)
        {
            var path = p.Key; // e.g. "/connect/token"
            if (!TokenPathRegex.IsMatch(path)) continue;

            // Prefer POST
            var hasPost = p.Value.Operations.ContainsKey(OperationType.Post);
            var conf = hasPost ? 0.65 : 0.55;

            var absolute = AbsoluteUrl(path, baseUrl, serverBase);
            // Emit a strong client_credentials candidate and a lower-confidence password candidate
            yield return OAuthCandidate(absolute, "openapi", conf, new TokenFormHints("client_credentials", null));

            var passwordFields = new Dictionary<string, string?>
            {
                ["grant_type"] = "password",
                ["username"] = "{{USERNAME}}",
                ["password"] = "{{PASSWORD}}",
                ["client_id"] = "{{CLIENT_ID}}",
                ["client_secret"] = "{{CLIENT_SECRET}}",
                ["scope"] = "{{SCOPE|api}}"
            };

            yield return OAuthCandidate(absolute, "openapi", Math.Min(conf - 0.1, 0.6), new TokenFormHints("password", passwordFields));
        }
    }

    private static IEnumerable<DetectionCandidateDto> DetectFromPostman(string postmanJson)
    {
        using var doc = JsonDocument.Parse(postmanJson);

        // 1) Collection-level auth block
        if (doc.RootElement.TryGetProperty("auth", out var auth))
        {
            var type = auth.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (string.Equals(type, "oauth2", StringComparison.OrdinalIgnoreCase))
            {
                var tokenUrl = FindKeyValue(auth, "tokenUrl");
                if (!string.IsNullOrWhiteSpace(tokenUrl))
                    yield return OAuthCandidate(tokenUrl!, "postman", 0.85, new TokenFormHints("client_credentials", null));
            }
            else if (string.Equals(type, "apikey", StringComparison.OrdinalIgnoreCase))
            {
                var name = FindKeyValue(auth, "key") ?? "X-API-Key";
                yield return new DetectionCandidateDto(
                    "api_key_header",
                    "",
                    null,
                    new InjectionPreview("header", name, "{{API_KEY}}"),
                    "postman",
                    0.65
                );
            }
            else if (string.Equals(type, "bearer", StringComparison.OrdinalIgnoreCase))
            {
                yield return new DetectionCandidateDto(
                    "bearer_static",
                    "",
                    null,
                    new InjectionPreview("header", "Authorization", "Bearer {{access_token}}"),
                    "postman",
                    0.45
                );
            }
        }

        // 2) Look for requests posting with grant_type (common pre-req flow)
        foreach (var req in EnumerateRequests(doc.RootElement))
        {
            if (req.TryGetProperty("url", out var urlEl))
            {
                var raw = urlEl.TryGetProperty("raw", out var rawEl) ? rawEl.GetString() : null;
                var pathStr = raw ?? urlEl.ToString();
                if (pathStr is not null && TokenPathRegex.IsMatch(pathStr))
                {
                    // Try to inspect the request body for grant_type
                    var grant = TryGetGrantTypeFromRequest(req);
                    if (!string.IsNullOrWhiteSpace(grant) && string.Equals(grant, "password", StringComparison.OrdinalIgnoreCase))
                    {
                        var passwordFields = new Dictionary<string, string?>
                        {
                            ["grant_type"] = "password",
                            ["username"] = "{{USERNAME}}",
                            ["password"] = "{{PASSWORD}}",
                            ["client_id"] = "{{CLIENT_ID}}",
                            ["client_secret"] = "{{CLIENT_SECRET}}",
                            ["scope"] = "{{SCOPE|api}}"
                        };

                        yield return OAuthCandidate(pathStr, "postman", 0.75, new TokenFormHints("password", passwordFields));
                    }
                    else if (!string.IsNullOrWhiteSpace(grant) && string.Equals(grant, "client_credentials", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return OAuthCandidate(pathStr, "postman", 0.8, new TokenFormHints("client_credentials", null));
                    }
                    else
                    {
                        // Fallback: emit both, password weaker
                        yield return OAuthCandidate(pathStr, "postman", 0.70, new TokenFormHints("client_credentials", null));

                        var passwordFields = new Dictionary<string, string?>
                        {
                            ["grant_type"] = "password",
                            ["username"] = "{{USERNAME}}",
                            ["password"] = "{{PASSWORD}}",
                            ["client_id"] = "{{CLIENT_ID}}",
                            ["client_secret"] = "{{CLIENT_SECRET}}",
                            ["scope"] = "{{SCOPE|api}}"
                        };

                        yield return OAuthCandidate(pathStr, "postman", 0.60, new TokenFormHints("password", passwordFields));
                    }
                }
            }
        }
    }

    private static DetectionCandidateDto OAuthCandidate(string absoluteTokenUrl, string source, double confidence, TokenFormHints? form)
    {
        // Normalize repeated token segments and extract a clean endpoint path
        var collapsed = CollapseRepeatedTokenPath(absoluteTokenUrl);
        var endpoint = TryGetPath(collapsed);
        var grant = form?.GrantType ?? "oauth2_client_credentials";
        return new DetectionCandidateDto(
            grant == "password" ? "oauth2_password" : "oauth2_client_credentials",
            endpoint,
            collapsed,
            new InjectionPreview("header", "Authorization", "Bearer {{access_token}}"),
            source,
            confidence,
            form
        );
    }

    // Collapse repeated segments like /connect/token/connect/token into single /connect/token
    private static string CollapseRepeatedTokenPath(string url)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return url;
            var segments = u.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0) return url;

            // Find the token-related segment index (first segment containing token keywords)
            var idx = Array.FindIndex(segments, s => TokenPathRegex.IsMatch("/" + s));
            if (idx <= 0) return url; // nothing to collapse

            // Keep the first occurrence and drop subsequent repeated runs of the same tail
            var tail = string.Join('/', segments.Skip(idx));
            var newSegments = segments.Take(idx).Append(segments[idx]).ToArray();
            var baseUri = u.GetLeftPart(UriPartial.Authority);
            return baseUri + "/" + string.Join('/', newSegments);
        }
        catch
        {
            return url;
        }
    }

    private static string AbsoluteUrl(string urlOrPath, string? baseUrl, string? serverBase)
    {
        // Already absolute - but check if it's a file:// URL that should be combined with baseUrl
        if (Uri.TryCreate(urlOrPath, UriKind.Absolute, out var abs))
        {
            // If it's a file:// URL and we have a proper baseUrl, combine them
            if (abs.Scheme == "file" && !string.IsNullOrWhiteSpace(baseUrl))
            {
                // Extract the path from the file:// URL and combine with baseUrl
                var path = abs.AbsolutePath;
                var baseRoot = !string.IsNullOrWhiteSpace(serverBase) ? serverBase :
                               !string.IsNullOrWhiteSpace(baseUrl) ? baseUrl!.TrimEnd('/') : string.Empty;

                if (!string.IsNullOrWhiteSpace(baseRoot))
                {
                    if (!path.StartsWith("/")) path = "/" + path;
                    var combinedUrl = baseRoot + path;
                    return CollapseRepeatedTokenPath(combinedUrl);
                }
            }
            return abs.ToString();
        }

        var root = !string.IsNullOrWhiteSpace(serverBase) ? serverBase :
                   !string.IsNullOrWhiteSpace(baseUrl) ? baseUrl!.TrimEnd('/') : string.Empty;

        if (string.IsNullOrWhiteSpace(root)) return urlOrPath; // best-effort

        if (!urlOrPath.StartsWith("/")) urlOrPath = "/" + urlOrPath;
        var combined = root + urlOrPath;
        // Collapse repeated token segments before returning
        return CollapseRepeatedTokenPath(combined);
    }

    private static string TryGetPath(string url)
    {
        // Try to parse and normalize; if parsing fails, return input
        if (Uri.TryCreate(url, UriKind.Absolute, out var u))
        {
            // Remove duplicate token segments in path
            var collapsed = CollapseRepeatedTokenPath(u.ToString());
            if (Uri.TryCreate(collapsed, UriKind.Absolute, out var cu)) return cu.AbsolutePath;
            return u.AbsolutePath;
        }
        return url;
    }

    private static string? TryGetGrantTypeFromRequest(JsonElement req)
    {
        try
        {
            if (req.TryGetProperty("body", out var body) && body.ValueKind == JsonValueKind.Object)
            {
                // Postman request body may contain mode/formdata/urlencoded/raw etc.
                if (body.TryGetProperty("urlencoded", out var urlencoded) && urlencoded.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in urlencoded.EnumerateArray())
                    {
                        if (item.TryGetProperty("key", out var k) && string.Equals(k.GetString(), "grant_type", StringComparison.OrdinalIgnoreCase)
                            && item.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.String)
                            return v.GetString();
                    }
                }

                // Fallback: raw JSON body
                if (body.TryGetProperty("raw", out var raw) && raw.ValueKind == JsonValueKind.String)
                {
                    var rawStr = raw.GetString();
                    try
                    {
                        using var jd = JsonDocument.Parse(rawStr ?? string.Empty);
                        if (jd.RootElement.TryGetProperty("grant_type", out var gt) && gt.ValueKind == JsonValueKind.String) return gt.GetString();
                    }
                    catch { }
                }
            }
        }
        catch { }
        return null;
    }

    private static string? FindKeyValue(JsonElement el, string key)
    {
        // Postman stores params under "params" or "oauth2" arrays sometimes
        string? val = null;
        if (el.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in el.EnumerateObject())
            {
                if (string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.String)
                    return p.Value.GetString();

                if (p.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                {
                    val ??= FindKeyValue(p.Value, key);
                }
            }
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
            {
                val ??= FindKeyValue(item, key);
                if (val is not null) break;
            }
        }
        return val;
    }

    private static IEnumerable<JsonElement> EnumerateRequests(JsonElement root)
    {
        if (root.TryGetProperty("item", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var it in items.EnumerateArray())
            {
                if (it.TryGetProperty("request", out var req))
                    yield return req;

                // Folders
                foreach (var nested in EnumerateRequests(it))
                    yield return nested;
            }
        }
        yield break;
    }
}
