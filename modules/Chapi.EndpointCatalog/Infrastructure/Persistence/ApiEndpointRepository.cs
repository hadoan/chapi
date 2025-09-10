using Microsoft.EntityFrameworkCore;
using Chapi.EndpointCatalog.Domain;
using Chapi.EndpointCatalog.Application;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Persistence;
using System.Text.Json;
using Chapi.ApiSpecs.Domain;

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
            //var updated = ApiEndpoint.Create(existing.Id, projectId, specId, dto);
            var updated = existing;
            updated.UpdatedAt = DateTime.UtcNow;
            updated.OperationId = dto.OperationId;
            updated.Summary = dto.Summary;
            updated.Description = dto.Description;
            updated.Tags = dto.Tags?.ToArray();
            updated.Servers = JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(dto.Servers));
            updated.Security = JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(dto.Security));
            updated.Parameters = JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(dto.Parameters));
            updated.Request = dto.Request != null ? JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(dto.Request)) : null;
            updated.Responses = JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(dto.Responses));
            updated.ComputeDerivedFlags(dto);
            _dbSet.Update(updated);
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

    // IRepository<ApiEndpoint, Guid> implementation
    public Task<ApiEndpoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IEnumerable<ApiEndpoint>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _dbSet.ToListAsync(cancellationToken);
        return list;
    }

    public async Task<ApiEndpoint> AddAsync(ApiEndpoint entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<ApiEndpoint> UpdateAsync(ApiEndpoint entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteBySpecIdAsync(Guid specId)
    {
        // Use server-side delete to remove all endpoints for the spec without loading entities
        await _dbSet.Where(x => x.SpecId == specId).ExecuteDeleteAsync();
        await _context.SaveChangesAsync();
    }

    public Task<ApiEndpoint?> FindAsync(Guid id) => GetByIdAsync(id);
}
