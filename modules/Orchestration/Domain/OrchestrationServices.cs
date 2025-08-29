using Runs.Domain;
using Policies.Domain;

namespace Orchestration.Domain;

public interface IRunScheduler
{
    Task<Guid> QueueRunAsync(Guid suiteId, CancellationToken ct);
}

public interface IRunExecutor
{
    Task StartAsync(Guid runId, CancellationToken ct);
    Task CompleteAsync(Guid runId, bool success, CancellationToken ct);
}

public interface IFlakeClassifier
{
    bool IsFlaky(Guid runId, IEnumerable<string> logs);
}

public class SimpleRunScheduler : IRunScheduler
{
    // TODO: inject repositories: Runs, Policies for region/concurrency decisions
    public Task<Guid> QueueRunAsync(Guid suiteId, CancellationToken ct) => Task.FromResult(Guid.NewGuid()); // placeholder creating id (should call Run repository)
}

public class SimpleRunExecutor : IRunExecutor
{
    public Task StartAsync(Guid runId, CancellationToken ct) => Task.CompletedTask; // TODO: load run, mark start
    public Task CompleteAsync(Guid runId, bool success, CancellationToken ct) => Task.CompletedTask; // TODO: mark success/fail, publish events
}

public class NaiveFlakeClassifier : IFlakeClassifier
{
    public bool IsFlaky(Guid runId, IEnumerable<string> logs) => logs.Any(l => l.Contains("timeout", StringComparison.OrdinalIgnoreCase));
}
