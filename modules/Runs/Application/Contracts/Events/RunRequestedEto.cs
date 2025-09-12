using ShipMvp.Core.EventBus;

namespace Runs.Application.Contracts.Events;

public sealed class RunRequestedEto : IDistributedEvent
{
    public Guid RunId { get; init; }
    public Guid? ProjectId { get; init; }
    public string IrPath { get; init; } = default!; // storage key (not full gs://)
    public string Actor { get; init; } = default!;
    public string Trigger { get; init; } = "Manual";
    public string? OrderingKey { get; init; } // optional (e.g., ProjectId string)
}