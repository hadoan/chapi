using System.Text.Json;
using Runs.Application.Ports;
using ShipMvp.Domain.Files;
using Runs.Application.Constants;

namespace Runs.Infrastructure.Storage;

public sealed class FileRunIrStorage : IRunIrStorage
{
    private readonly IFileStorageService _fs;

    public FileRunIrStorage(IFileStorageService fs) => _fs = fs;

    public async Task<string> WriteIrAsync(Guid runId, JsonElement ir, CancellationToken ct = default)
    {
        var key = string.Format(RunConstants.Paths.SuitePathTemplate, runId);
        using var ms = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(ir));
        await _fs.UploadAsync(RunConstants.Storage.ArtifactsContainer, key, ms, RunConstants.Http.JsonContentType, false, ct);
        return key;
    }
}