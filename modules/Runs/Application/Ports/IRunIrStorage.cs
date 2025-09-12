using System.Text.Json;

namespace Runs.Application.Ports;

public interface IRunIrStorage
{
    Task<string> WriteIrAsync(Guid runId, JsonElement ir, CancellationToken ct = default);
    // returns storage key, e.g., "runs/{runId}/suite.json"
}