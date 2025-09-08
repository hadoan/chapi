using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AuthProfiles.Domain
{
    public interface IAuthProfileRepository : ShipMvp.Core.Abstractions.IRepository<AuthProfile, Guid>
    {
        Task<IEnumerable<AuthProfile>> GetByEnabledAsync(bool enabled, CancellationToken ct);

        Task<(IEnumerable<AuthProfile> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool? enabled, Guid? projectId, Guid? serviceId, string? env, CancellationToken ct);

        Task<AuthProfile?> GetWithSecretsAsync(Guid id, CancellationToken ct);
    }
}
