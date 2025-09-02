using Microsoft.AspNetCore.Mvc;
using Orchestration.Application.Services;
using Microsoft.AspNetCore.Authorization;

namespace Orchestration.Controllers;

[ApiController]
[Route("api/orchestration")]
[Authorize]
public class OrchestrationController : ControllerBase
{
    private readonly IOrchestrationAppService _service;
    public OrchestrationController(IOrchestrationAppService service) => _service = service;

    [HttpPost("queue/{suiteId}")] public async Task<ActionResult> Queue(Guid suiteId, CancellationToken ct) { var runId = await _service.QueueAsync(suiteId, ct); return Accepted(new { RunId = runId }); }
    [HttpPost("start/{runId}")] public Task Start(Guid runId, CancellationToken ct) => _service.StartAsync(runId, ct);
    [HttpPost("complete/{runId}")] public Task Complete(Guid runId, [FromQuery] bool success, CancellationToken ct) => _service.CompleteAsync(runId, success, ct);
}
