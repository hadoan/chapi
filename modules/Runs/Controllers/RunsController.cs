using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Runs.Application.Contracts;
using Runs.Application.Services;
using ShipMvp.Domain.Files;

namespace Runs.Controllers;

[ApiController]
[Route("api/runs")]
[Authorize]
public class RunsController : ControllerBase
{
    private readonly IRunsAppService _service;
    private readonly IFileStorageService _fileStorageService;

    public RunsController(IRunsAppService service, IFileStorageService fileStorageService)
    {
        _service = service;
        _fileStorageService = fileStorageService;
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<RunDto?>> Get(Guid id, CancellationToken ct)
    {
        var dto = await _service.GetAsync(id, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public Task<CreateRunResponse> Create([FromBody] CreateRunRequest input, CancellationToken ct)
        => _service.CreateAsync(input, ct);

    [HttpGet("{runId:guid}/timeline")]
    public Task<IReadOnlyList<RunEventDto>> Timeline(Guid runId, CancellationToken ct)
        => _service.GetTimelineAsync(runId, ct);

    // Signed URL proxy for artifacts
    [HttpGet("{runId:guid}/artifacts/{stepId}/{name}")]
    public async Task<IActionResult> GetArtifact(Guid runId, string stepId, string name, CancellationToken ct)
    {
        var key = $"runs/{runId}/steps/{stepId}/{name}";
        var url = await _fileStorageService.GetSignedUrlAsync("artifacts", key, TimeSpan.FromMinutes(10), ct);
        return Redirect(url);
    }
}
