using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Runs.Application;
using Runs.Application.Dtos;

namespace Runs.Controllers;

[ApiController]
[Route("api/runs")]
[Authorize]
public class RunsController : ControllerBase
{
    private readonly IRunAppService _service;
    public RunsController(IRunAppService service) => _service = service;
    [HttpGet] public async Task<IEnumerable<RunDto>> GetList([FromQuery] GetRunsQuery q, CancellationToken ct) => await _service.GetListAsync(q, ct);
    [HttpGet("count")] public async Task<int> Count([FromQuery] GetRunsQuery q, CancellationToken ct) => await _service.GetCountAsync(q, ct);
    [HttpGet("{id}")] public async Task<ActionResult<RunDto?>> Get(Guid id, CancellationToken ct) { var dto = await _service.GetByIdAsync(id, ct); return dto == null ? NotFound() : Ok(dto); }
    [HttpPost] public async Task<RunDto> Queue(QueueRunRequest req, CancellationToken ct) => await _service.QueueAsync(req, ct);
    [HttpPost("{id}/start")] public async Task<RunDto> Start(Guid id, CancellationToken ct) => await _service.StartAsync(id, ct);
    [HttpPost("{id}/succeed")] public async Task<RunDto> Succeed(Guid id, CancellationToken ct) => await _service.SucceedAsync(id, ct);
    [HttpPost("{id}/fail")] public async Task<RunDto> Fail(Guid id, CancellationToken ct) => await _service.FailAsync(id, ct);
    [HttpPost("{id}/cancel")] public async Task<RunDto> Cancel(Guid id, CancellationToken ct) => await _service.CancelAsync(id, ct);
    [HttpPost("{id}/steps")] public async Task<RunDto> AddStep(Guid id, AddRunStepRequest req, CancellationToken ct) => await _service.AddStepAsync(id, req, ct);
    [HttpDelete("{id}")] public async Task Delete(Guid id, CancellationToken ct) => await _service.DeleteAsync(id, ct);
}
