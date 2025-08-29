using ShipMvp.Core.Abstractions;

namespace Projects.Domain;

/// <summary>
/// Repository interface for Project aggregate root.
/// </summary>
public interface IProjectRepository : IRepository<Project, Guid>
{
    Task<IEnumerable<Project>> GetByStatusAsync(ProjectStatus status, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Project> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, ProjectStatus? status = null, string? search = null, CancellationToken cancellationToken = default);
}
