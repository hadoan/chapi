using Runs.Application.Contracts;

namespace Runs.Application.Services;

public interface IRunsAppService
{
    Task<CreateRunResponse> CreateAsync(CreateRunRequest input, CancellationToken ct = default);
    Task<RunDto?> GetAsync(Guid runId, CancellationToken ct = default);
    Task<IReadOnlyList<RunEventDto>> GetTimelineAsync(Guid runId, CancellationToken ct = default);
}