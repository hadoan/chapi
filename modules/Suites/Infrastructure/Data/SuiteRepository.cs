using Microsoft.EntityFrameworkCore;
using Suites.Domain;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Persistence;

namespace Suites.Infrastructure.Data;

[UnitOfWork]
public class SuiteRepository : ISuiteRepository
{
    private readonly IDbContext _context;
    private readonly DbSet<Suite> _dbSet;
    public SuiteRepository(IDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Suite>();
    }

    public async Task<Suite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include("_files")
            .Include("_testCases")
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Suite>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include("_files")
            .Include("_testCases")
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Suite> AddAsync(Suite entity, CancellationToken cancellationToken = default)
    {
        var entry = await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }

    public async Task<Suite> UpdateAsync(Suite entity, CancellationToken cancellationToken = default)
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

    public async Task<(IEnumerable<Suite> Items, int Total)> GetPagedAsync(int page, int pageSize, SuiteStatus? status = null, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include("_files").Include("_testCases").AsQueryable();
        if (status.HasValue) query = query.Where(s => s.Status == status);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(s => s.Name.Contains(search));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }
}
