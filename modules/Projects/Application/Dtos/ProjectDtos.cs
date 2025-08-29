namespace Projects.Application.Dtos;

public record ProjectTaskDto(Guid Id, string Title, string? Description, bool IsDone);

public record ProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public IReadOnlyList<ProjectTaskDto> Tasks { get; init; } = Array.Empty<ProjectTaskDto>();
}

public record CreateProjectRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<CreateProjectTaskRequest> Tasks { get; init; } = new();
}

public record CreateProjectTaskRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public record UpdateProjectRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<CreateProjectTaskRequest> Tasks { get; init; } = new();
}

public record GetProjectsQuery
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
