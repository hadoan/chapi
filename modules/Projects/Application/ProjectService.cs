using Projects.Application.Dtos;
using Projects.Domain;
using ShipMvp.Core.Attributes;

namespace Projects.Application;

[AutoController(Route = "api/projects")]
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _repository;

    public ProjectService(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = Project.Create(request.Name, request.Description);
        foreach (var t in request.Tasks)
        {
            project.AddTask(t.Title, t.Description);
        }
        var created = await _repository.AddAsync(project, cancellationToken);
        return Map(created);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await _repository.DeleteAsync(id, cancellationToken);

    public async Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity == null ? null : Map(entity);
    }

    public async Task<IEnumerable<ProjectDto>> GetListAsync(GetProjectsQuery query, CancellationToken cancellationToken = default)
    {
        ProjectStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<ProjectStatus>(query.Status, true, out var s))
            status = s;
        var (items, _) = await _repository.GetPagedAsync(query.Page, query.PageSize, status, query.Search, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<int> GetCountAsync(GetProjectsQuery query, CancellationToken cancellationToken = default)
    {
        ProjectStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<ProjectStatus>(query.Status, true, out var s))
            status = s;
        var (_, total) = await _repository.GetPagedAsync(query.Page, query.PageSize, status, query.Search, cancellationToken);
        return total;
    }

    public async Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Project not found");
        entity.UpdateDetails(request.Name, request.Description);
        entity.Tasks.Clear();
        foreach (var t in request.Tasks)
            entity.AddTask(t.Title, t.Description);
        var updated = await _repository.UpdateAsync(entity, cancellationToken);
        return Map(updated);
    }

    public async Task<ProjectDto> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
        => await Transition(id, p => p.Activate(), cancellationToken);

    public async Task<ProjectDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await Transition(id, p => p.Complete(), cancellationToken);

    public async Task<ProjectDto> HoldAsync(Guid id, CancellationToken cancellationToken = default)
        => await Transition(id, p => p.Hold(), cancellationToken);

    public async Task<ProjectDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
        => await Transition(id, p => p.Archive(), cancellationToken);

    private async Task<ProjectDto> Transition(Guid id, Func<Project, Project> action, CancellationToken ct)
    {
        var e = await _repository.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Project not found");
        action(e);
        var updated = await _repository.UpdateAsync(e, ct);
        return Map(updated);
    }

    private static ProjectDto Map(Project p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Status = p.Status.ToString(),
        CreatedAt = p.CreatedAt,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        Tasks = p.Tasks.Select(t => new ProjectTaskDto(t.Id, t.Title, t.Description, t.IsDone)).ToList()
    };
}
