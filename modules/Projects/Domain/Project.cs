using ShipMvp.Core.Entities;

namespace Projects.Domain;

/// <summary>
/// Status values for a Project aggregate.
/// </summary>
public enum ProjectStatus
{
    Draft,
    Active,
    OnHold,
    Completed,
    Archived
}

/// <summary>
/// Child task entity belonging to a Project aggregate.
/// </summary>
public class ProjectTask : Entity<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid ProjectId { get; set; }
    public bool IsDone { get; set; }

    private ProjectTask() : base(Guid.Empty) { }

    public ProjectTask(Guid id, string title, string? description, Guid projectId) : base(id)
    {
        Title = title;
        Description = description;
        ProjectId = projectId;
    }
}

/// <summary>
/// Project aggregate root representing a deliverable with tasks.
/// </summary>
public class Project : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Draft;
    public List<ProjectTask> Tasks { get; private set; } = new();

    private Project() : base(Guid.Empty) { }

    public Project(Guid id, string name, string? description = null) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));
        Name = name.Trim();
        Description = description;
        Status = ProjectStatus.Draft;
    }

    public static Project Create(string name, string? description = null)
        => new(Guid.NewGuid(), name, description);

    public Project Activate()
    {
        if (Status != ProjectStatus.Draft && Status != ProjectStatus.OnHold)
            throw new InvalidOperationException($"Cannot activate project from state {Status}");
        Status = ProjectStatus.Active;
        StartDate ??= DateTime.UtcNow;
        return this;
    }

    public Project Complete()
    {
        if (Status != ProjectStatus.Active && Status != ProjectStatus.OnHold)
            throw new InvalidOperationException("Only active or on-hold projects can be completed");
        Status = ProjectStatus.Completed;
        EndDate = DateTime.UtcNow;
        return this;
    }

    public Project Hold()
    {
        if (Status != ProjectStatus.Active)
            throw new InvalidOperationException("Only active projects can be put on hold");
        Status = ProjectStatus.OnHold;
        return this;
    }

    public Project Archive()
    {
        if (Status is not ProjectStatus.Completed and not ProjectStatus.Draft and not ProjectStatus.OnHold)
            throw new InvalidOperationException("Only draft, completed or on-hold projects can be archived");
        Status = ProjectStatus.Archived;
        return this;
    }

    public Project UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));
        Name = name.Trim();
        Description = description;
        return this;
    }

    public ProjectTask AddTask(string title, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title required", nameof(title));
        var task = new ProjectTask(Guid.NewGuid(), title.Trim(), description, Id);
        Tasks.Add(task);
        return task;
    }
}
