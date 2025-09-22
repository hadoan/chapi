using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Chapi.AI.Dto;
using Chapi.AI.Services.Auth;

namespace Chapi.AI.Services
{
    public interface IChapiIRGenerator
    {
        string GenerateTestsJsonContent(SelectedEndpoint endpoint, AuthProfile authProfile, TestGenOptions options);
    }

    public class ChapiIRGenerator : IChapiIRGenerator
    {
        private readonly ILogger<ChapiIRGenerator> _logger;
        private readonly ITokenGenerator _tokenGenerator;

        public ChapiIRGenerator(ILogger<ChapiIRGenerator> logger, ITokenGenerator tokenGenerator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenGenerator = tokenGenerator ?? throw new ArgumentNullException(nameof(tokenGenerator));
        }

        public string GenerateTestsJsonContent(SelectedEndpoint endpoint, AuthProfile authProfile, TestGenOptions options)
        {
            var suite = new
            {
                version = "chapi-ir/0.1",
                suite = new
                {
                    name = $"{endpoint.Method.ToLower()}-{SanitizePath(endpoint.Path)}",
                    env = new { BASE_URL = "{{BASE_URL}}" },
                    secrets = GetRequiredSecrets(authProfile, options?.ForbiddenAuthProfile),
                    auth = GenerateAuthConfig(authProfile),
                    steps = GenerateTestSteps(endpoint, authProfile, options)
                }
            };

            return JsonSerializer.Serialize(suite, new JsonSerializerOptions { WriteIndented = true });
        }

        private string[] GetRequiredSecrets(AuthProfile authProfile, AuthProfile? forbidden = null)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "BASE_URL" };

            void add(AuthProfile p)
            {
                switch (p.Type)
                {
                    case "API_KEY":
                        set.Add("API_KEY"); break;
                    case "BASIC":
                        set.Add("BASIC_USER"); set.Add("BASIC_PASS"); break;
                    case "BEARER":
                        set.Add("API_TOKEN"); break;
                    case "OIDC_CLIENT_CREDENTIALS":
                        set.Add("TOKEN_URL"); set.Add("CLIENT_ID"); set.Add("CLIENT_SECRET");
                        if (p.Config.ContainsKey("scope") || p.Config.ContainsKey("SCOPE")) set.Add("SCOPE");
                        if (p.Config.ContainsKey("audience") || p.Config.ContainsKey("AUDIENCE")) set.Add("AUDIENCE");
                        break;
                    case "OIDC_PASSWORD":
                        set.Add("TOKEN_URL"); set.Add("CLIENT_ID"); set.Add("CLIENT_SECRET");
                        set.Add("USERNAME"); set.Add("PASSWORD");
                        if (p.Config.ContainsKey("scope") || p.Config.ContainsKey("SCOPE")) set.Add("SCOPE");
                        break;
                    case "CUSTOM_SCRIPT":
                        set.Add("TOKEN_SCRIPT_PATH"); break;
                }
            }

            if (authProfile != null) add(authProfile);
            if (forbidden != null) add(forbidden);

            return set.ToArray();
        }

        private object GenerateAuthConfig(AuthProfile authProfile, string? tokenStepId = null)
        {
            switch (authProfile.Type)
            {
                case "NONE":
                    return new { strategy = "none" };

                case "API_KEY":
                    var injectAt = authProfile.Config.GetValueOrDefault("injectAt", "header");
                    if (injectAt == "header")
                    {
                        var headerName = authProfile.Config.GetValueOrDefault("headerName", "Authorization");
                        return new
                        {
                            strategy = "api_key",
                            inject = new { header = headerName, format = "{{API_KEY}}" }
                        };
                    }
                    else
                    {
                        return new { strategy = "api_key" }; // Query params handled in URL
                    }

                case "BASIC":
                    return new { strategy = "basic" };

                case "BEARER":
                    return new
                    {
                        strategy = "bearer",
                        inject = new { header = "Authorization", format = "Bearer {{API_TOKEN}}" }
                    };

                case "OIDC_CLIENT_CREDENTIALS":
                    return new
                    {
                        strategy = "client_credentials",
                        token_step = tokenStepId ?? "get_token",
                        inject = new { header = "Authorization", format = "Bearer {access_token}" }
                    };

                case "OIDC_PASSWORD":
                    return new
                    {
                        strategy = "password",
                        token_step = tokenStepId ?? "get_token",
                        inject = new { header = "Authorization", format = "Bearer {access_token}" }
                    };

                case "CUSTOM_SCRIPT":
                    return new
                    {
                        strategy = "custom",
                        token_step = tokenStepId ?? "get_token",
                        inject = new { header = "Authorization", format = "Bearer {access_token}" }
                    };

                default:
                    return new { strategy = "none" };
            }
        }

        private object[] GenerateTestSteps(SelectedEndpoint endpoint, AuthProfile authProfile, TestGenOptions? options)
        {
            var steps = new List<object>();

            // Add token step if needed
            if (authProfile.Type == "OIDC_CLIENT_CREDENTIALS" || authProfile.Type == "OIDC_PASSWORD" || authProfile.Type == "CUSTOM_SCRIPT")
            {
                steps.Add(GenerateTokenStep(authProfile, forForbidden: false));
            }

            // If a forbidden alternate profile is provided and it needs tokens, emit its token step too
            if (options?.ForbiddenAuthProfile != null)
            {
                var f = options.ForbiddenAuthProfile;
                if (f.Type == "OIDC_CLIENT_CREDENTIALS" || f.Type == "OIDC_PASSWORD" || f.Type == "CUSTOM_SCRIPT")
                {
                    steps.Add(GenerateTokenStep(f, forForbidden: true));
                    // Attempt to prefetch token via ITokenGenerator for realism (do not inline)
                    try
                    {
                        var tok = _tokenGenerator.GetAccessTokenAsync(f).ConfigureAwait(false).GetAwaiter().GetResult();
                        if (tok != null)
                        {
                            _logger.LogInformation("Prefetched forbidden profile token (not inlined) for profile {ProfileId}", f.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to prefetch token for forbidden profile {ProfileId}", f.Id);
                    }
                }
            }

            // Add happy path step
            steps.Add(GenerateHappyPathStep(endpoint, authProfile, options));

            // Add unauthorized step if endpoint requires auth
            if (endpoint.RequiresAuth)
            {
                steps.Add(GenerateUnauthorizedStep(endpoint));
            }

            // Add forbidden step if requested and applicable
            if (options != null && options.IncludeForbidden && endpoint.RequiresAuth)
            {
                steps.Add(GenerateForbiddenStep(endpoint, options));
            }

            return steps.ToArray();
        }

        private string GenerateTokenStepId(AuthProfile profile, bool forForbidden)
        {
            return forForbidden ? "get_token_forbidden" : "get_token";
        }

        private object GenerateTokenStep(AuthProfile authProfile, bool forForbidden)
        {
            var id = GenerateTokenStepId(authProfile, forForbidden);
            switch (authProfile.Type)
            {
                case "OIDC_CLIENT_CREDENTIALS":
                    var form = new Dictionary<string, object>
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = "{{CLIENT_ID}}",
                        ["client_secret"] = "{{CLIENT_SECRET}}"
                    };

                    if (authProfile.Config.ContainsKey("scope") || authProfile.Config.ContainsKey("SCOPE"))
                        form["scope"] = "{{SCOPE|api}}";
                    if (authProfile.Config.ContainsKey("audience") || authProfile.Config.ContainsKey("AUDIENCE"))
                        form["audience"] = "{{AUDIENCE|}}";

                    return new
                    {
                        id,
                        type = "http",
                        request = new
                        {
                            method = "POST",
                            url = "{{TOKEN_URL}}",
                            headers = new Dictionary<string, string> { ["Content-Type"] = "application/x-www-form-urlencoded" },
                            form,
                            timeout_ms = 15000
                        },
                        save = new { access_token = new { from = "body", jsonpath = "$.access_token" } },
                        assert = new { status = 200 }
                    };

                case "OIDC_PASSWORD":
                    var passwordForm = new Dictionary<string, object>
                    {
                        ["grant_type"] = "password",
                        ["client_id"] = "{{CLIENT_ID}}",
                        ["client_secret"] = "{{CLIENT_SECRET}}",
                        ["username"] = "{{USERNAME}}",
                        ["password"] = "{{PASSWORD}}"
                    };

                    if (authProfile.Config.ContainsKey("scope") || authProfile.Config.ContainsKey("SCOPE"))
                        passwordForm["scope"] = "{{SCOPE|}}";

                    return new
                    {
                        id,
                        type = "http",
                        request = new
                        {
                            method = "POST",
                            url = "{{TOKEN_URL}}",
                            headers = new Dictionary<string, string> { ["Content-Type"] = "application/x-www-form-urlencoded" },
                            form = passwordForm,
                            timeout_ms = 15000
                        },
                        save = new { access_token = new { from = "body", jsonpath = "$.access_token" } },
                        assert = new { status = 200 }
                    };

                case "CUSTOM_SCRIPT":
                    return new
                    {
                        id,
                        type = "script",
                        script = "{{TOKEN_SCRIPT_PATH}}",
                        save = new { access_token = new { from = "stdout" } },
                        timeout_ms = 30000
                    };

                default:
                    throw new ArgumentException($"Unsupported auth type for token step: {authProfile.Type}");
            }
        }

        private object GenerateHappyPathStep(SelectedEndpoint endpoint, AuthProfile authProfile, TestGenOptions? options)
        {
            var headers = new Dictionary<string, object>();
            var url = $"{{{{BASE_URL}}}}{endpoint.Path}";
            object? perStepAuth = null;

            switch (authProfile.Type)
            {
                case "API_KEY":
                    var injectAt = authProfile.Config.GetValueOrDefault("injectAt", "header");
                    if (injectAt == "header")
                    {
                        var headerName = authProfile.Config.GetValueOrDefault("headerName", "Authorization");
                        headers[headerName] = "{{API_KEY}}";
                    }
                    else if (injectAt == "query")
                    {
                        var queryName = authProfile.Config.GetValueOrDefault("queryName", "api_key");
                        var separator = endpoint.Path.Contains("?") ? "&" : "?";
                        url = $"{{{{BASE_URL}}}}{endpoint.Path}{separator}{queryName}={{{{API_KEY}}}}";
                    }
                    break;

                case "BASIC":
                    // handled by runner; no header here
                    break;

                case "BEARER":
                    headers["Authorization"] = "Bearer {{API_TOKEN}}";
                    break;

                case "OIDC_CLIENT_CREDENTIALS":
                case "OIDC_PASSWORD":
                case "CUSTOM_SCRIPT":
                    // Use per-step auth bound to get_token instead of manual header
                    perStepAuth = GenerateAuthConfig(authProfile, tokenStepId: "get_token");
                    break;
            }

            var request = new Dictionary<string, object>
            {
                ["method"] = endpoint.Method,
                ["url"] = url,
                ["timeout_ms"] = 15000
            };
            if (headers.Any()) request["headers"] = headers;

            if (endpoint.Method != "GET" && !string.IsNullOrEmpty(endpoint.RequestSchemaHint))
            {
                switch (endpoint.RequestSchemaHint)
                {
                    case "json": request["json"] = new { }; break;
                    case "form": request["form"] = new { test = "data" }; break;
                    case "multipart": request["multipart"] = new { field = "value" }; break;
                }
            }

            var assertObj = new Dictionary<string, object> { ["status"] = endpoint.SuccessCode };
            if (options?.IncludeJsonTypeChecks ?? true)
            {
                assertObj["content_type"] = "application/json";
                assertObj["jsonpath"] = new object[] { new object[] { "$", "type", "array" } };
            }
            else
            {
                assertObj["body_contains"] = new[] { "[", "]" };
            }

            var step = new Dictionary<string, object?>
            {
                ["id"] = "endpoint_happy",
                ["type"] = "http",
                ["request"] = request,
                ["assert"] = assertObj
            };
            if (perStepAuth != null) step["auth"] = perStepAuth;

            return step;
        }

        private object GenerateUnauthorizedStep(SelectedEndpoint endpoint)
        {
            return new
            {
                id = "endpoint_unauthorized",
                type = "http",
                auth = new { strategy = "none" },
                request = new
                {
                    method = endpoint.Method,
                    url = $"{{{{BASE_URL}}}}{endpoint.Path}",
                    timeout_ms = 15000
                },
                assert = new { status = 401 }
            };
        }

        private object GenerateForbiddenStep(SelectedEndpoint endpoint, TestGenOptions options)
        {
            // If an alternate profile is provided, emit an auth override pointing to its token_step
            if (options?.ForbiddenAuthProfile != null)
            {
                var f = options.ForbiddenAuthProfile;
                var authObj = GenerateAuthConfig(f, tokenStepId: GenerateTokenStepId(f, forForbidden: true));

                return new
                {
                    id = "endpoint_forbidden",
                    type = "http",
                    auth = authObj,
                    request = new
                    {
                        method = endpoint.Method,
                        url = $"{{{{BASE_URL}}}}{endpoint.Path}",
                        timeout_ms = 15000
                    },
                    assert = new { status = 403 }
                };
            }

            // Fallback to an invalid token (may return 401 on some systems)
            return new
            {
                id = "endpoint_forbidden",
                type = "http",
                request = new
                {
                    method = endpoint.Method,
                    url = $"{{{{BASE_URL}}}}{endpoint.Path}",
                    headers = new { Authorization = "Bearer invalid_or_limited_token" },
                    timeout_ms = 15000
                },
                assert = new { status = 403 }
            };
        }

        private string SanitizePath(string path)
        {
            return path
                .Replace("/", "-")
                .Replace("{", "")
                .Replace("}", "")
                .Replace("--", "-")
                .Trim('-');
        }
    }
}
