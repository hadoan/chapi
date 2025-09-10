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
            IDictionary<string, object>? paramsDict = null;
            if (e.Parameters != null)
            {
                paramsDict = new Dictionary<string, object>();
                // copy known params
                if (!string.IsNullOrEmpty(e.Parameters.TokenUrl)) paramsDict["TokenUrl"] = e.Parameters.TokenUrl;
                if (!string.IsNullOrEmpty(e.Parameters.ClientId)) paramsDict["ClientId"] = e.Parameters.ClientId;
                if (!string.IsNullOrEmpty(e.Parameters.ClientSecretRef)) paramsDict["ClientSecretRef"] = e.Parameters.ClientSecretRef;
                if (!string.IsNullOrEmpty(e.Parameters.UsernameRef)) paramsDict["UsernameRef"] = e.Parameters.UsernameRef;
                if (!string.IsNullOrEmpty(e.Parameters.PasswordRef)) paramsDict["PasswordRef"] = e.Parameters.PasswordRef;
                if (!string.IsNullOrEmpty(e.Parameters.CustomLoginUrl)) paramsDict["CustomLoginUrl"] = e.Parameters.CustomLoginUrl;
                if (!string.IsNullOrEmpty(e.Parameters.CustomBodyType)) paramsDict["CustomBodyType"] = e.Parameters.CustomBodyType;
                if (!string.IsNullOrEmpty(e.Parameters.CustomUserKey)) paramsDict["CustomUserKey"] = e.Parameters.CustomUserKey;
                if (!string.IsNullOrEmpty(e.Parameters.CustomPassKey)) paramsDict["CustomPassKey"] = e.Parameters.CustomPassKey;
                if (!string.IsNullOrEmpty(e.Parameters.TokenJsonPath)) paramsDict["TokenJsonPath"] = e.Parameters.TokenJsonPath;
            }
            DateTimeOffset createdAt = DateTime.SpecifyKind(e.CreatedAt, DateTimeKind.Utc);
            DateTimeOffset updatedAt = e.UpdatedAt.HasValue ? DateTime.SpecifyKind(e.UpdatedAt.Value, DateTimeKind.Utc) : createdAt;

            return new AuthProfileDto(
                e.Id,
                e.ProjectId,
                e.ServiceId,
                e.EnvironmentKey,
                e.Type,
                paramsDict,
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
            if (r.Params != null)
            {
                var p = new AuthProfile.Params();
                if (r.Params.TryGetValue("TokenUrl", out var tv)) p.TokenUrl = tv;
                if (r.Params.TryGetValue("ClientId", out var cid)) p.ClientId = cid;
                if (r.Params.TryGetValue("ClientSecretRef", out var csr)) p.ClientSecretRef = csr;
                if (r.Params.TryGetValue("UsernameRef", out var ur)) p.UsernameRef = ur;
                if (r.Params.TryGetValue("PasswordRef", out var pr)) p.PasswordRef = pr;
                if (r.Params.TryGetValue("CustomLoginUrl", out var cu)) p.CustomLoginUrl = cu;
                if (r.Params.TryGetValue("CustomBodyType", out var cb)) p.CustomBodyType = cb;
                if (r.Params.TryGetValue("CustomUserKey", out var cuk)) p.CustomUserKey = cuk;
                if (r.Params.TryGetValue("CustomPassKey", out var cpk)) p.CustomPassKey = cpk;
                if (r.Params.TryGetValue("TokenJsonPath", out var tj)) p.TokenJsonPath = tj;
                e.UpdateParameters(p);
            }
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
