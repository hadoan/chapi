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
        double Confidence
    );

    public record TestAuthRequest(Guid AuthProfileId, IDictionary<string, string>? OverrideSecretValues);

    public record TestAuthResult(bool Ok, string Status, string? Message, string? AccessToken, string? TokenType, DateTimeOffset? ExpiresAt, IDictionary<string, string>? Headers, string? CookieHeader);

    public record ExportInjectionResult(IDictionary<string, string> Headers, string? CookieHeader);
}
