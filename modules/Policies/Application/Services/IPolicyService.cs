using Policies.Application.Dtos;
using Policies.Application.Requests;

namespace Policies.Application.Services;

public interface IPolicyService
{
    Task<PolicyDto> SaveAsync(SavePolicyRequest request, CancellationToken ct);
    Task<PolicyDto> SaveMinimalFlagsAsync(SaveMinimalFlagsRequest request, CancellationToken ct);
    Task<PolicyDto?> GetAsync(Guid id, CancellationToken ct);
    Task<PolicyDto?> GetEffectiveAsync(string scope, Guid? projectId, DateTime? asOf, CancellationToken ct);
    Task<IEnumerable<PolicyDto>> ListAsync(string? scope, Guid? projectId, int page, int pageSize, CancellationToken ct);
}
