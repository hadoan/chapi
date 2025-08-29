using Microsoft.EntityFrameworkCore;
using Runs.Domain;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Abstractions;

namespace Runs.Infrastructure.Data;

[UnitOfWork]
public class RunRepository : IRunRepository
{
    private readonly ShipMvp.Core.Persistence.IDbContext _context;
    private readonly DbSet<Run> _dbSet;
    public RunRepository(ShipMvp.Core.Persistence.IDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Run>();
    }
    public async Task<Run?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet.Include("_steps").FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    public async Task<IEnumerable<Run>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet.Include("_steps").OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    public async Task<Run> AddAsync(Run entity, CancellationToken cancellationToken = default)
    { var e = await _dbSet.AddAsync(entity, cancellationToken); await _context.SaveChangesAsync(cancellationToken); return e.Entity; }
    public async Task<Run> UpdateAsync(Run entity, CancellationToken cancellationToken = default)
    { var e = _dbSet.Update(entity); await _context.SaveChangesAsync(cancellationToken); return e.Entity; }
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    { var existing = await GetByIdAsync(id, cancellationToken); if (existing != null) { _dbSet.Remove(existing); await _context.SaveChangesAsync(cancellationToken);} }
    public async Task<(IEnumerable<Run> Items, int Total)> GetPagedAsync(int page, int pageSize, Guid? suiteId = null, RunStatus? status = null, CancellationToken cancellationToken = default)
    {
        var q = _dbSet.Include("_steps").AsQueryable();
        if (suiteId.HasValue) q = q.Where(r => r.SuiteId == suiteId);
        if (status.HasValue) q = q.Where(r => r.Status == status);
        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(r => r.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
}
