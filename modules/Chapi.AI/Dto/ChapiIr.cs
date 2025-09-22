
// ChapiIr.cs
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chapi.IR
{
    public sealed class ChapiIr
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "chapi-ir/0.1";

        [JsonPropertyName("suite")]
        public Suite Suite { get; set; } = new();
    }

    public sealed class Suite
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        // Arbitrary key/value environment vars (e.g., BASE_URL, TOKEN_URL, etc.)
        [JsonPropertyName("env")]
        public Dictionary<string, string> Env { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        // Secret keys referenced elsewhere (e.g., CLIENT_ID)
        [JsonPropertyName("secrets")]
        public List<string> Secrets { get; set; } = new();

        [JsonPropertyName("auth")]
        public Auth Auth { get; set; } = new();

        [JsonPropertyName("tests")]
        public List<Test> Tests { get; set; } = new();

        [JsonPropertyName("metadata")]
        public Metadata Metadata { get; set; } = new();
    }

    public sealed class Auth
    {
        [JsonPropertyName("profile")]
        public AuthProfile Profile { get; set; } = new();

        // Optional; omit when there is no TokenUrl / token flow.
        [JsonPropertyName("token_request")]
        public TokenRequest? TokenRequest { get; set; }
    }

    public sealed class AuthProfile
    {
        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AuthType Type { get; set; } = AuthType.None;

        [JsonPropertyName("environmentKey")]
        public string EnvironmentKey { get; set; } = "default";

        [JsonPropertyName("parameters")]
        public AuthParameters Parameters { get; set; } = new();

        [JsonPropertyName("injection")]
        public Injection Injection { get; set; } = new();
    }

    public enum AuthType
    {
        OAuth2,
        ApiKey,
        None
    }

    public sealed class AuthParameters
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
        [JsonPropertyName("customBodyType")] public string? CustomBodyType { get; set; }
        [JsonPropertyName("customUserKey")] public string? CustomUserKey { get; set; }
        [JsonPropertyName("customPassKey")] public string? CustomPassKey { get; set; }
        [JsonPropertyName("tokenJsonPath")] public string? TokenJsonPath { get; set; }
    }

    public sealed class Injection
    {
        [JsonPropertyName("mode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public InjectionMode Mode { get; set; } = InjectionMode.Header;

        // e.g., "X-API-Key" or "Authorization"
        [JsonPropertyName("name")]
        public string Name { get; set; } = "Authorization";

        // e.g., "Bearer {{access_token}}"
        [JsonPropertyName("format")]
        public string Format { get; set; } = "Bearer {{access_token}}";
    }

    public enum InjectionMode
    {
        Header,
        Query,
        Cookie
    }

    public sealed class TokenRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "oauth_password_grant";

        [JsonPropertyName("request")]
        public HttpRequestDef Request { get; set; } = new();

        [JsonPropertyName("expect")]
        public Expect Expect { get; set; } = new();

        // Flexible map (at minimum access_token is expected)
        [JsonPropertyName("save")]
        public Dictionary<string, string> Save { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class Test
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        // Optional; e.g., ["oauth_password_grant"]
        [JsonPropertyName("pre")]
        public List<string>? Pre { get; set; }

        [JsonPropertyName("request")]
        public HttpRequestDef Request { get; set; } = new();

        [JsonPropertyName("expect")]
        public Expect Expect { get; set; } = new();
    }

    public sealed class HttpRequestDef
    {
        [JsonPropertyName("method")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public HttpMethodVerb Method { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        // Arbitrary headers
        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("body")]
        public BodyDef? Body { get; set; }
    }

    public enum HttpMethodVerb
    {
        GET,
        POST,
        PUT,
        PATCH,
        DELETE
    }

    public sealed class BodyDef
    {
        [JsonPropertyName("kind")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BodyKind Kind { get; set; } = BodyKind.json;

        // Arbitrary JSON payload (object/array/primitive)
        [JsonPropertyName("value")]
        public JsonElement Value { get; set; }
    }

    public enum BodyKind
    {
        json,
        form,
        text,
        none
    }

    public sealed class Expect
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("jsonpath")]
        public List<JsonPathCheck>? JsonPath { get; set; }
    }

    public sealed class JsonPathCheck
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        // Optional matchers
        [JsonPropertyName("equals")]
        public JsonElement? Equals { get; set; }  // can be any JSON type

        [JsonPropertyName("exists")]
        public bool? Exists { get; set; }

        [JsonPropertyName("contains")]
        public string? Contains { get; set; }
    }

    public sealed class Metadata
    {
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("endpoint")]
        public EndpointMeta? Endpoint { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public sealed class EndpointMeta
    {
        [JsonPropertyName("method")]
        public string? Method { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        [JsonPropertyName("requiresAuth")]
        public bool? RequiresAuth { get; set; }

        // e.g., [{ "Bearer": [] }]
        [JsonPropertyName("security")]
        public List<Dictionary<string, List<string>>>? Security { get; set; }
    }

    // Handy options you can reuse wherever you serialize/deserialize this IR
    public static class ChapiIrJson
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }
}
