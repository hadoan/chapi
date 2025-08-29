using Microsoft.AspNetCore.Mvc;
using Projects.Application;
using Projects.Application.Dtos;

namespace Projects.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _service;

    public ProjectsController(IProjectService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> List([FromQuery] GetProjectsQuery query, CancellationToken ct)
    {
        var list = await _service.GetListAsync(query, ct);
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectDto?>> GetById(Guid id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProjectDto>> Update(Guid id, UpdateProjectRequest request, CancellationToken ct)
    {
        var updated = await _service.UpdateAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<ProjectDto>> Activate(Guid id, CancellationToken ct)
        => Ok(await _service.ActivateAsync(id, ct));

    [HttpPost("{id}/complete")]
    public async Task<ActionResult<ProjectDto>> Complete(Guid id, CancellationToken ct)
        => Ok(await _service.CompleteAsync(id, ct));

    [HttpPost("{id}/hold")]
    public async Task<ActionResult<ProjectDto>> Hold(Guid id, CancellationToken ct)
        => Ok(await _service.HoldAsync(id, ct));

    [HttpPost("{id}/archive")]
    public async Task<ActionResult<ProjectDto>> Archive(Guid id, CancellationToken ct)
        => Ok(await _service.ArchiveAsync(id, ct));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
