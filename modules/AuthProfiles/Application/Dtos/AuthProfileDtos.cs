using System;
using System.Collections.Generic;
using AuthProfiles.Domain;

namespace AuthProfiles.Application.Dtos
{
    public record AuthProfileDto(
        Guid Id,
        Guid ProjectId,
        Guid ServiceId,
        string EnvironmentKey,
    AuthType Type,
    IDictionary<string, object>? Params,
    string? TokenUrl,
    string? Audience,
    string? ScopesCsv,
        InjectionMode InjectionMode,
        string InjectionName,
        string InjectionFormat,
        string DetectSource,
        double DetectConfidence,
        bool Enabled,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        IReadOnlyDictionary<string, string> SecretRefs
    );

    public record AuthDetectionCandidateDto(
        AuthType Type,
        string? TokenUrl,
        string? Audience,
        string? ScopesCsv,
        InjectionMode InjectionMode,
        string InjectionName,
        string InjectionFormat,
        string Source,
        double Confidence,
        TokenFormHints? Form
    );

    // TokenFormHints, DetectionCandidateDto and InjectionPreview are defined in DetectionDtos.cs

    public record TestAuthRequest(Guid AuthProfileId, AuthProfileDto? ProfileInline, Guid? EnvId, IDictionary<string, string>? OverrideSecretValues);

    // SampleTokenPrefix: first N chars of token for display; AccessToken may be returned by internal services but Test endpoint should mask
    public record TestAuthResult(bool Ok, string Status, string? Message, string? SampleTokenPrefix, string? AccessToken, string? TokenType, DateTimeOffset? ExpiresAt, IDictionary<string, string>? Headers, string? CookieHeader);

    public record ExportInjectionResult(IDictionary<string, string> Headers, string? CookieHeader);
}
