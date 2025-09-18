using ShipMvp.Core.Entities;

namespace Runs.Domain;

public class RunStep : Entity<Guid>
{
    public Guid RunId { get; private set; }
    public string StepId { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public RunStatus Status { get; private set; } = RunStatus.Pending;
    public int? StatusCode { get; private set; }
    public int? DurationMs { get; private set; }
    public string? Error { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }

    private RunStep() : base(Guid.Empty) { }

    private RunStep(Guid id, Guid runId, string stepId, string type, int order) : base(id)
    {
        RunId = runId;
        StepId = stepId;
        Type = type;
        Order = order;
        CreatedAt = DateTime.UtcNow;
    }

    public static RunStep Create(Guid runId, string stepId, string type, int order)
    {
        return new RunStep(Guid.NewGuid(), runId, stepId, type, order);
    }

    public void Start()
    {
        Status = RunStatus.Running;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(bool success, int? statusCode = null, int? durationMs = null, string? error = null)
    {
        Status = success ? RunStatus.Passed : RunStatus.Failed;
        StatusCode = statusCode;
        DurationMs = durationMs;
        Error = error;
        FinishedAt = DateTime.UtcNow;
    }
}
