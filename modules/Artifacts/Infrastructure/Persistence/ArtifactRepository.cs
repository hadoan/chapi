using Artifacts.Domain;
using Microsoft.EntityFrameworkCore;
using ShipMvp.Core.Persistence;

namespace Artifacts.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of artifact repository.
/// </summary>
public class ArtifactRepository : IArtifactEfRepository
{
    private readonly IDbContext _db; // Assumes abstraction provided by core
    private readonly DbSet<Artifact> _set;
    public ArtifactRepository(IDbContext db)
    {
        _db = db;
        _set = db.Set<Artifact>();
    }
    public IQueryable<Artifact> Query() => _set.AsQueryable();
    public async Task<Artifact?> GetByIdAsync(Guid id, CancellationToken ct) => await _set.FindAsync(new object?[] { id }, ct).AsTask();
    public async Task AddAsync(Artifact entity, CancellationToken ct) { await _set.AddAsync(entity, ct); await _db.SaveChangesAsync(ct); }
    public Task UpdateAsync(Artifact entity, CancellationToken ct) { _set.Update(entity); return _db.SaveChangesAsync(ct); }
    public Task DeleteAsync(Artifact entity, CancellationToken ct) { _set.Remove(entity); return _db.SaveChangesAsync(ct); }
    public async Task<IEnumerable<Artifact>> ListByRunAsync(Guid runId, ArtifactKind? kind = null, CancellationToken ct = default)
    {
        var q = _set.Where(a => a.RunId == runId);
        if (kind.HasValue) q = q.Where(a => a.Kind == kind.Value);
        return await q.OrderBy(a => a.CreatedAt).ToListAsync(ct);
    }
}
