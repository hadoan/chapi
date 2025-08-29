using Runs.Application.Dtos;
using Runs.Domain;
using ShipMvp.Core.Attributes;

namespace Runs.Application;

[AutoController(Route = "api/runs")]
public class RunAppService : IRunAppService
{
    private readonly IRunRepository _repository;
    public RunAppService(IRunRepository repository) => _repository = repository;

    public async Task<RunDto> QueueAsync(QueueRunRequest request, CancellationToken cancellationToken = default)
    {
        var run = Run.Queue(request.SuiteId);
        var created = await _repository.AddAsync(run, cancellationToken);
        return Map(created);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => await _repository.DeleteAsync(id, cancellationToken);
    public async Task<RunDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var r = await _repository.GetByIdAsync(id, cancellationToken);
        return r == null ? null : Map(r);
    }
    public async Task<IEnumerable<RunDto>> GetListAsync(GetRunsQuery query, CancellationToken cancellationToken = default)
    {
        Runs.Domain.RunStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<RunStatus>(query.Status, true, out var s)) status = s;
        var (items, _) = await _repository.GetPagedAsync(query.Page, query.PageSize, query.SuiteId, status, cancellationToken);
        return items.Select(Map).ToList();
    }
    public async Task<int> GetCountAsync(GetRunsQuery query, CancellationToken cancellationToken = default)
    {
        RunStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<RunStatus>(query.Status, true, out var s)) status = s;
        var (_, total) = await _repository.GetPagedAsync(query.Page, query.PageSize, query.SuiteId, status, cancellationToken);
        return total;
    }
    public async Task<RunDto> StartAsync(Guid id, CancellationToken cancellationToken = default) => await Transition(id, r => r.Start(), cancellationToken);
    public async Task<RunDto> SucceedAsync(Guid id, CancellationToken cancellationToken = default) => await Transition(id, r => r.Succeed(), cancellationToken);
    public async Task<RunDto> FailAsync(Guid id, CancellationToken cancellationToken = default) => await Transition(id, r => r.Fail(), cancellationToken);
    public async Task<RunDto> CancelAsync(Guid id, CancellationToken cancellationToken = default) => await Transition(id, r => r.Cancel(), cancellationToken);
    public async Task<RunDto> AddStepAsync(Guid id, AddRunStepRequest request, CancellationToken cancellationToken = default)
    {
        var run = await _repository.GetByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Run not found");
        run.AddStep(request.Name, request.Log);
        var updated = await _repository.UpdateAsync(run, cancellationToken);
        return Map(updated);
    }
    private async Task<RunDto> Transition(Guid id, Action<Run> action, CancellationToken ct)
    {
        var run = await _repository.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Run not found");
        action(run);
        var updated = await _repository.UpdateAsync(run, ct);
        return Map(updated);
    }
    private static RunDto Map(Run r) => new()
    {
        Id = r.Id,
        SuiteId = r.SuiteId,
        Status = r.Status.ToString(),
        CreatedAt = r.CreatedAt,
        StartedAt = r.StartedAt,
        FinishedAt = r.FinishedAt,
        Steps = r.Steps.Select(s => new RunStepDto(s.Id, s.Name, s.Log)).ToList()
    };
}
