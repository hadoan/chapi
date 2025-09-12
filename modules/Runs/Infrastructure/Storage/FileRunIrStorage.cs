using System.Text.Json;
using Runs.Application.Ports;
using ShipMvp.Domain.Files;

namespace Runs.Infrastructure.Storage;

public sealed class FileRunIrStorage : IRunIrStorage
{
    private readonly IFileStorageService _fs;          // your abstraction
    private const string Container = "artifacts";      // logical bucket/container

    public FileRunIrStorage(IFileStorageService fs) => _fs = fs;

    public async Task<string> WriteIrAsync(Guid runId, JsonElement ir, CancellationToken ct = default)
    {
        var key = $"runs/{runId}/suite.json";
        using var ms = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(ir));
        await _fs.UploadAsync(Container, key, ms, "application/json", false, ct);
        return key;
    }
}