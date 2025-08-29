using Environments.Application.Dtos;
using Environments.Domain;
using ShipMvp.Core.Attributes;

namespace Environments.Application;

[AutoController(Route = "api/environments")]
public class EnvironmentAppService : IEnvironmentAppService
{
    private readonly IEnvironmentRepository _repository;

    public EnvironmentAppService(IEnvironmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<EnvironmentDto> CreateAsync(CreateEnvironmentRequest request, CancellationToken ct = default)
    {
        var env = Domain.Environment.Create(request.Name, request.BaseUrl, request.TimeoutMs, request.FollowRedirects)
            .SetHeaders(request.Headers.Select(h => (h.Key, h.Value)));
        var created = await _repository.AddAsync(env, ct);
        return Map(created);
    }

    public async Task<EnvironmentDto> CreateForProjectAsync(Guid projectId, CreateEnvironmentRequest request, CancellationToken ct = default)
    {
        var env = Domain.Environment.Create(request.Name, request.BaseUrl, request.TimeoutMs, request.FollowRedirects)
            .SetHeaders(request.Headers.Select(h => (h.Key, h.Value)));
        // Set project id via reflection on aggregate (since ctor doesn't accept it yet)
        // We'll set the property via a simple assignment using InitOnly via runtime (assigning private setter)
        var createdEntity = await _repository.AddAsync(env, ct);
        // Assign ProjectId if not already set - repository persists it via EF mapping if present
        // Note: domain had ProjectId private setter; set via reflection
        var pi = typeof(Domain.Environment).GetProperty("ProjectId");
        if (pi != null) pi.SetValue(createdEntity, projectId);
        var updated = await _repository.UpdateAsync(createdEntity, ct);
        return Map(updated);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _repository.DeleteAsync(id, ct);

    public async Task<EnvironmentDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var e = await _repository.GetByIdAsync(id, ct);
        return e == null ? null : Map(e);
    }

    public async Task<EnvironmentDto?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var e = await _repository.GetByNameAsync(name, ct);
        return e == null ? null : Map(e);
    }

    public async Task<IEnumerable<EnvironmentDto>> GetListAsync(GetEnvironmentsQuery query, CancellationToken ct = default)
    {
            var (items, _) = await _repository.GetPagedAsync(query.Page, query.PageSize, query.Search, null, ct);
        return items.Select(Map).ToList();
    }

    public async Task<int> GetCountAsync(GetEnvironmentsQuery query, CancellationToken ct = default)
    {
            var (_, total) = await _repository.GetPagedAsync(query.Page, query.PageSize, query.Search, null, ct);
        return total;
    }

    public async Task<IEnumerable<EnvironmentDto>> GetByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var items = await _repository.GetByProjectIdAsync(projectId, ct);
        return items.Select(Map).ToList();
    }

    public async Task<EnvironmentDto> UpdateAsync(Guid id, UpdateEnvironmentRequest request, CancellationToken ct = default)
    {
        var e = await _repository.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Environment not found");
        e.Update(request.BaseUrl, request.TimeoutMs, request.FollowRedirects)
            .SetHeaders(request.Headers.Select(h => (h.Key, h.Value)));
        var updated = await _repository.UpdateAsync(e, ct);
        return Map(updated);
    }

    private static EnvironmentDto Map(Domain.Environment e) => new()
    {
        Id = e.Id,
        Name = e.Name,
    ProjectId = e.ProjectId,
        BaseUrl = e.BaseUrl,
        TimeoutMs = e.TimeoutMs,
        FollowRedirects = e.FollowRedirects,
        CreatedAt = e.CreatedAt,
        Headers = e.Headers.Select(h => new EnvironmentHeaderDto(h.Key, h.Value)).ToList(),
        Secrets = e.Secrets.Select(s => new SecretRefDto(s.Provider, s.KeyPath, s.LastRotatedAt, Mask(s.KeyPath))).ToList()
    };

    private static string Mask(string keyPath)
    {
        if (string.IsNullOrEmpty(keyPath)) return string.Empty;
        return keyPath.Length <= 4 ? new string('*', keyPath.Length) : new string('*', keyPath.Length - 4) + keyPath[^4..];
    }
}
