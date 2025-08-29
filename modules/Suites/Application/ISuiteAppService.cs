using Suites.Application.Dtos;

namespace Suites.Application;

public interface ISuiteAppService
{
    Task<SuiteDto> CreateAsync(CreateSuiteRequest request, CancellationToken cancellationToken = default);
    Task<SuiteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SuiteDto>> GetListAsync(GetSuitesQuery query, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(GetSuitesQuery query, CancellationToken cancellationToken = default);
    Task<SuiteDto> UpdateAsync(Guid id, UpdateSuiteRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SuiteDto> ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SuiteDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
