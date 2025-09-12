using Runs.Application.Contracts;
using Runs.Application.Ports;
using Runs.Domain;
using ShipMvp.Core.Abstractions;

namespace Runs.Application.Services;

public class RunsAppService : IRunsAppService
{
    private readonly IRunRepository _runRepo;
    private readonly IRunEventRepository _eventRepo;
    private readonly IRunIrStorage _irStorage;
    private readonly IRunScheduler _scheduler;

    public RunsAppService(
        IRunRepository runRepo,
        IRunEventRepository eventRepo,
        IRunIrStorage irStorage,
        IRunScheduler scheduler)
    {
        _runRepo = runRepo;
        _eventRepo = eventRepo;
        _irStorage = irStorage;
        _scheduler = scheduler;
    }

    public async Task<CreateRunResponse> CreateAsync(CreateRunRequest input, CancellationToken ct = default)
    {
        if (input.Ir is null && string.IsNullOrWhiteSpace(input.IrPath))
            throw new ArgumentException("Either IR or IrPath must be provided");

        var run = Run.New(input.ProjectId, input.SuiteName, input.Version, input.Actor, input.Trigger);
        await _runRepo.AddAsync(run, ct);

        var irPath = input.Ir is not null
            ? await _irStorage.WriteIrAsync(run.Id, input.Ir.Value, ct)
            : input.IrPath!;

        run.AttachIr(irPath);
        await _runRepo.UpdateAsync(run, ct);

        await _eventRepo.AppendEventAsync(RunEvent.Created(run.Id, input.Actor), ct);
        await _scheduler.EnqueueAsync(run, ct);

        return new CreateRunResponse(run.Id, run.Status.ToString(), irPath);
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
}
