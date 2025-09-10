using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using AuthProfiles.Application.Dtos;
using AuthProfiles.Application.Services;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace AuthProfiles.Infrastructure.Services;

public sealed class AuthDetectionService : IAuthDetectionService
{
    // e.g. /connect/token, /oauth/token, /auth/token, /token
    private static readonly Regex TokenPathRegex = new(
        @"(?i)(^|/)(connect|oauth|auth)(/|_)?token($|/)|(^|/)token($|/)|(^|/)access_token($|/)",
        RegexOptions.Compiled);

    public Task<IReadOnlyList<DetectionCandidateDto>> DetectAsync(DetectTokenRequest req, CancellationToken ct)
    {
        var candidates = new List<DetectionCandidateDto>();

        if (!string.IsNullOrWhiteSpace(req.OpenApiJson))
        {
            try
            {
                candidates.AddRange(DetectFromOpenApi(req.OpenApiJson!, req.BaseUrl));
            }
            catch
            {
                // swallow parse errors and continue with other sources
            }
        }

        if (!string.IsNullOrWhiteSpace(req.PostmanJson))
        {
            try
            {
                candidates.AddRange(DetectFromPostman(req.PostmanJson!));
            }
            catch
            {
                // swallow parse errors
            }
        }

        // TODO: If ProjectId/ServiceId provided, you can fetch spec from your ApiSpecs store.

        // Deduplicate by (Type, TokenUrl/Endpoint)
        var distinct = candidates
                .GroupBy(c => $"{c.Type}|{c.TokenUrl ?? c.Endpoint}|{c.Injection.Mode}|{c.Injection.Name}")
                .Select(g =>
                {
                    var best = g.OrderByDescending(x => x.Confidence).First();
                    // Merge confidence bumps if multiple sources confirmed the same candidate
                    var boost = Math.Min(0.1 * (g.Count() - 1), 0.1);
                    return best with { Confidence = Math.Min(best.Confidence + boost, 0.95) };
                })
                .OrderByDescending(c => c.Confidence)
                .ToList();
        return Task.FromResult((IReadOnlyList<DetectionCandidateDto>)distinct);
    }

    // Backwards-compatible wrapper for existing consumers that expect AuthDetectionCandidateDto
    public async Task<IReadOnlyList<AuthDetectionCandidateDto>> DetectAsync(Guid projectId, Guid serviceId, CancellationToken ct)
    {
        var req = new DetectTokenRequest(projectId, serviceId, null, null, null);
        var res = await DetectAsync(req, ct).ConfigureAwait(false);

        var list = res.Select(d => new AuthDetectionCandidateDto(
            AuthProfiles.Domain.AuthType.OAuth2ClientCredentials, // best-effort mapping
            d.TokenUrl,
            null,
            null,
            AuthProfiles.Domain.InjectionMode.Header,
            d.Injection.Name,
            d.Injection.Format,
            d.Source,
            d.Confidence)).ToList();

        return list;
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
                    var flows = new[] { scheme.Flows.ClientCredentials, scheme.Flows.Password, scheme.Flows.AuthorizationCode, scheme.Flows.Implicit }
                                .Where(f => f is not null).Cast<OpenApiOAuthFlow>();

                    foreach (var flow in flows)
                    {
                        var tokenUrl = flow.TokenUrl?.ToString();
                        if (!string.IsNullOrWhiteSpace(tokenUrl))
                        {
                            var absolute = AbsoluteUrl(tokenUrl!, baseUrl, serverBase);
                            yield return OAuthCandidate(absolute, "openapi", 0.85);
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
            yield return OAuthCandidate(absolute, "openapi", conf);
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
                    yield return OAuthCandidate(tokenUrl!, "postman", 0.85);
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
                    yield return OAuthCandidate(pathStr, "postman", 0.70);
                }
            }
        }
    }

    private static DetectionCandidateDto OAuthCandidate(string absoluteTokenUrl, string source, double confidence)
    {
        var endpoint = TryGetPath(absoluteTokenUrl);
        return new DetectionCandidateDto(
            "oauth2_client_credentials",
            endpoint,
            absoluteTokenUrl,
            new InjectionPreview("header", "Authorization", "Bearer {{access_token}}"),
            source,
            confidence
        );
    }

    private static string AbsoluteUrl(string urlOrPath, string? baseUrl, string? serverBase)
    {
        // Already absolute
        if (Uri.TryCreate(urlOrPath, UriKind.Absolute, out var abs)) return abs.ToString();

        var root = !string.IsNullOrWhiteSpace(serverBase) ? serverBase :
                   !string.IsNullOrWhiteSpace(baseUrl) ? baseUrl!.TrimEnd('/') : string.Empty;

        if (string.IsNullOrWhiteSpace(root)) return urlOrPath; // best-effort

        if (!urlOrPath.StartsWith("/")) urlOrPath = "/" + urlOrPath;
        return root + urlOrPath;
    }

    private static string TryGetPath(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.AbsolutePath : url;
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
