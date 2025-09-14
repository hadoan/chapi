namespace Runs.Application.Contracts.Events;

public sealed record RunRequestedEto(
    Guid RunId,
    Guid? ProjectId,
    string IrPath,
    string Actor,
    string Trigger = "Manual",
    string? OrderingKey = null);