using Microsoft.EntityFrameworkCore;
using Policies.Domain;
using ShipMvp.Core.Persistence;

namespace Policies.Infrastructure.Persistence;

public class PolicyRepository : IPolicyEfRepository
{
    private readonly IDbContext _db;
    private readonly DbSet<Policy> _set;
    public PolicyRepository(IDbContext db)
    {
        _db = db;
        _set = db.Set<Policy>();
    }
    public IQueryable<Policy> Query() => _set.AsQueryable();
    public async Task<Policy?> GetByIdAsync(Guid id, CancellationToken ct) => await _set.FindAsync(new object?[] { id }, ct).AsTask();
    public async Task<IEnumerable<Policy>> GetAllAsync(CancellationToken ct) => await _set.ToListAsync(ct);
    public async Task<Policy> AddAsync(Policy entity, CancellationToken ct) { await _set.AddAsync(entity, ct); await _db.SaveChangesAsync(ct); return entity; }
    public async Task<Policy> UpdateAsync(Policy entity, CancellationToken ct) { _set.Update(entity); await _db.SaveChangesAsync(ct); return entity; }
    public async Task DeleteAsync(Guid id, CancellationToken ct) { var entity = await _set.FindAsync(new object?[] { id }, ct).AsTask(); if (entity is null) return; _set.Remove(entity); await _db.SaveChangesAsync(ct); }
    public async Task<Policy?> GetEffectiveAsync(PolicyScope scope, Guid? projectId, DateTime asOf, CancellationToken ct = default)
    {
        var q = _set.Where(p => p.Scope == scope);
        if (scope == PolicyScope.Project && projectId.HasValue) q = q.Where(p => p.ProjectId == projectId);
        else if (scope == PolicyScope.Project && !projectId.HasValue) return null;
        return await q.Where(p => p.EffectiveAt <= asOf && p.SupersededAt == null)
            .OrderByDescending(p => p.EffectiveAt).FirstOrDefaultAsync(ct);
    }
}
