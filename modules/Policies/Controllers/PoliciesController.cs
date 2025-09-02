using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Policies.Application.Requests;
using Policies.Application.Services;

namespace Policies.Controllers;

[ApiController]
[Route("api/policies")]
[Authorize]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyService _service;
    public PoliciesController(IPolicyService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult> Save(SavePolicyRequest request, CancellationToken ct)
    {
        var dto = await _service.SaveAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPost("minimal")]
    public async Task<ActionResult> SaveMinimal(SaveMinimalFlagsRequest request, CancellationToken ct)
    {
        var dto = await _service.SaveMinimalFlagsAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await _service.GetAsync(id, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpGet("effective")]
    public async Task<ActionResult> GetEffective([FromQuery] string scope, [FromQuery] Guid? projectId, [FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var dto = await _service.GetEffectiveAsync(scope, projectId, asOf, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    public async Task<ActionResult> List([FromQuery] string? scope, [FromQuery] Guid? projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var list = await _service.ListAsync(scope, projectId, page, pageSize, ct);
        return Ok(list);
    }
}
