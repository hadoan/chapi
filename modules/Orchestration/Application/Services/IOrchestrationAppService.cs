namespace Orchestration.Application.Services;

public interface IOrchestrationAppService
{
    Task<Guid> QueueAsync(Guid suiteId, CancellationToken ct);
    Task StartAsync(Guid runId, CancellationToken ct);
    Task CompleteAsync(Guid runId, bool success, CancellationToken ct);
}
