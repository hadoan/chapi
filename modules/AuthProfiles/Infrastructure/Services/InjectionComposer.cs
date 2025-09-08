using System;
using System.Collections.Generic;
using System.Linq;
using AuthProfiles.Application.Dtos;
using AuthProfiles.Application.Services;
using AuthProfiles.Domain;

namespace AuthProfiles.Infrastructure.Services
{
    public class InjectionComposer : IInjectionComposer
    {
        public ExportInjectionResult Compose(AuthProfile profile, TestAuthResult token, IReadOnlyDictionary<string, string> resolvedSecrets)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? cookieHeader = null;

            switch (profile.Type)
            {
                case AuthType.OAuth2ClientCredentials:
                case AuthType.BearerStatic:
                    if (token.Ok && !string.IsNullOrWhiteSpace(token.AccessToken))
                    {
                        headers[profile.InjectionName] = profile.InjectionFormat.Replace("{{access_token}}", token.AccessToken);
                    }
                    break;
                case AuthType.ApiKeyHeader:
                    // look up first secret value for API key
                    if (resolvedSecrets.TryGetValue("API_KEY", out var apiKey) && !string.IsNullOrWhiteSpace(apiKey))
                        headers[profile.InjectionName] = profile.InjectionFormat.Replace("{{api_key}}", apiKey).Replace("{{API_KEY}}", apiKey);
                    break;
                case AuthType.SessionCookie:
                    if (resolvedSecrets.TryGetValue("COOKIE", out var cookie) && !string.IsNullOrWhiteSpace(cookie))
                        cookieHeader = cookie;
                    break;
            }

            return new ExportInjectionResult(headers, cookieHeader);
        }
    }
}
