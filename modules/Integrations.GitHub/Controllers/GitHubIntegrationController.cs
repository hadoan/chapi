using Integrations.GitHub.Application.Requests;
using Integrations.GitHub.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Integrations.GitHub.Controllers;

[ApiController]
[Route("api/integrations/github")] // TODO: [Authorize]
public class GitHubIntegrationController : ControllerBase
{
    private readonly IGitHubIntegrationAppService _service;
    public GitHubIntegrationController(IGitHubIntegrationAppService service) => _service = service;

    [HttpPost("connect")] public async Task<ActionResult> Connect(ConnectInstallationRequest req, CancellationToken ct)
    { var dto = await _service.ConnectAsync(req, ct); return CreatedAtAction(nameof(Get), new { installationId = dto.InstallationId }, dto); }
    [HttpGet("{installationId:long}")] public async Task<ActionResult> Get(long installationId, CancellationToken ct)
    { var dto = await _service.GetAsync(installationId, ct); return dto == null ? NotFound() : Ok(dto); }
    [HttpPost("checks")] public async Task<ActionResult> EnableChecks(EnableChecksRequest req, CancellationToken ct)
    { var dto = await _service.EnableChecksAsync(req, ct); return dto == null ? NotFound() : Ok(dto); }
    [HttpPost("repos")] public async Task<ActionResult> AddRepo(AddRepoRequest req, CancellationToken ct)
    { var dto = await _service.AddRepoAsync(req, ct); return dto == null ? NotFound() : Ok(dto); }
    [HttpDelete("repos")] public async Task<ActionResult> RemoveRepo([FromBody] RemoveRepoRequest req, CancellationToken ct)
    { var dto = await _service.RemoveRepoAsync(req, ct); return dto == null ? NotFound() : Ok(dto); }
}
