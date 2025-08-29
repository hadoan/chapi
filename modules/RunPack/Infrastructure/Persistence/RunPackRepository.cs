using Microsoft.EntityFrameworkCore;
using ShipMvp.Core.Persistence;

namespace RunPack.Infrastructure.Persistence;

public class RunPackRepository : RunPack.Domain.IRunPackRepository
{
    private readonly IDbContext _db; private readonly DbSet<RunPack.Domain.RunPack> _set;
    public RunPackRepository(IDbContext db) { _db = db; _set = db.Set<RunPack.Domain.RunPack>(); }
    public IQueryable<RunPack.Domain.RunPack> Query() => _set.AsQueryable();
    public async Task<RunPack.Domain.RunPack?> GetByIdAsync(Guid id, CancellationToken ct) => await _set.FindAsync(new object?[] { id }, ct).AsTask();
    public async Task AddAsync(RunPack.Domain.RunPack entity, CancellationToken ct) { await _set.AddAsync(entity, ct); await _db.SaveChangesAsync(ct); }
    public Task UpdateAsync(RunPack.Domain.RunPack entity, CancellationToken ct) { _set.Update(entity); return _db.SaveChangesAsync(ct); }
    public Task DeleteAsync(RunPack.Domain.RunPack entity, CancellationToken ct) { _set.Remove(entity); return _db.SaveChangesAsync(ct); }
    public async Task<int> GetLatestVersionAsync(Guid projectId, Guid suiteId, CancellationToken ct) => await _set.Where(p => p.ProjectId == projectId && p.SuiteId == suiteId).Select(p => p.Version).DefaultIfEmpty(0).MaxAsync(ct);
}
