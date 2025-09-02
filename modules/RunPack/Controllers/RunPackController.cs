using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunPack.Application.Requests;
using RunPack.Application.Services;

namespace RunPack.Controllers;

[ApiController]
[Route("api/runpacks")]
[Authorize]
public class RunPackController : ControllerBase
{
    private readonly IRunPackAppService _service;
    public RunPackController(IRunPackAppService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult> Build(BuildRunPackRequest request, CancellationToken ct)
    { var dto = await _service.BuildAsync(request, ct); return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto); }

    [HttpGet("{id}")]
    public async Task<ActionResult> Get(Guid id, CancellationToken ct)
    { var dto = await _service.GetAsync(id, ct); return dto == null ? NotFound() : Ok(dto); }

    [HttpGet]
    public async Task<ActionResult> List([FromQuery] Guid projectId, [FromQuery] Guid suiteId, CancellationToken ct)
    { var list = await _service.ListAsync(projectId, suiteId, ct); return Ok(list); }

    [HttpGet("{id}/signed-url")]
    public async Task<ActionResult> SignedUrl(Guid id, [FromQuery] int minutes = 15, CancellationToken ct = default)
    { var url = await _service.GetSignedUrlAsync(id, TimeSpan.FromMinutes(minutes), ct); return Ok(new { Url = url }); }
}
