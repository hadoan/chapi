using Runs.Application.Dtos;

namespace Runs.Application;

public interface IRunAppService
{
    Task<RunDto> QueueAsync(QueueRunRequest request, CancellationToken cancellationToken = default);
    Task<RunDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<RunDto>> GetListAsync(GetRunsQuery query, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(GetRunsQuery query, CancellationToken cancellationToken = default);
    Task<RunDto> StartAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RunDto> SucceedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RunDto> FailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RunDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RunDto> AddStepAsync(Guid id, AddRunStepRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
