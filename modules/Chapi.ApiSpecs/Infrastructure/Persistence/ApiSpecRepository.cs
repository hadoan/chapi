using Microsoft.EntityFrameworkCore;
using Chapi.ApiSpecs.Domain;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Persistence;

namespace Chapi.ApiSpecs.Infrastructure.Persistence;

[UnitOfWork]
public class ApiSpecRepository : IApiSpecRepository
{
    private readonly IDbContext _context;
    private readonly DbSet<ApiSpec> _dbSet;

    public ApiSpecRepository(IDbContext context)
    {
        _context = context;
        _dbSet = context.Set<ApiSpec>();
    }

    public async Task<ApiSpec?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ApiSpec>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<ApiSpec> AddAsync(ApiSpec entity, CancellationToken cancellationToken = default)
    {
        var entry = await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }

    public async Task<ApiSpec> UpdateAsync(ApiSpec entity, CancellationToken cancellationToken = default)
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

    public async Task<ApiSpec?> FindByShaAsync(string sha)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.Sha256 == sha);
    }

    public async Task<Guid> UpsertAsync(ApiSpec spec)
    {
        var existing = await _dbSet.FirstOrDefaultAsync(x => x.Sha256 == spec.Sha256);
        if (existing != null) return existing.Id;

        await _dbSet.AddAsync(spec);
        await _context.SaveChangesAsync();
        return spec.Id;
    }
}
