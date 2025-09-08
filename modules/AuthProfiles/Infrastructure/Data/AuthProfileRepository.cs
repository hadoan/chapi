using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AuthProfiles.Domain;

namespace AuthProfiles.Infrastructure.Data
{
    /// <summary>
    /// EF Core implementation of IAuthProfileRepository.
    /// </summary>
    [ShipMvp.Core.Attributes.UnitOfWork]
    public class AuthProfileRepository : IAuthProfileRepository
    {
        private readonly ShipMvp.Core.Persistence.IDbContext _db;
        private readonly Microsoft.EntityFrameworkCore.DbSet<AuthProfile> _dbSet;

        public AuthProfileRepository(ShipMvp.Core.Persistence.IDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _dbSet = _db.Set<AuthProfile>();
        }
        public async Task<AuthProfile> AddAsync(AuthProfile entity, CancellationToken ct)
        {
            var entry = await _dbSet.AddAsync(entity, ct).ConfigureAwait(false);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return entry.Entity;
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var entity = await _dbSet.FindAsync(new object[] { id }, ct).ConfigureAwait(false);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
        }

        public async Task<AuthProfile?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _db.Set<AuthProfile>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
        }

        public async Task<AuthProfile?> GetWithSecretsAsync(Guid id, CancellationToken ct)
        {
            return await _db.Set<AuthProfile>().Include(x => x.SecretRefs).FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
        }

        public async Task<IEnumerable<AuthProfile>> GetAllAsync(CancellationToken ct)
        {
            return await _dbSet.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        }

        public async Task<IEnumerable<AuthProfile>> GetByEnabledAsync(bool enabled, CancellationToken ct)
        {
            return await _db.Set<AuthProfile>().Where(x => x.Enabled == enabled).ToListAsync(ct).ConfigureAwait(false);
        }

        public async Task<(IEnumerable<AuthProfile> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool? enabled, Guid? projectId, Guid? serviceId, string? env, CancellationToken ct)
        {
            var q = _dbSet.AsQueryable();
            if (enabled.HasValue) q = q.Where(x => x.Enabled == enabled.Value);
            if (projectId.HasValue) q = q.Where(x => x.ProjectId == projectId.Value);
            if (serviceId.HasValue) q = q.Where(x => x.ServiceId == serviceId.Value);
            if (!string.IsNullOrWhiteSpace(env)) q = q.Where(x => x.EnvironmentKey == env);

            var total = await q.CountAsync(ct).ConfigureAwait(false);
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).Include(x => x.SecretRefs).ToListAsync(ct).ConfigureAwait(false);
            return (items, total);
        }

        public async Task<AuthProfile> UpdateAsync(AuthProfile entity, CancellationToken ct)
        {
            var entry = _dbSet.Update(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return entry.Entity;
        }
    }
}
