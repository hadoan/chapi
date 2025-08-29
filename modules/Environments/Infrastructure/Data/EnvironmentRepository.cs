using Microsoft.EntityFrameworkCore;
using Environments.Domain;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Persistence;

namespace Environments.Infrastructure.Data;

[UnitOfWork]
public class EnvironmentRepository : IEnvironmentRepository
{
    private readonly IDbContext _context;
    private readonly DbSet<Environments.Domain.Environment> _dbSet;

    public EnvironmentRepository(IDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Environments.Domain.Environment>();
    }

    public async Task<Environments.Domain.Environment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await IncludeParts(_dbSet).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IEnumerable<Environments.Domain.Environment>> GetAllAsync(CancellationToken cancellationToken = default)
        => await IncludeParts(_dbSet).OrderByDescending(e => e.CreatedAt).ToListAsync(cancellationToken);

    public async Task<Environments.Domain.Environment> AddAsync(Environments.Domain.Environment entity, CancellationToken cancellationToken = default)
    {
        var entry = await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }

    public async Task<Environments.Domain.Environment> UpdateAsync(Environments.Domain.Environment entity, CancellationToken cancellationToken = default)
    {
        var entry = _dbSet.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken);
        if (existing != null)
        {
            _dbSet.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<Environments.Domain.Environment?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => await IncludeParts(_dbSet).FirstOrDefaultAsync(e => e.Name == name, cancellationToken);

    public async Task<(IEnumerable<Environments.Domain.Environment> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search = null, Guid? projectId = null, CancellationToken cancellationToken = default)
    {
        var query = IncludeParts(_dbSet).AsQueryable();
        if (projectId.HasValue) query = query.Where(e => e.ProjectId == projectId.Value);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(e => e.Name.Contains(search));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IEnumerable<Environments.Domain.Environment>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
        => await IncludeParts(_dbSet).Where(e => e.ProjectId == projectId).OrderByDescending(e => e.CreatedAt).ToListAsync(cancellationToken);

    private static IQueryable<Environments.Domain.Environment> IncludeParts(DbSet<Environments.Domain.Environment> set) => set
        .Include(e => e.Headers)
        .Include(e => e.Secrets);
}
