using System.Text.Json;
using ShipMvp.Core.Entities;

namespace Runs.Domain;

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
