using Runs.Application.Contracts;
using Runs.Application.Ports;
using Runs.Domain;
using ShipMvp.Core.Abstractions;
using RunPack.Application.Services;
using ShipMvp.Domain.Files;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Runs.Application.Services;

public class RunsAppService : IRunsAppService
{
    private readonly IRunRepository _runRepo;
    private readonly IRunEventRepository _eventRepo;
    private readonly IRunIrStorage _irStorage;
    private readonly IRunScheduler _scheduler;
    private readonly IRunPackAppService _runPackService;
    private readonly IFileStorageService _fileStorageService;
    private readonly RunPack.Domain.IRunPackRepository _runPackRepo;
    private readonly ILogger<RunsAppService> _logger;

    public RunsAppService(
        IRunRepository runRepo,
        IRunEventRepository eventRepo,
        IRunIrStorage irStorage,
        IRunScheduler scheduler,
        IRunPackAppService runPackService,
        IFileStorageService fileStorageService,
        RunPack.Domain.IRunPackRepository runPackRepo,
        ILogger<RunsAppService> logger)
    {
        _runRepo = runRepo;
        _eventRepo = eventRepo;
        _irStorage = irStorage;
        _scheduler = scheduler;
        _runPackService = runPackService;
        _fileStorageService = fileStorageService;
        _runPackRepo = runPackRepo;
        _logger = logger;
    }

    public async Task<CreateRunResponse> CreateAsync(CreateRunRequest input, CancellationToken ct = default)
    {
        if (input.Ir is null && string.IsNullOrWhiteSpace(input.IrPath) && string.IsNullOrWhiteSpace(input.RunPackId))
            throw new ArgumentException("Either IR, IrPath, or RunPackId must be provided");

        var run = Run.New(input.ProjectId, input.EnvironmentId, input.SuiteName, input.Version, input.Actor, input.Trigger);
        await _runRepo.AddAsync(run, ct);

        string irPath;
        if (input.Ir is not null)
        {
            irPath = await _irStorage.WriteIrAsync(run.Id, input.Ir.Value, ct);
        }
        else if (!string.IsNullOrWhiteSpace(input.IrPath))
        {
            irPath = input.IrPath;
        }
        else if (!string.IsNullOrWhiteSpace(input.RunPackId))
        {
            // Get IR from runpack files (tests.json)
            var ir = await GetIrFromRunPackAsync(Guid.Parse(input.RunPackId), ct);
            irPath = await _irStorage.WriteIrAsync(run.Id, ir, ct);
        }
        else
        {
            throw new InvalidOperationException("No valid IR source provided");
        }

        run.AttachIr(irPath);
        await _runRepo.UpdateAsync(run, ct);

        await _eventRepo.AppendEventAsync(RunEvent.Created(run.Id, input.Actor), ct);
        await _scheduler.EnqueueAsync(run, ct);

        return new CreateRunResponse(run.Id, run.Status.ToString(), irPath);
    }

    private async Task<JsonElement> GetIrFromRunPackAsync(Guid runPackId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting IR from RunPack {RunPackId}", runPackId);

            // Get the runpack with files directly from repository
            var runPack = await _runPackRepo.Query()
                .Include(rp => rp.Files)
                .ThenInclude(rpf => rpf.File)
                .FirstOrDefaultAsync(rp => rp.Id == runPackId, ct);

            if (runPack == null)
                throw new ArgumentException($"RunPack with ID {runPackId} not found");

            // Find the tests.json file in the runpack files
            var testFile = runPack.Files
                .FirstOrDefault(rpf => rpf.File != null &&
                                     rpf.File.FileName.Equals("tests.json", StringComparison.OrdinalIgnoreCase));

            if (testFile?.File == null)
                throw new InvalidOperationException("No tests.json file found in the RunPack");

            _logger.LogInformation("Found tests.json file: Container={Container}, StoragePath={StoragePath}",
                testFile.File.ContainerName, testFile.File.StoragePath);

            // Extract the actual file path from the storage path if it's a full GCS URL
            string filePath = testFile.File.StoragePath;
            if (filePath.StartsWith("gs://"))
            {
                // Extract path after bucket name: gs://bucket-name/path/to/file.json -> path/to/file.json
                var uri = new Uri(filePath);
                filePath = uri.AbsolutePath.TrimStart('/');
            }

            // Check if file exists before attempting download
            var exists = await _fileStorageService.ExistsAsync(testFile.File.ContainerName, filePath, ct);
            if (!exists)
            {
                _logger.LogError("File does not exist: {Container}/{FilePath}", testFile.File.ContainerName, filePath);
                throw new InvalidOperationException($"File does not exist: {testFile.File.ContainerName}/{filePath}");
            }

            // Download the file content using IFileStorageService
            using var fileContent = await _fileStorageService.DownloadAsync(
                testFile.File.ContainerName,
                filePath,
                ct);

            // Parse the JSON content and return as JsonElement
            var jsonDocument = await JsonDocument.ParseAsync(fileContent, cancellationToken: ct);
            return jsonDocument.RootElement.Clone(); // Clone to ensure it survives document disposal
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting IR from RunPack {RunPackId}", runPackId);
            throw;
        }
    }

    public async Task<RunDto?> GetAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await _runRepo.GetWithStepsAsync(runId, ct);
        return run == null ? null : MapToDto(run);
    }

    public async Task<IReadOnlyList<RunEventDto>> GetTimelineAsync(Guid runId, CancellationToken ct = default)
    {
        var events = await _eventRepo.GetByRunIdAsync(runId, ct);
        return events.Select(e => new RunEventDto(
            e.Id,
            e.RunId,
            e.StepId,
            e.Kind,
            e.Payload,
            e.CreatedAt)).ToList();
    }

    private static RunDto MapToDto(Run run)
    {
        return new RunDto(
            run.Id,
            run.ProjectId,
            run.EnvironmentId,
            run.SuiteName,
            run.Version,
            run.Status.ToString(),
            run.Actor,
            run.Trigger,
            run.IrPath,
            run.CreatedAt,
            run.StartedAt,
            run.FinishedAt,
            run.Error,
            run.Steps.Count);
    }

    public async Task<Runs.Application.Contracts.RunsPagedResult> GetPagedAsync(int page, int pageSize, Guid? projectId = null, string? status = null, CancellationToken ct = default)
    {
        Runs.Domain.RunStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Runs.Domain.RunStatus>(status, true, out var s))
            parsedStatus = s;

        var (items, total) = await _runRepo.GetPagedAsync(page, pageSize, projectId, parsedStatus, ct);
        var dtos = items.Select(MapToDto).ToList();
        return new Runs.Application.Contracts.RunsPagedResult(dtos, total);
    }
}
