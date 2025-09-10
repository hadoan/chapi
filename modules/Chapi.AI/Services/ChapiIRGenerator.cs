using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Chapi.AI.Dto;

namespace Chapi.AI.Services
{
    public interface IChapiIRGenerator
    {
        string GenerateTestsJsonContent(SelectedEndpoint endpoint, AuthProfile authProfile, TestGenOptions options);
    }

    public class ChapiIRGenerator : IChapiIRGenerator
    {
        private readonly ILogger<ChapiIRGenerator> _logger;

        public ChapiIRGenerator(ILogger<ChapiIRGenerator> logger)
        {
            _logger = logger;
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
                    secrets = GetRequiredSecrets(authProfile),
                    auth = GenerateAuthConfig(authProfile),
                    steps = GenerateTestSteps(endpoint, authProfile, options)
                }
            };

            return JsonSerializer.Serialize(suite, new JsonSerializerOptions { WriteIndented = true });
        }

        private string[] GetRequiredSecrets(AuthProfile authProfile)
        {
            var secrets = new List<string> { "BASE_URL" };

            switch (authProfile.Type)
            {
                case "API_KEY":
                    secrets.Add("API_KEY");
                    break;
                case "BASIC":
                    secrets.Add("BASIC_USER");
                    secrets.Add("BASIC_PASS");
                    break;
                case "BEARER":
                    secrets.Add("API_TOKEN");
                    break;
                case "OIDC_CLIENT_CREDENTIALS":
                    secrets.Add("TOKEN_URL");
                    secrets.Add("CLIENT_ID");
                    secrets.Add("CLIENT_SECRET");
                    if (authProfile.Config.ContainsKey("scope")) secrets.Add("SCOPE");
                    if (authProfile.Config.ContainsKey("audience")) secrets.Add("AUDIENCE");
                    break;
                case "OIDC_PASSWORD":
                    secrets.Add("TOKEN_URL");
                    secrets.Add("CLIENT_ID");
                    secrets.Add("CLIENT_SECRET");
                    secrets.Add("USERNAME");
                    secrets.Add("PASSWORD");
                    break;
                case "CUSTOM_SCRIPT":
                    secrets.Add("TOKEN_SCRIPT_PATH");
                    break;
            }

            return secrets.ToArray();
        }

        private object GenerateAuthConfig(AuthProfile authProfile)
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
                        token_step = "get_token",
                        inject = new { header = "Authorization", format = "Bearer {access_token}" }
                    };

                case "OIDC_PASSWORD":
                    return new
                    {
                        strategy = "password",
                        token_step = "get_token",
                        inject = new { header = "Authorization", format = "Bearer {access_token}" }
                    };

                case "CUSTOM_SCRIPT":
                    return new
                    {
                        strategy = "custom",
                        token_step = "get_token",
                        inject = new { header = "Authorization", format = "Bearer {access_token}" }
                    };

                default:
                    return new { strategy = "none" };
            }
        }

        private object[] GenerateTestSteps(SelectedEndpoint endpoint, AuthProfile authProfile, TestGenOptions options)
        {
            var steps = new List<object>();

            // Add token step if needed
            if (authProfile.Type == "OIDC_CLIENT_CREDENTIALS" || authProfile.Type == "OIDC_PASSWORD" || authProfile.Type == "CUSTOM_SCRIPT")
            {
                steps.Add(GenerateTokenStep(authProfile));
            }

            // Add happy path step
            steps.Add(GenerateHappyPathStep(endpoint, authProfile));

            // Add unauthorized step if endpoint requires auth
            if (endpoint.RequiresAuth)
            {
                steps.Add(GenerateUnauthorizedStep(endpoint));
            }

            // Add forbidden step if requested and applicable
            if (options.IncludeForbidden && endpoint.RequiresAuth)
            {
                steps.Add(GenerateForbiddenStep(endpoint));
            }

            return steps.ToArray();
        }

        private object GenerateTokenStep(AuthProfile authProfile)
        {
            switch (authProfile.Type)
            {
                case "OIDC_CLIENT_CREDENTIALS":
                    var form = new Dictionary<string, object>
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = "{{CLIENT_ID}}",
                        ["client_secret"] = "{{CLIENT_SECRET}}"
                    };

                    if (authProfile.Config.ContainsKey("scope"))
                        form["scope"] = "{{SCOPE|api}}";
                    if (authProfile.Config.ContainsKey("audience"))
                        form["audience"] = "{{AUDIENCE|}}";

                    return new
                    {
                        id = "get_token",
                        type = "http",
                        request = new
                        {
                            method = "POST",
                            url = "{{TOKEN_URL}}",
                            headers = new { ContentType = "application/x-www-form-urlencoded" },
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

                    return new
                    {
                        id = "get_token",
                        type = "http",
                        request = new
                        {
                            method = "POST",
                            url = "{{TOKEN_URL}}",
                            headers = new { ContentType = "application/x-www-form-urlencoded" },
                            form = passwordForm,
                            timeout_ms = 15000
                        },
                        save = new { access_token = new { from = "body", jsonpath = "$.access_token" } },
                        assert = new { status = 200 }
                    };

                case "CUSTOM_SCRIPT":
                    return new
                    {
                        id = "get_token",
                        type = "script",
                        script = "{{TOKEN_SCRIPT_PATH}}",
                        save = new { access_token = new { from = "stdout" } },
                        timeout_ms = 30000
                    };

                default:
                    throw new ArgumentException($"Unsupported auth type for token step: {authProfile.Type}");
            }
        }

        private object GenerateHappyPathStep(SelectedEndpoint endpoint, AuthProfile authProfile)
        {
            var headers = new Dictionary<string, object>();
            var url = $"{{{{BASE_URL}}}}{endpoint.Path}";

            // Add auth headers based on auth profile
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
                    // Basic auth handled via curl -u flag in runner
                    break;

                case "BEARER":
                    headers["Authorization"] = "Bearer {{API_TOKEN}}";
                    break;

                case "OIDC_CLIENT_CREDENTIALS":
                case "OIDC_PASSWORD":
                case "CUSTOM_SCRIPT":
                    headers["Authorization"] = "Bearer {access_token}";
                    break;
            }

            var request = new Dictionary<string, object>
            {
                ["method"] = endpoint.Method,
                ["url"] = url,
                ["timeout_ms"] = 15000
            };

            if (headers.Any())
                request["headers"] = headers;

            // Add request body for non-GET methods
            if (endpoint.Method != "GET" && !string.IsNullOrEmpty(endpoint.RequestSchemaHint))
            {
                switch (endpoint.RequestSchemaHint)
                {
                    case "json":
                        request["json"] = new { };
                        break;
                    case "form":
                        request["form"] = new { test = "data" };
                        break;
                    case "multipart":
                        request["multipart"] = new { field = "value" };
                        break;
                }
            }

            return new
            {
                id = "endpoint_happy",
                type = "http",
                request,
                assert = new
                {
                    status = endpoint.SuccessCode,
                    body_contains = new[] { "[", "]" } // Minimal check for JSON response
                }
            };
        }

        private object GenerateUnauthorizedStep(SelectedEndpoint endpoint)
        {
            return new
            {
                id = "endpoint_unauthorized",
                type = "http",
                request = new
                {
                    method = endpoint.Method,
                    url = $"{{{{BASE_URL}}}}{endpoint.Path}",
                    timeout_ms = 15000
                },
                assert = new { status = 401 }
            };
        }

        private object GenerateForbiddenStep(SelectedEndpoint endpoint)
        {
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
