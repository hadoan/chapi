namespace Runs.Application.Contracts.Events;

public sealed record RunRequestedEto(
    Guid RunId,
    Guid? ProjectId,
    Guid? EnvironmentId,
    string IrPath,
    string Actor,
    string Trigger = "Manual",
    string? OrderingKey = null);