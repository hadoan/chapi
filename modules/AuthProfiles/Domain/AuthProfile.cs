using System;
using System.Collections.Generic;
using System.Linq;
using ShipMvp.Core.Entities;
using System.Text.Json.Serialization;

namespace AuthProfiles.Domain
{
    /// <summary>
    /// Aggregate root for authentication profiles.
    /// </summary>
    public class AuthProfile : Entity<Guid>
    {
        private readonly List<AuthProfileSecretRef> _secretRefs = new();

        // Params is a flexible JSON-backed bag for scheme-specific settings
        public class Params
        {
            public string? TokenUrl { get; set; }
            public string? AuthorizationUrl { get; set; }
            public string? Audience { get; set; }
            public string? Scopes { get; set; }
            public string? ClientId { get; set; }
            public string? ClientSecretRef { get; set; }

            public string? UsernameRef { get; set; }
            public string? PasswordRef { get; set; }

            public string? CustomLoginUrl { get; set; }
            public string? CustomBodyType { get; set; }
            public string? CustomUserKey { get; set; }
            public string? CustomPassKey { get; set; }
            public string? TokenJsonPath { get; set; }
        }

        public Params? Parameters { get; set; }


        private AuthProfile()
            : base(Guid.Empty)
        {
            // For EF
        }

        private AuthProfile(Guid id, Guid projectId, Guid serviceId, string environmentKey, AuthType type)
            : base(id)
        {
            ProjectId = projectId;
            ServiceId = serviceId;
            EnvironmentKey = string.IsNullOrWhiteSpace(environmentKey) ? throw new ArgumentException("EnvironmentKey is required", nameof(environmentKey)) : environmentKey;
            Type = type;
            InjectionMode = InjectionMode.Header;
            InjectionName = "Authorization";
            InjectionFormat = "Bearer {{access_token}}";
            DetectSource = "manual";
            DetectConfidence = 1.0;
            Enabled = true;
            Parameters = new Params();
            // CreatedAt set by base ctor
            UpdatedAt = DateTime.UtcNow;
        }

        public static AuthProfile Create(Guid id, Guid projectId, Guid serviceId, string environmentKey, AuthType type)
            => new AuthProfile(id, projectId, serviceId, environmentKey, type);

        public Guid ProjectId { get; private set; }

        public Guid ServiceId { get; private set; }

        public string EnvironmentKey { get; private set; } = null!;

        public AuthType Type { get; private set; }

        public string? TokenUrl { get; private set; }

        public string? AuthorizationUrl { get; private set; }

        public string? Audience { get; private set; }

        public string? ScopesCsv { get; private set; }

        // Backwards-compat: helper accessors to Parameters
        [JsonIgnore]
        public string? ParamsTokenUrl => Parameters?.TokenUrl ?? TokenUrl;

        [JsonIgnore]
        public string? ParamsTokenJsonPath => Parameters?.TokenJsonPath;

        public InjectionMode InjectionMode { get; private set; }

        public string InjectionName { get; private set; } = null!;

        public string InjectionFormat { get; private set; } = null!;

        public string DetectSource { get; private set; } = null!;

        public double DetectConfidence { get; private set; }

        public bool Enabled { get; private set; }

        public Guid? TenantId { get; private set; }

        // CreatedAt/UpdatedAt come from base Entity as DateTime/DateTime?

        public byte[]? RowVersion { get; private set; }

        public IReadOnlyCollection<AuthProfileSecretRef> SecretRefs => _secretRefs.AsReadOnly();

        public void Enable()
        {
            Enabled = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Disable()
        {
            Enabled = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetInjection(InjectionMode mode, string name, string format)
        {
            InjectionMode = mode;
            InjectionName = name ?? throw new ArgumentNullException(nameof(name));
            InjectionFormat = format ?? throw new ArgumentNullException(nameof(format));
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateDetection(string source, double confidence)
        {
            DetectSource = source ?? throw new ArgumentNullException(nameof(source));
            DetectConfidence = confidence;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateCore(string? tokenUrl, string? audience, string? scopesCsv)
        {
            TokenUrl = tokenUrl;
            Audience = audience;
            ScopesCsv = scopesCsv;
            if (Parameters == null) Parameters = new Params();
            Parameters.TokenUrl = tokenUrl;
            Parameters.Audience = audience;
            Parameters.Scopes = scopesCsv;
            UpdatedAt = DateTime.UtcNow;
        }

     
        public AuthProfileSecretRef AddSecretRef(Guid id, string key, string secretRef, string? notes = null)
        {
            var existing = _secretRefs.FirstOrDefault(s => s.Key == key);
            if (existing != null)
            {
                existing.UpdateSecretRef(secretRef, notes);
                UpdatedAt = DateTime.UtcNow;
                return existing;
            }

            var r = new AuthProfileSecretRef(id, Id, key, secretRef, notes);
            _secretRefs.Add(r);
            UpdatedAt = DateTime.UtcNow;
            return r;
        }

        public void RemoveSecretRef(string key)
        {
            var existing = _secretRefs.FirstOrDefault(s => s.Key == key);
            if (existing == null) return;
            _secretRefs.Remove(existing);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
