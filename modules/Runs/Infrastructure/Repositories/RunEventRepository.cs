using Microsoft.EntityFrameworkCore;
using Runs.Domain;
using ShipMvp.Core.Persistence;

namespace Runs.Infrastructure.Repositories;

public class RunEventRepository : IRunEventRepository
{
    private readonly IDbContext _db;
    private readonly DbSet<RunEvent> _set;

    public RunEventRepository(IDbContext db)
    {
        _db = db;
        _set = db.Set<RunEvent>();
    }

    public async Task<RunEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _set.FindAsync(new object?[] { id }, cancellationToken).AsTask();
    }

    public async Task<IEnumerable<RunEvent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _set.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RunEvent>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        return await _set
            .Where(e => e.RunId == runId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<RunEvent> AddAsync(RunEvent entity, CancellationToken cancellationToken = default)
    {
        await _set.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task AppendEventAsync(RunEvent runEvent, CancellationToken cancellationToken = default)
    {
        await _set.AddAsync(runEvent, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<RunEvent> UpdateAsync(RunEvent entity, CancellationToken cancellationToken = default)
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
}