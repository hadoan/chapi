using Environments.Application.Dtos;

namespace Environments.Application;

public interface IEnvironmentAppService
{
    Task<EnvironmentDto> CreateAsync(CreateEnvironmentRequest request, CancellationToken ct = default);
    Task<EnvironmentDto> CreateForProjectAsync(Guid projectId, CreateEnvironmentRequest request, CancellationToken ct = default);
    Task<EnvironmentDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EnvironmentDto?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IEnumerable<EnvironmentDto>> GetListAsync(GetEnvironmentsQuery query, CancellationToken ct = default);
    Task<IEnumerable<EnvironmentDto>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<int> GetCountAsync(GetEnvironmentsQuery query, CancellationToken ct = default);
    Task<EnvironmentDto> UpdateAsync(Guid id, UpdateEnvironmentRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
