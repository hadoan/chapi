using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using AuthProfiles.Application.Services;
using AuthProfiles.Domain;
using AuthProfiles.Application.Dtos;
using System.Collections.Generic;

namespace AuthProfiles.Infrastructure.Services
{
    public class TokenCache : ITokenCache
    {
        private readonly IDistributedCache _cache;

        public TokenCache(IDistributedCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<TestAuthResult> GetOrAcquireAsync(AuthProfile profile, IReadOnlyDictionary<string, string> resolvedSecrets, CancellationToken ct)
        {
            var key = $"token:{profile.Id}:{profile.EnvironmentKey}";
            var raw = await _cache.GetStringAsync(key, ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    var cached = JsonSerializer.Deserialize<CachedToken>(raw);
                    if (cached != null && cached.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(60))
                    {
                        return new TestAuthResult(true, "ok", null, cached.AccessToken, cached.AccessToken, cached.TokenType, cached.ExpiresAt, new Dictionary<string, string> { { "Authorization", $"{cached.TokenType} {cached.AccessToken}" } }, null);
                    }
                }
                catch
                {
                    // ignore and acquire
                }
            }

            var acquired = await new AuthTokenService(new System.Net.Http.HttpClient()).AcquireAsync(profile, resolvedSecrets, ct).ConfigureAwait(false);
            if (acquired.Ok && acquired.AccessToken != null && acquired.ExpiresAt.HasValue)
            {
                var ctok = new CachedToken { AccessToken = acquired.AccessToken, TokenType = acquired.TokenType ?? "Bearer", ExpiresAt = acquired.ExpiresAt.Value };
                var json = JsonSerializer.Serialize(ctok);
                var options = new DistributedCacheEntryOptions { AbsoluteExpiration = ctok.ExpiresAt };
                await _cache.SetStringAsync(key, json, options, ct).ConfigureAwait(false);
            }

            return acquired;
        }

        private class CachedToken
        {
            public string AccessToken { get; set; } = null!;
            public string TokenType { get; set; } = "Bearer";
            public DateTimeOffset ExpiresAt { get; set; }
        }
    }
}
