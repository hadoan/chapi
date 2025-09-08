using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using AuthProfiles.Domain;
using AuthProfiles.Application.Dtos;
using AuthProfiles.Application.Requests;
using AuthProfiles.Application.Mappings;

namespace AuthProfiles.Application.Services
{
    /// <summary>
    /// Application service implementing read/write operations for AuthProfile.
    /// </summary>
    public class AuthProfileService : IAuthProfileService, IAuthProfileReadService
    {
        private readonly IAuthProfileRepository _repo;
        private readonly ISecretStore _secretStore;
        private readonly IAuthTokenService _tokenService;
        private readonly ITokenCache _tokenCache;

        public AuthProfileService(IAuthProfileRepository repo, ISecretStore secretStore, IAuthTokenService tokenService, ITokenCache tokenCache)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _tokenCache = tokenCache ?? throw new ArgumentNullException(nameof(tokenCache));
        }

        public async Task<AuthProfileDto> CreateAsync(CreateAuthProfileRequest r, CancellationToken ct)
        {
            if (r.ProjectId == Guid.Empty) throw new ArgumentException("ProjectId is required", nameof(r.ProjectId));
            if (r.ServiceId == Guid.Empty) throw new ArgumentException("ServiceId is required", nameof(r.ServiceId));
            if (string.IsNullOrWhiteSpace(r.EnvironmentKey)) throw new ArgumentException("EnvironmentKey is required", nameof(r.EnvironmentKey));

            // TokenUrl required for OAuth2
            if (r.Type == AuthType.OAuth2ClientCredentials && string.IsNullOrWhiteSpace(r.TokenUrl))
                throw new ArgumentException("TokenUrl is required for OAuth2ClientCredentials", nameof(r.TokenUrl));

            var entity = AuthProfile.Create(r.Id, r.ProjectId, r.ServiceId, r.EnvironmentKey, r.Type);
            entity.SetInjection(r.InjectionMode, r.InjectionName, r.InjectionFormat);
            entity.UpdateCore(r.TokenUrl, r.Audience, r.ScopesCsv);

            if (r.SecretRefs != null)
            {
                foreach (var kv in r.SecretRefs)
                {
                    entity.AddSecretRef(Guid.NewGuid(), kv.Key, kv.Value);
                }
            }

            await _repo.AddAsync(entity, ct).ConfigureAwait(false);

            return entity.ToDto();
        }

        public async Task<AuthProfileDto> UpdateAsync(Guid id, UpdateAuthProfileRequest r, CancellationToken ct)
        {
            var entity = await _repo.GetWithSecretsAsync(id, ct).ConfigureAwait(false) ?? throw new KeyNotFoundException($"AuthProfile {id} not found");
            entity.UpdateFrom(r);
            await _repo.UpdateAsync(entity, ct).ConfigureAwait(false);
            return entity.ToDto();
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var entity = await _repo.GetByIdAsync(id, ct).ConfigureAwait(false) ?? throw new KeyNotFoundException($"AuthProfile {id} not found");
            await _repo.DeleteAsync(id, ct).ConfigureAwait(false);
        }

        public async Task EnableAsync(Guid id, CancellationToken ct)
        {
            var entity = await _repo.GetByIdAsync(id, ct).ConfigureAwait(false) ?? throw new KeyNotFoundException($"AuthProfile {id} not found");
            entity.Enable();
            await _repo.UpdateAsync(entity, ct).ConfigureAwait(false);
        }

        public async Task DisableAsync(Guid id, CancellationToken ct)
        {
            var entity = await _repo.GetByIdAsync(id, ct).ConfigureAwait(false) ?? throw new KeyNotFoundException($"AuthProfile {id} not found");
            entity.Disable();
            await _repo.UpdateAsync(entity, ct).ConfigureAwait(false);
        }

        public async Task<AuthProfileDto?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var e = await _repo.GetByIdAsync(id, ct).ConfigureAwait(false);
            return e?.ToDto();
        }

        public async Task<(IEnumerable<AuthProfileDto> Items, int Total)> ListAsync(GetAuthProfilesQuery q, CancellationToken ct)
        {
            var (items, total) = await _repo.GetPagedAsync(q.Page, q.PageSize, q.Enabled, q.ProjectId, q.ServiceId, q.Env, ct).ConfigureAwait(false);
            return (items.Select(i => i.ToDto()), total);
        }
    }
}
