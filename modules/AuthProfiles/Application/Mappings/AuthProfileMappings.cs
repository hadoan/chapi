using System;
using System.Linq;
using System.Collections.Generic;
using AuthProfiles.Domain;
using AuthProfiles.Application.Dtos;
using AuthProfiles.Application.Requests;

namespace AuthProfiles.Application.Mappings
{
    public static class AuthProfileMappings
    {
        public static AuthProfileDto ToDto(this AuthProfile e)
        {
            var dict = e.SecretRefs.ToDictionary(s => s.Key, s => s.SecretRef);
            DateTimeOffset createdAt = DateTime.SpecifyKind(e.CreatedAt, DateTimeKind.Utc);
            DateTimeOffset updatedAt = e.UpdatedAt.HasValue ? DateTime.SpecifyKind(e.UpdatedAt.Value, DateTimeKind.Utc) : createdAt;

            return new AuthProfileDto(
                e.Id,
                e.ProjectId,
                e.ServiceId,
                e.EnvironmentKey,
                e.Type,
                e.TokenUrl,
                e.Audience,
                e.ScopesCsv,
                e.InjectionMode,
                e.InjectionName,
                e.InjectionFormat,
                e.DetectSource,
                e.DetectConfidence,
                e.Enabled,
                createdAt,
                updatedAt,
                dict
            );
        }

        public static void UpdateFrom(this AuthProfile e, UpdateAuthProfileRequest r)
        {
            e.UpdateCore(r.TokenUrl, r.Audience, r.ScopesCsv);
            e.SetInjection(r.InjectionMode, r.InjectionName, r.InjectionFormat);

            // sync secret refs: prefer replace strategy
            if (r.SecretRefs != null)
            {
                var keys = r.SecretRefs.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
                // remove those not present
                foreach (var exist in e.SecretRefs.ToList())
                {
                    if (!keys.Contains(exist.Key))
                        e.RemoveSecretRef(exist.Key);
                }

                foreach (var kv in r.SecretRefs)
                {
                    var id = Guid.NewGuid();
                    e.AddSecretRef(id, kv.Key, kv.Value);
                }
            }
        }
    }
}
