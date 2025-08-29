using Orchestration.Domain;

namespace Orchestration.Application.Services;

public class OrchestrationAppService : IOrchestrationAppService
{
    private readonly IRunScheduler _scheduler; private readonly IRunExecutor _executor;
    public OrchestrationAppService(IRunScheduler scheduler, IRunExecutor executor) { _scheduler = scheduler; _executor = executor; }
    public Task<Guid> QueueAsync(Guid suiteId, CancellationToken ct) => _scheduler.QueueRunAsync(suiteId, ct);
    public Task StartAsync(Guid runId, CancellationToken ct) => _executor.StartAsync(runId, ct);
    public Task CompleteAsync(Guid runId, bool success, CancellationToken ct) => _executor.CompleteAsync(runId, success, ct);
}
