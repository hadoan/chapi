using System.Text.Json;

namespace Runs.Application.Contracts;

public sealed record CreateRunRequest(
    Guid? ProjectId,
    string SuiteName,
    string Version,
    JsonElement? Ir,
    string? IrPath,      // optional: pre-uploaded key
    string Actor,
    string Trigger = "Manual",
    string? IdempotencyKey = null);

public sealed record CreateRunResponse(Guid RunId, string Status, string IrPath);

public sealed record RunDto(
    Guid Id,
    Guid? ProjectId,
    string SuiteName,
    string Version,
    string Status,
    string Actor,
    string Trigger,
    string? IrPath,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    string? Error,
    int StepsCount);

public sealed record RunStepDto(
    Guid Id,
    Guid RunId,
    string StepId,
    string Type,
    int Order,
    string Status,
    int? StatusCode,
    int? DurationMs,
    string? Error,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? FinishedAt);

public sealed record RunEventDto(
    Guid Id,
    Guid RunId,
    string? StepId,
    string Kind,
    string Payload,
    DateTime CreatedAt);