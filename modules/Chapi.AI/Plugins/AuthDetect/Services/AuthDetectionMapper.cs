using System;
using System.Linq;
using AuthProfiles.Domain;
using Plugins.AuthDetect.Dto;

namespace Plugins.AuthDetect.Services
{
    public static class AuthDetectionMapper
    {
        public static AuthProfile ToEntity(
            AuthDetectionResult dto,
            Guid id,
            Guid projectId,
            Guid serviceId)
        {
            var p = dto.Profile;

            // Map loose type strings to AuthType enum
            AuthType MapType(string? t)
            {
                if (string.IsNullOrWhiteSpace(t)) return AuthType.OAuth2ClientCredentials;
                var s = t.Trim().ToLowerInvariant();
                if (s.Contains("password")) return AuthType.OAuth2Password;
                if (s.Contains("oauth") || s.Contains("oidc")) return AuthType.OAuth2ClientCredentials;
                if (s.Contains("basic")) return AuthType.Basic;
                if (s.Contains("bearer")) return AuthType.BearerStatic;
                if (s.Contains("api")) return AuthType.ApiKeyHeader;
                if (s.Contains("cookie")) return AuthType.SessionCookie;
                if (s.Contains("custom")) return AuthType.CustomLogin;
                return AuthType.OAuth2ClientCredentials;
            }

            var type = MapType(p.Type);

            var entity = AuthProfile.Create(
                id: id,
                projectId: projectId,
                serviceId: serviceId,
                environmentKey: p.EnvironmentKey,
                type: type
            );

            // Core + Parameters
            entity.UpdateCore(
                tokenUrl: p.Parameters.TokenUrl,
                audience: p.Parameters.Audience,
                scopesCsv: p.Parameters.Scopes
            );

            // Injection
            var mode = Enum.TryParse<InjectionMode>(p.Injection.Mode, true, out var m)
                ? m
                : InjectionMode.Header;

            entity.SetInjection(
                mode,
                name: p.Injection.Name,
                format: p.Injection.Format
            );

            // Detection provenance
            entity.UpdateDetection(dto.DetectSource, dto.DetectConfidence);

            // Secrets
            foreach (var s in p.Secrets ?? Enumerable.Empty<DetectedSecret>())
            {
                entity.AddSecretRef(Guid.NewGuid(), s.Key, s.SecretRef, s.Notes);
            }

            // Extra parameter mirrors (for convenience)
            if (entity.Parameters != null)
            {
                entity.Parameters.AuthorizationUrl = p.Parameters.AuthorizationUrl;
                entity.Parameters.ClientId = p.Parameters.ClientId;
                entity.Parameters.ClientSecretRef = p.Parameters.ClientSecretRef;
                entity.Parameters.UsernameRef = p.Parameters.UsernameRef;
                entity.Parameters.PasswordRef = p.Parameters.PasswordRef;
                entity.Parameters.CustomLoginUrl = p.Parameters.CustomLoginUrl;
                entity.Parameters.CustomBodyType = p.Parameters.CustomBodyType;
                entity.Parameters.CustomUserKey = p.Parameters.CustomUserKey;
                entity.Parameters.CustomPassKey = p.Parameters.CustomPassKey;
                entity.Parameters.TokenJsonPath = p.Parameters.TokenJsonPath;
            }

            return entity;
        }
    }
}
