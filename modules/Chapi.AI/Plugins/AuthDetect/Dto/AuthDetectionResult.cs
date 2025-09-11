using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Plugins.AuthDetect.Dto
{
    public sealed class AuthDetectionResult
    {
        [JsonPropertyName("detect_source")] public string DetectSource { get; set; } = "";
        [JsonPropertyName("detect_confidence")] public double DetectConfidence { get; set; }
        [JsonPropertyName("profile")] public DetectedProfile Profile { get; set; } = new();
    }

    public sealed class DetectedProfile
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "Custom"; // OAuth2|OIDC|ApiKey|Basic|Custom
        [JsonPropertyName("environmentKey")] public string EnvironmentKey { get; set; } = "dev";
        [JsonPropertyName("parameters")] public DetectedParams Parameters { get; set; } = new();
        [JsonPropertyName("injection")] public DetectedInjection Injection { get; set; } = new();
        [JsonPropertyName("secrets")] public List<DetectedSecret> Secrets { get; set; } = new();
        [JsonPropertyName("token_request")] public DetectedTokenRequest TokenRequest { get; set; } = new();
    }

    public sealed class DetectedParams
    {
        [JsonPropertyName("tokenUrl")] public string? TokenUrl { get; set; }
        [JsonPropertyName("authorizationUrl")] public string? AuthorizationUrl { get; set; }
        [JsonPropertyName("audience")] public string? Audience { get; set; }
        [JsonPropertyName("scopes")] public string? Scopes { get; set; }

        [JsonPropertyName("clientId")] public string? ClientId { get; set; }
        [JsonPropertyName("clientSecretRef")] public string? ClientSecretRef { get; set; }

        [JsonPropertyName("usernameRef")] public string? UsernameRef { get; set; }
        [JsonPropertyName("passwordRef")] public string? PasswordRef { get; set; }

        [JsonPropertyName("customLoginUrl")] public string? CustomLoginUrl { get; set; }
        [JsonPropertyName("customBodyType")] public string? CustomBodyType { get; set; } // json|form|raw|null
        [JsonPropertyName("customUserKey")] public string? CustomUserKey { get; set; }
        [JsonPropertyName("customPassKey")] public string? CustomPassKey { get; set; }
        [JsonPropertyName("tokenJsonPath")] public string? TokenJsonPath { get; set; }
    }

    public sealed class DetectedInjection
    {
        [JsonPropertyName("mode")] public string Mode { get; set; } = "Header"; // Header|Query|Cookie|Body
        [JsonPropertyName("name")] public string Name { get; set; } = "Authorization";
        [JsonPropertyName("format")] public string Format { get; set; } = "Bearer {{access_token}}";
    }

    public sealed class DetectedSecret
    {
        [JsonPropertyName("key")] public string Key { get; set; } = "";
        [JsonPropertyName("secretRef")] public string SecretRef { get; set; } = "";
        [JsonPropertyName("notes")] public string? Notes { get; set; }
    }

    public sealed class DetectedTokenRequest
    {
        [JsonPropertyName("method")] public string? Method { get; set; } // POST|GET|...
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("headers")] public Dictionary<string, string> Headers { get; set; } = new();
        [JsonPropertyName("body")] public DetectedBody Body { get; set; } = new();
        [JsonPropertyName("expect")] public DetectedExpect Expect { get; set; } = new();
    }

    public sealed class DetectedBody
    {
        [JsonPropertyName("kind")] public string? Kind { get; set; } // json|form|raw|null
        [JsonPropertyName("value")] public object? Value { get; set; }
    }

    public sealed class DetectedExpect
    {
        [JsonPropertyName("status")] public int Status { get; set; } = 200;
        [JsonPropertyName("tokenJsonPath")] public string? TokenJsonPath { get; set; }
    }
}
