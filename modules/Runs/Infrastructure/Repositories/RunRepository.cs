using Microsoft.EntityFrameworkCore;
using Runs.Domain;
using ShipMvp.Core.Persistence;

namespace Runs.Infrastructure.Repositories;

public class RunRepository : IRunRepository
{
    private readonly IDbContext _db;
    private readonly DbSet<Run> _set;

    public RunRepository(IDbContext db)
    {
        _db = db;
        _set = db.Set<Run>();
    }

    public async Task<Run?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _set.FindAsync(new object?[] { id }, cancellationToken).AsTask();
    }

    public async Task<Run?> GetWithStepsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _set
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Run>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _set.ToListAsync(cancellationToken);
    }

    public async Task<Run> AddAsync(Run entity, CancellationToken cancellationToken = default)
    {
        await _set.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Run> UpdateAsync(Run entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _set.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _set.FindAsync(new object?[] { id }, cancellationToken).AsTask();
        if (entity != null)
        {
            _set.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<(IEnumerable<Run> Items, int Total)> GetPagedAsync(
        int page, 
        int pageSize, 
        Guid? projectId = null, 
        RunStatus? status = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _set.AsQueryable();

        if (projectId.HasValue)
            query = query.Where(r => r.ProjectId == projectId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}