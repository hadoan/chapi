using System;
using System.Collections.Generic;
using System.Linq;
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
