using Microsoft.EntityFrameworkCore;
using Policies.Application.Dtos;
using Policies.Application.Mappings;
using Policies.Application.Requests;
using Policies.Domain;

namespace Policies.Application.Services;

public class PolicyService : IPolicyService
{
    private readonly IPolicyRepository _repo;
    public PolicyService(IPolicyRepository repo) => _repo = repo;

    public async Task<PolicyDto> SaveAsync(SavePolicyRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<PolicyScope>(request.Scope, true, out var scope)) throw new ArgumentException("Invalid scope", nameof(request.Scope));
        var policy = Policy.Create(scope, request.ProjectId, request.Format, request.Document, request.EffectiveAt);
        await _repo.AddAsync(policy, ct);
        return policy.ToDto();
    }

    public async Task<PolicyDto> SaveMinimalFlagsAsync(SaveMinimalFlagsRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<PolicyScope>(request.Scope, true, out var scope)) throw new ArgumentException("Invalid scope", nameof(request.Scope));
        // Build JSON doc from flags (simple, naive)
        var doc = System.Text.Json.JsonSerializer.Serialize(new
        {
            prodGuard = request.ProdGuard,
            failOnDrift = request.FailOnDrift,
            maxRetries = request.MaxRetries,
            redactionEnabled = request.RedactionEnabled
        });
        var policy = Policy.Create(scope, request.ProjectId, "json", doc, null);
        await _repo.AddAsync(policy, ct);
        return policy.ToDto();
    }

    public async Task<PolicyDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(id, ct);
        return p?.ToDto();
    }

    public async Task<PolicyDto?> GetEffectiveAsync(string scope, Guid? projectId, DateTime? asOf, CancellationToken ct)
    {
        if (!Enum.TryParse<PolicyScope>(scope, true, out var ps)) throw new ArgumentException("Invalid scope", nameof(scope));
        var p = await _repo.GetEffectiveAsync(ps, projectId, asOf ?? DateTime.UtcNow, ct);
        return p?.ToDto();
    }

    public async Task<IEnumerable<PolicyDto>> ListAsync(string? scope, Guid? projectId, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) page = 1; if (pageSize <= 0) pageSize = 50;
        var query = _repo.Query();
        if (!string.IsNullOrWhiteSpace(scope) && Enum.TryParse<PolicyScope>(scope, true, out var sc)) query = query.Where(p => p.Scope == sc);
        if (projectId.HasValue) query = query.Where(p => p.ProjectId == projectId);
        return await query.OrderByDescending(p => p.EffectiveAt).Skip((page - 1) * pageSize).Take(pageSize).Select(p => p.ToDto()).ToListAsync(ct);
    }
}
