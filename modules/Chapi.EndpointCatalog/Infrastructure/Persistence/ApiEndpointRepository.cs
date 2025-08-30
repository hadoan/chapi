using Microsoft.EntityFrameworkCore;
using Chapi.EndpointCatalog.Domain;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Persistence;

namespace Chapi.EndpointCatalog.Infrastructure.EntityFrameworkCore;

[UnitOfWork]
public class ApiEndpointRepository : IApiEndpointRepository
{
    private readonly IDbContext _context;
    private readonly DbSet<ApiEndpoint> _dbSet;

    public ApiEndpointRepository(IDbContext context)
    {
        _context = context;
        _dbSet = context.Set<ApiEndpoint>();
    }

    public async Task UpsertAsync(Guid specId, Guid projectId, EndpointDto dto)
    {
        var existing = await _dbSet.FirstOrDefaultAsync(x => x.SpecId == specId && x.Method == dto.Method && x.Path == dto.Path);
        if (existing is null)
        {
            await _dbSet.AddAsync(ApiEndpoint.Create(Guid.NewGuid(), projectId, specId, dto));
        }
        else
        {
            existing = ApiEndpoint.Create(existing.Id, projectId, specId, dto);
            _context.Entry(existing).State = EntityState.Modified;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<List<ApiEndpoint>> ListByProjectAsync(Guid projectId, string? tag, string? search)
    {
        var q = _dbSet.Where(x => x.ProjectId == projectId);
        if (!string.IsNullOrWhiteSpace(tag)) q = q.Where(x => x.Tags.Contains(tag));
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Path.Contains(search) || (x.Summary != null && x.Summary.Contains(search)));
        return await q.OrderBy(x => x.Path).ThenBy(x => x.Method).ToListAsync();
    }

    public Task<ApiEndpoint?> FindByMethodPathAsync(Guid projectId, string method, string path) =>
        _dbSet.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Method == method && x.Path == path);
}
