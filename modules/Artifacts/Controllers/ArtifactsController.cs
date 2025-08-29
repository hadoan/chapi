using Artifacts.Application.Requests;
using Artifacts.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Artifacts.Controllers;

[ApiController]
[Route("api/artifacts")]
// TODO: [Authorize]
public class ArtifactsController : ControllerBase
{
    private readonly IArtifactService _service;
    public ArtifactsController(IArtifactService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult> Create(CreateArtifactRequest request, CancellationToken ct)
    {
        var dto = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await _service.GetAsync(id, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    public async Task<ActionResult> List([FromQuery] Guid? runId, [FromQuery] string? kind, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var items = await _service.ListAsync(runId, kind, page, pageSize, ct);
        return Ok(items);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, UpdateArtifactRequest request, CancellationToken ct)
    {
        var dto = await _service.UpdateAsync(id, request, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id}/signed-url")]
    public async Task<ActionResult> GetSignedUrl(Guid id, [FromQuery] int minutes = 10, CancellationToken ct = default)
    {
        var url = await _service.GenerateSignedUrlAsync(id, TimeSpan.FromMinutes(minutes), ct);
        return Ok(new { Url = url });
    }

    [HttpGet("run/{runId}")]
    public async Task<ActionResult> ListByRun(Guid runId, [FromQuery] string? kind, CancellationToken ct)
    {
        var list = await _service.ListByRunAsync(runId, kind, ct);
        return Ok(list);
    }
}
