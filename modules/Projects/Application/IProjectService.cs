using Projects.Application.Dtos;

namespace Projects.Application;

public interface IProjectService
{
    Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProjectDto>> GetListAsync(GetProjectsQuery query, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(GetProjectsQuery query, CancellationToken cancellationToken = default);
    Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken = default);
    Task<ProjectDto> ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectDto> HoldAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
