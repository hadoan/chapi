using System;
using System.Collections.Generic;
using AuthProfiles.Domain;

namespace AuthProfiles.Application.Requests
{
    public class UpdateAuthProfileRequest
    {
        public string? TokenUrl { get; init; }
        public string? Audience { get; init; }
        public string? ScopesCsv { get; init; }

        // Flexible params stored as JSON in DB
        public IDictionary<string, string>? Params { get; init; }

        public InjectionMode InjectionMode { get; init; } = InjectionMode.Header;
        public string InjectionName { get; init; } = "Authorization";
        public string InjectionFormat { get; init; } = "Bearer {{access_token}}";

        public IDictionary<string, string>? SecretRefs { get; init; }
    }
}
