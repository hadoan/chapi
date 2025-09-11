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
                // normalize keys for lookup (case-insensitive, preserve original value)
                var normalized = r.Params.ToDictionary(k => k.Key ?? string.Empty, v => v.Value, StringComparer.OrdinalIgnoreCase);

                string? GetParam(params string[] keys)
                {
                    foreach (var k in keys)
                    {
                        if (normalized.TryGetValue(k, out var val) && !string.IsNullOrEmpty(val)) return val;
                    }
                    return null;
                }

                var p = new AuthProfile.Params();

              
                // ClientId
                var cid = GetParam("ClientId", "client_id", "clientId");
                if (!string.IsNullOrEmpty(cid)) p.ClientId = cid;

                // ClientSecretRef (may be provided as client_secret_ref or client_secret)
                var csr = GetParam("ClientSecretRef", "client_secret_ref", "clientSecretRef", "client_secret", "clientSecret");
                if (!string.IsNullOrEmpty(csr)) p.ClientSecretRef = csr;

                // UsernameRef
                var ur = GetParam("UsernameRef", "username_ref", "usernameRef", "username");
                if (!string.IsNullOrEmpty(ur)) p.UsernameRef = ur;

                // PasswordRef
                var pr = GetParam("PasswordRef", "password_ref", "passwordRef", "password");
                if (!string.IsNullOrEmpty(pr)) p.PasswordRef = pr;

                // CustomLoginUrl
                var cu = GetParam("CustomLoginUrl", "custom_login_url", "customLoginUrl");
                if (!string.IsNullOrEmpty(cu)) p.CustomLoginUrl = cu;

                // CustomBodyType
                var cb = GetParam("CustomBodyType", "custom_body_type", "customBodyType");
                if (!string.IsNullOrEmpty(cb)) p.CustomBodyType = cb;

                // CustomUserKey
                var cuk = GetParam("CustomUserKey", "custom_user_key", "customUserKey");
                if (!string.IsNullOrEmpty(cuk)) p.CustomUserKey = cuk;

                // CustomPassKey
                var cpk = GetParam("CustomPassKey", "custom_pass_key", "customPassKey");
                if (!string.IsNullOrEmpty(cpk)) p.CustomPassKey = cpk;

                // TokenJsonPath
                var tj = GetParam("TokenJsonPath", "token_json_path", "tokenJsonPath");
                if (!string.IsNullOrEmpty(tj)) p.TokenJsonPath = tj;

                e.Parameters = p;

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

                // build lookup of existing refs by key (case-insensitive)
                var existingLookup = e.SecretRefs.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);

                foreach (var kv in r.SecretRefs)
                {
                    if (existingLookup.TryGetValue(kv.Key, out var existing))
                    {
                        // update existing secret ref value
                        existing.UpdateSecretRef(kv.Value);
                    }
                    else
                    {
                        var id = Guid.NewGuid();
                        // kv.Value is expected to be the secret reference or secret value depending on caller
                        e.AddSecretRef(id, kv.Key, kv.Value);
                    }
                }
            }
        }
    }
}
