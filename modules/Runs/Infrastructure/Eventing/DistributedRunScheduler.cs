using Runs.Application.Contracts.Events;
using Runs.Application.Ports;
using Runs.Domain;
using ShipMvp.Core.EventBus;

namespace Runs.Infrastructure.Eventing;

public sealed class DistributedRunScheduler : IRunScheduler
{
    private readonly IDistributedEventBus _bus;

    public DistributedRunScheduler(IDistributedEventBus bus) => _bus = bus;

    public async Task EnqueueAsync(Run run, CancellationToken ct = default)
    {
        var eto = new RunRequestedEto
        {
            RunId = run.Id,
            ProjectId = run.ProjectId,
            IrPath = run.IrPath!,   // e.g., runs/{runId}/suite.json
            Actor = run.Actor,
            Trigger = run.Trigger,
            OrderingKey = run.ProjectId?.ToString()
        };
        await _bus.PublishAsync(eto); // maps to GCP Pub/Sub by provider
    }
}