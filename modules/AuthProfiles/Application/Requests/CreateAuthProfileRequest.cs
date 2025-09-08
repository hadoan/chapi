using System;
using System.Collections.Generic;
using AuthProfiles.Domain;

namespace AuthProfiles.Application.Requests
{
    public class CreateAuthProfileRequest
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid ProjectId { get; init; }
        public Guid ServiceId { get; init; }
        public string EnvironmentKey { get; init; } = "Dev";
        public AuthType Type { get; init; }

        public string? TokenUrl { get; init; }
        public string? Audience { get; init; }
        public string? ScopesCsv { get; init; }

        public InjectionMode InjectionMode { get; init; } = InjectionMode.Header;
        public string InjectionName { get; init; } = "Authorization";
        public string InjectionFormat { get; init; } = "Bearer {{access_token}}";

        public IDictionary<string, string>? SecretRefs { get; init; }
    }
}
