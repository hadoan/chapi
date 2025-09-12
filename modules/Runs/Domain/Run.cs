using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ShipMvp.Core.Entities;
using ShipMvp.Core.Abstractions;

namespace Runs.Domain;

public enum RunStatus { Pending, Running, Passed, Failed, Cancelled }

public class Run : Entity<Guid>
{
    public Guid? ProjectId { get; private set; }
    public string SuiteName { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public RunStatus Status { get; private set; } = RunStatus.Pending;
    public string Actor { get; private set; } = string.Empty;
    public string Trigger { get; private set; } = "Manual";
    public string? IrPath { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public string? Error { get; private set; }

    private readonly List<RunStep> _steps = new();
    public IReadOnlyCollection<RunStep> Steps => _steps;

    private Run() : base(Guid.Empty) { }
    
    private Run(Guid id, Guid? projectId, string suiteName, string version, string actor, string trigger) 
        : base(id)
    {
        ProjectId = projectId;
        SuiteName = suiteName;
        Version = version;
        Actor = actor;
        Trigger = trigger;
        CreatedAt = DateTime.UtcNow;
    }

    public static Run New(Guid? projectId, string suiteName, string version, string actor, string trigger = "Manual")
    {
        return new Run(Guid.NewGuid(), projectId, suiteName, version, actor, trigger);
    }

    public void AttachIr(string irPath)
    {
        IrPath = irPath;
    }

    public void MarkRunning()
    {
        if (Status == RunStatus.Pending)
        {
            Status = RunStatus.Running;
            StartedAt = DateTime.UtcNow;
        }
    }

    public void Complete(bool success, string? error = null)
    {
        if (Status == RunStatus.Running)
        {
            Status = success ? RunStatus.Passed : RunStatus.Failed;
            FinishedAt = DateTime.UtcNow;
            Error = error;
        }
    }

    public void Cancel()
    {
        if (Status is RunStatus.Pending or RunStatus.Running)
        {
            Status = RunStatus.Cancelled;
            FinishedAt = DateTime.UtcNow;
        }
    }

    public void AddStep(RunStep step)
    {
        _steps.Add(step);
    }
}

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

public class RunEvent : Entity<Guid>
{
    public Guid RunId { get; private set; }
    public string? StepId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string Payload { get; private set; } = "{}";

    private RunEvent() : base(Guid.Empty) { }

    private RunEvent(Guid id, Guid runId, string? stepId, string kind, object? payload) : base(id)
    {
        RunId = runId;
        StepId = stepId;
        Kind = kind;
        Payload = payload != null ? JsonSerializer.Serialize(payload) : "{}";
        CreatedAt = DateTime.UtcNow;
    }

    public static RunEvent New(Guid runId, string? stepId, string kind, object? payload = null)
    {
        return new RunEvent(Guid.NewGuid(), runId, stepId, kind, payload);
    }

    // Helper factory methods
    public static RunEvent Created(Guid runId, string actor) =>
        New(runId, null, "RunCreated", new { actor });

    public static RunEvent Started(Guid runId) =>
        New(runId, null, "RunStarted", null);

    public static RunEvent StepStarted(Guid runId, string stepId) =>
        New(runId, stepId, "StepStarted", null);

    public static RunEvent StepCompleted(Guid runId, string stepId, bool ok, int? status, long? durationMs, string? requestKey, string? responseKey) =>
        New(runId, stepId, "StepCompleted", new
        {
            ok,
            status,
            durationMs,
            artifacts = new { request = requestKey, response = responseKey }
        });

    public static RunEvent Completed(Guid runId, bool success, string? error = null) =>
        New(runId, null, "RunCompleted", new { success, error });

    public static RunEvent Log(Guid runId, string? stepId, string message, string level = "Info") =>
        New(runId, stepId, "Log", new { message, level });
}

public interface IRunRepository : IRepository<Run, Guid>
{
    Task<(IEnumerable<Run> Items, int Total)> GetPagedAsync(int page, int pageSize, Guid? projectId = null, RunStatus? status = null, CancellationToken cancellationToken = default);
    Task<Run?> GetWithStepsAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IRunEventRepository : IRepository<RunEvent, Guid>
{
    Task<IEnumerable<RunEvent>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default);
    Task AppendEventAsync(RunEvent runEvent, CancellationToken cancellationToken = default);
}
