using Integrations.GitHub.Domain;
using Microsoft.EntityFrameworkCore;
using ShipMvp.Core.Persistence;

namespace Integrations.GitHub.Infrastructure.Persistence;

public class GitHubInstallationRepository : IGitHubInstallationRepository
{
    private readonly IDbContext _db; private readonly DbSet<GitHubInstallation> _set;
    public GitHubInstallationRepository(IDbContext db) { _db = db; _set = db.Set<GitHubInstallation>(); }
    public IQueryable<GitHubInstallation> Query() => _set.Include(i => i.Repos).AsQueryable();
    public async Task<GitHubInstallation?> GetByIdAsync(Guid id, CancellationToken ct) => await _set.Include(i => i.Repos).FirstOrDefaultAsync(i => i.Id == id, ct);
    public async Task<GitHubInstallation?> GetByInstallationIdAsync(long installationId, CancellationToken ct) => await _set.Include(i => i.Repos).FirstOrDefaultAsync(i => i.InstallationId == installationId, ct);
    public async Task AddAsync(GitHubInstallation entity, CancellationToken ct) { await _set.AddAsync(entity, ct); await _db.SaveChangesAsync(ct); }
    public Task UpdateAsync(GitHubInstallation entity, CancellationToken ct) { _set.Update(entity); return _db.SaveChangesAsync(ct); }
    public Task DeleteAsync(GitHubInstallation entity, CancellationToken ct) { _set.Remove(entity); return _db.SaveChangesAsync(ct); }
}

public class PrCheckRepository : IPrCheckRepository
{
    private readonly IDbContext _db; private readonly DbSet<PrCheck> _set;
    public PrCheckRepository(IDbContext db) { _db = db; _set = db.Set<PrCheck>(); }
    public IQueryable<PrCheck> Query() => _set.AsQueryable();
    public async Task<PrCheck?> GetByIdAsync(Guid id, CancellationToken ct) => await _set.FindAsync(new object?[] { id }, ct).AsTask();
    public async Task<PrCheck?> GetByRunAsync(Guid runId, CancellationToken ct) => await _set.FirstOrDefaultAsync(c => c.RunId == runId, ct);
    public async Task AddAsync(PrCheck entity, CancellationToken ct) { await _set.AddAsync(entity, ct); await _db.SaveChangesAsync(ct); }
    public Task UpdateAsync(PrCheck entity, CancellationToken ct) { _set.Update(entity); return _db.SaveChangesAsync(ct); }
    public Task DeleteAsync(PrCheck entity, CancellationToken ct) { _set.Remove(entity); return _db.SaveChangesAsync(ct); }
}
