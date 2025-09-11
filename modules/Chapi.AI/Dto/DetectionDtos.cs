using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chapi.AI.Dto
{
    public sealed record InjectionPreview(
        [property: JsonPropertyName("mode")] string Mode, 
        [property: JsonPropertyName("name")] string Name, 
        [property: JsonPropertyName("format")] string Format);

    public sealed record DetectedParameters(
        [property: JsonPropertyName("tokenUrl")] string? TokenUrl,
        [property: JsonPropertyName("authorizationUrl")] string? AuthorizationUrl,
        [property: JsonPropertyName("audience")] string? Audience,
        [property: JsonPropertyName("scopes")] string? Scopes,
        [property: JsonPropertyName("clientId")] string? ClientId,
        [property: JsonPropertyName("clientSecretRef")] string? ClientSecretRef,
        [property: JsonPropertyName("usernameRef")] string? UsernameRef,
        [property: JsonPropertyName("passwordRef")] string? PasswordRef,
        [property: JsonPropertyName("customLoginUrl")] string? CustomLoginUrl,
        [property: JsonPropertyName("customBodyType")] string? CustomBodyType,
        [property: JsonPropertyName("customUserKey")] string? CustomUserKey,
        [property: JsonPropertyName("customPassKey")] string? CustomPassKey,
        [property: JsonPropertyName("tokenJsonPath")] string? TokenJsonPath
    );

    public sealed record DetectedSecret(
        [property: JsonPropertyName("key")] string Key, 
        [property: JsonPropertyName("secretRef")] string SecretRef, 
        [property: JsonPropertyName("notes")] string? Notes);

    public sealed record DetectedBody(
        [property: JsonPropertyName("kind")] string? Kind,
        [property: JsonPropertyName("value")] Dictionary<string, object>? Value
    );

    public sealed record DetectedExpect(
        [property: JsonPropertyName("status")] int Status, 
        [property: JsonPropertyName("tokenJsonPath")] string? TokenJsonPath);

    public sealed record DetectedTokenRequest(
        [property: JsonPropertyName("method")] string? Method,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("headers")] Dictionary<string, string>? Headers,
        [property: JsonPropertyName("body")] DetectedBody? Body,
        [property: JsonPropertyName("expect")] DetectedExpect? Expect
    );

    public sealed record DetectedProfile(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("environmentKey")] string EnvironmentKey,
        [property: JsonPropertyName("parameters")] DetectedParameters Parameters,
        [property: JsonPropertyName("injection")] InjectionPreview Injection,
        [property: JsonPropertyName("secrets")] List<DetectedSecret>? Secrets,
        [property: JsonPropertyName("token_request")] DetectedTokenRequest? TokenRequest
    );

    // This matches the root JSON structure
    public sealed record DetectionResponseDto(
        [property: JsonPropertyName("detect_source")] string DetectSource,
        [property: JsonPropertyName("detect_confidence")] double DetectConfidence,
        [property: JsonPropertyName("profile")] DetectedProfile Profile
    );

    public sealed record TokenFormHints(string GrantType, IReadOnlyDictionary<string, string>? Value);

    public sealed record DetectionCandidateDto(
        string DetectSource,
        double DetectConfidence,
        DetectedProfile? Profile
    );

    public sealed record DetectByCodeRequest(string Code, Guid? ProjectId);

    public sealed record DetectByPromptRequest(string Prompt, Guid? ProjectId);
}
