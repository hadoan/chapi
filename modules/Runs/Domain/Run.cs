namespace Runs.Domain;

public enum RunStatus { Queued, Running, Succeeded, Failed, Cancelled }

public class Run : ShipMvp.Core.Entities.Entity<Guid>
{
    public Guid SuiteId { get; private set; }
    public RunStatus Status { get; private set; } = RunStatus.Queued;
    public DateTime? StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    private readonly List<RunStep> _steps = new();
    public IReadOnlyCollection<RunStep> Steps => _steps;

    private Run() : base(Guid.Empty) { }
    private Run(Guid id, Guid suiteId) : base(id) { SuiteId = suiteId; }
    public static Run Queue(Guid suiteId) => new(Guid.NewGuid(), suiteId);
    public void Start() { if (Status == RunStatus.Queued) { Status = RunStatus.Running; StartedAt = DateTime.UtcNow; } }
    public void Succeed() { if (Status == RunStatus.Running) { Status = RunStatus.Succeeded; FinishedAt = DateTime.UtcNow; } }
    public void Fail() { if (Status == RunStatus.Running) { Status = RunStatus.Failed; FinishedAt = DateTime.UtcNow; } }
    public void Cancel() { if (Status is RunStatus.Queued or RunStatus.Running) { Status = RunStatus.Cancelled; FinishedAt = DateTime.UtcNow; } }
    public void AddStep(string name, string? log = null) => _steps.Add(RunStep.Create(Id, name, log));
}

public class RunStep : ShipMvp.Core.Entities.Entity<Guid>
{
    public Guid RunId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Log { get; private set; }
    private RunStep() : base(Guid.Empty) { }
    private RunStep(Guid id, Guid runId, string name, string? log) : base(id) { RunId = runId; Name = name; Log = log; }
    public static RunStep Create(Guid runId, string name, string? log) => new(Guid.NewGuid(), runId, name, log);
}

public interface IRunRepository : ShipMvp.Core.Abstractions.IRepository<Run, Guid>
{
    Task<(IEnumerable<Run> Items, int Total)> GetPagedAsync(int page, int pageSize, Guid? suiteId = null, RunStatus? status = null, CancellationToken cancellationToken = default);
}
