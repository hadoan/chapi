using Microsoft.AspNetCore.Mvc;
using Environments.Application;
using Environments.Application.Dtos;

namespace Environments.Controllers;

[ApiController]
[Route("api/environments")]
public class EnvironmentsController : ControllerBase
{
    private readonly IEnvironmentAppService _service;

    public EnvironmentsController(IEnvironmentAppService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult<EnvironmentDto>> Create(CreateEnvironmentRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnvironmentDto>>> List([FromQuery] GetEnvironmentsQuery query, CancellationToken ct)
        => Ok(await _service.GetListAsync(query, ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<EnvironmentDto?>> GetById(Guid id, CancellationToken ct)
    {
        var e = await _service.GetByIdAsync(id, ct);
        return e == null ? NotFound() : Ok(e);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EnvironmentDto>> Update(Guid id, UpdateEnvironmentRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
