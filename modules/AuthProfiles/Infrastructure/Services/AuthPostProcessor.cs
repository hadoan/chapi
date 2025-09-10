using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AuthProfiles.Application.Dtos;

namespace AuthProfiles.Infrastructure.Services;

public static class AuthPostProcessor
{
    public sealed record Input(
        DetectionCandidateDto Detection,
        string? BaseUrl,
        string Prefer,
        string? DefaultScope,
        IReadOnlyDictionary<string, string>? EnvNames
    );

    public sealed record Output(
        object AuthProfile,
        IReadOnlyList<string> EnvExample,
        object TestsJson,
        string Reason
    );

    private static string NormalizeUrl(string urlOrPath, string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(urlOrPath)) return urlOrPath ?? string.Empty;
        // Already absolute?
        if (Uri.TryCreate(urlOrPath, UriKind.Absolute, out var abs))
            return CollapseRepeatedTokenPath(abs.ToString());

        if (string.IsNullOrWhiteSpace(baseUrl)) return CollapseRepeatedTokenPath(urlOrPath);
        var root = baseUrl.TrimEnd('/');
        if (!urlOrPath.StartsWith('/')) urlOrPath = "/" + urlOrPath;
        return CollapseRepeatedTokenPath(root + urlOrPath);
    }

    private static string CollapseRepeatedTokenPath(string url)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return url;
            var segments = u.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0) return url;
            var idx = Array.FindIndex(segments, s => s.IndexOf("token", StringComparison.OrdinalIgnoreCase) >= 0);
            if (idx <= 0) return url;
            // keep up to first token segment and drop repeated tail
            var newSegments = segments.Take(idx + 1);
            var baseUri = u.GetLeftPart(UriPartial.Authority);
            return baseUri + "/" + string.Join('/', newSegments);
        }
        catch
        {
            return url;
        }
    }

    public static Output Normalize(Input input)
    {
        var det = input.Detection ?? throw new ArgumentNullException(nameof(input.Detection));
        var env = input.EnvNames ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CLIENT_ID"] = "CLIENT_ID",
            ["CLIENT_SECRET"] = "CLIENT_SECRET",
            ["SCOPE"] = "SCOPE",
            ["AUDIENCE"] = "AUDIENCE",
            ["USERNAME"] = "USERNAME",
            ["PASSWORD"] = "PASSWORD",
            ["API_KEY"] = "API_KEY",
            ["API_KEY_HEADER"] = "X-API-KEY"
        };

        // pick candidate (single one only) - since input gives one detection, treat it as chosen
        var cand = det;
        var reason = "Selected candidate by post-processor.";

        // Map types
        var type = cand.Type?.ToLowerInvariant() ?? "";
        object authProfileConfig = new Dictionary<string, object>();
        var envExample = new List<string> { "BASE_URL" };

        if (type.Contains("oauth2") || type.Contains("oauth"))
        {
            // determine grant
            var grant = cand.Form?.GrantType?.ToLowerInvariant() ?? (type.Contains("password") ? "password" : "client_credentials");
            var tokenUrl = NormalizeUrl(cand.TokenUrl ?? cand.Endpoint ?? string.Empty, input.BaseUrl);
            var scope = input.DefaultScope ?? "api";

            if (grant == "password")
            {
                var fields = new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["username"] = $"{{{{{env.GetValueOrDefault("USERNAME")}}}}}",
                    ["password"] = $"{{{{{env.GetValueOrDefault("PASSWORD")}}}}}",
                    ["client_id"] = $"{{{{{env.GetValueOrDefault("CLIENT_ID")}}}}}",
                    ["client_secret"] = $"{{{{{env.GetValueOrDefault("CLIENT_SECRET")}}}}}",
                    ["scope"] = $"{{{{{env.GetValueOrDefault("SCOPE")}}}}}"
                };

                authProfileConfig = new Dictionary<string, object>
                {
                    ["tokenUrl"] = tokenUrl,
                    ["clientIdEnv"] = $"{{{{{env.GetValueOrDefault("CLIENT_ID")}}}}}",
                    ["clientSecretEnv"] = $"{{{{{env.GetValueOrDefault("CLIENT_SECRET")}}}}}",
                    ["scope"] = $"{{{{{env.GetValueOrDefault("SCOPE")}}}}}",
                    ["audience"] = $"{{{{{env.GetValueOrDefault("AUDIENCE")}}}}}",
                    ["usernameEnv"] = $"{{{{{env.GetValueOrDefault("USERNAME")}}}}}",
                    ["passwordEnv"] = $"{{{{{env.GetValueOrDefault("PASSWORD")}}}}}"
                };

                envExample.Add(env.GetValueOrDefault("CLIENT_ID"));
                envExample.Add(env.GetValueOrDefault("CLIENT_SECRET"));
                envExample.Add(env.GetValueOrDefault("SCOPE"));
                envExample.Add(env.GetValueOrDefault("USERNAME"));
                envExample.Add(env.GetValueOrDefault("PASSWORD"));

                var tests = BuildOauthTests(tokenUrl, fields, input.BaseUrl);
                var authProfile = new Dictionary<string, object>
                {
                    ["id"] = Guid.NewGuid().ToString(),
                    ["name"] = $"OIDC_PASSWORD @ {new Uri(tokenUrl).Host}",
                    ["type"] = "OIDC_PASSWORD",
                    ["config"] = authProfileConfig
                };

                return new Output(authProfile, envExample.Distinct().ToList(), tests, reason);
            }
            else
            {
                // client_credentials
                var fields = new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = $"{{{{{env.GetValueOrDefault("CLIENT_ID")}}}}}",
                    ["client_secret"] = $"{{{{{env.GetValueOrDefault("CLIENT_SECRET")}}}}}",
                    ["scope"] = $"{{{{{env.GetValueOrDefault("SCOPE")}}}}}",
                    ["audience"] = $"{{{{{env.GetValueOrDefault("AUDIENCE")}}}}}"
                };

                authProfileConfig = new Dictionary<string, object>
                {
                    ["tokenUrl"] = tokenUrl,
                    ["clientIdEnv"] = $"{{{{{env.GetValueOrDefault("CLIENT_ID")}}}}}",
                    ["clientSecretEnv"] = $"{{{{{env.GetValueOrDefault("CLIENT_SECRET")}}}}}",
                    ["scope"] = $"{{{{{env.GetValueOrDefault("SCOPE")}}}}}",
                    ["audience"] = $"{{{{{env.GetValueOrDefault("AUDIENCE")}}}}}"
                };

                envExample.Add(env.GetValueOrDefault("CLIENT_ID"));
                envExample.Add(env.GetValueOrDefault("CLIENT_SECRET"));
                envExample.Add(env.GetValueOrDefault("SCOPE"));

                var tests = BuildOauthTests(tokenUrl, fields, input.BaseUrl);
                var authProfile = new Dictionary<string, object>
                {
                    ["id"] = Guid.NewGuid().ToString(),
                    ["name"] = $"OIDC_CLIENT_CREDENTIALS @ {new Uri(tokenUrl).Host}",
                    ["type"] = "OIDC_CLIENT_CREDENTIALS",
                    ["config"] = authProfileConfig
                };

                return new Output(authProfile, envExample.Distinct().ToList(), tests, reason);
            }
        }

        // API key / bearer / static
        if (type.Contains("api_key") || type.Contains("api-key") || type.Contains("api_key_header"))
        {
            var headerName = cand.Injection?.Name;
            var chosenHeader = string.IsNullOrWhiteSpace(headerName) || string.Equals(headerName, "Authorization", StringComparison.OrdinalIgnoreCase)
                ? env.GetValueOrDefault("API_KEY_HEADER")
                : headerName;

            authProfileConfig = new Dictionary<string, object>
            {
                ["headerName"] = $"{{{{{chosenHeader}}}}}",
                ["injectAt"] = "header"
            };
            envExample.Add(env.GetValueOrDefault("API_KEY"));
            envExample.Add(chosenHeader);

            var tests = BuildApiKeyTests(input.BaseUrl, cand.Endpoint ?? "/api/ping", chosenHeader);
            var authProfile = new Dictionary<string, object>
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["name"] = $"API_KEY @ {input.BaseUrl ?? (cand.TokenUrl ?? cand.Endpoint ?? "")}",
                ["type"] = "API_KEY",
                ["config"] = authProfileConfig
            };

            var reason2 = string.IsNullOrWhiteSpace(headerName) || string.Equals(headerName, "Authorization", StringComparison.OrdinalIgnoreCase)
                ? "API key candidate used; header name normalized to env var and Authorization was avoided." : "API key candidate used.";

            return new Output(authProfile, envExample.Distinct().ToList(), tests, reason2);
        }

        // Fallback: return a bearer/static profile
        var bearerTests = BuildBearerTests(input.BaseUrl, cand.Endpoint ?? "/api/ping");
        var bearerProfile = new Dictionary<string, object>
        {
            ["id"] = Guid.NewGuid().ToString(),
            ["name"] = $"BEARER @ {input.BaseUrl ?? (cand.TokenUrl ?? cand.Endpoint ?? "")}",
            ["type"] = "BEARER",
            ["config"] = new Dictionary<string, object>
            {
                ["headerName"] = cand.Injection?.Name ?? "Authorization",
                ["injectAt"] = "header"
            }
        };

        envExample.Add("API_KEY");
        return new Output(bearerProfile, envExample.Distinct().ToList(), bearerTests, reason);
    }

    private static object BuildOauthTests(string tokenUrl, IDictionary<string, string> formFields, string? baseUrl)
    {
        // chapi-ir/0.1 minimal
        var steps = new List<object>();

        steps.Add(new Dictionary<string, object>
        {
            ["name"] = "token",
            ["type"] = "http",
            ["method"] = "POST",
            ["url"] = tokenUrl,
            ["headers"] = new Dictionary<string, string> { { "Content-Type", "application/x-www-form-urlencoded" } },
            ["form"] = formFields,
            ["save"] = new Dictionary<string, string> { ["access_token"] = "$.access_token" }
        });

        var health = (baseUrl ?? "{{BASE_URL}}") + "/api/ping";
        steps.Add(new Dictionary<string, object>
        {
            ["name"] = "health",
            ["type"] = "http",
            ["method"] = "GET",
            ["url"] = health,
            ["headers"] = new Dictionary<string, string> { { "Authorization", "Bearer {{access_token}}" } },
            ["assertStatus"] = 200
        });

        steps.Add(new Dictionary<string, object>
        {
            ["name"] = "health_unauth",
            ["type"] = "http",
            ["method"] = "GET",
            ["url"] = health,
            ["assertStatus"] = 401
        });

        return new Dictionary<string, object>
        {
            ["version"] = "chapi-ir/0.1",
            ["steps"] = steps
        };
    }

    private static object BuildApiKeyTests(string? baseUrl, string endpoint, string headerName)
    {
        var baseu = baseUrl ?? "{{BASE_URL}}";
        var url = baseu.TrimEnd('/') + (endpoint.StartsWith("/") ? endpoint : "/" + endpoint);
        var steps = new List<object>
        {
            new Dictionary<string, object>
            {
                ["name"] = "health",
                ["type"] = "http",
                ["method"] = "GET",
                ["url"] = url,
                ["headers"] = new Dictionary<string, string> { { headerName, "{{API_KEY}}" } },
                ["assertStatus"] = 200
            },
            new Dictionary<string, object>
            {
                ["name"] = "health_unauth",
                ["type"] = "http",
                ["method"] = "GET",
                ["url"] = url,
                ["assertStatus"] = 401
            }
        };

        return new Dictionary<string, object>
        {
            ["version"] = "chapi-ir/0.1",
            ["steps"] = steps
        };
    }

    private static object BuildBearerTests(string? baseUrl, string endpoint)
    {
        var baseu = baseUrl ?? "{{BASE_URL}}";
        var url = baseu.TrimEnd('/') + (endpoint.StartsWith("/") ? endpoint : "/" + endpoint);
        var steps = new List<object>
        {
            new Dictionary<string, object>
            {
                ["name"] = "health",
                ["type"] = "http",
                ["method"] = "GET",
                ["url"] = url,
                ["headers"] = new Dictionary<string, string> { { "Authorization", "Bearer {{access_token}}" } },
                ["assertStatus"] = 200
            }
        };

        return new Dictionary<string, object>
        {
            ["version"] = "chapi-ir/0.1",
            ["steps"] = steps
        };
    }
}
