using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Suites.Application;
using Suites.Application.Dtos;

namespace Suites.Controllers;

[ApiController]
[Route("api/suites")]
[Authorize]
public class SuitesController : ControllerBase
{
    private readonly ISuiteAppService _service;
    public SuitesController(ISuiteAppService service) => _service = service;

    [HttpGet]
    public async Task<IEnumerable<SuiteDto>> GetList([FromQuery] GetSuitesQuery query, CancellationToken ct)
        => await _service.GetListAsync(query, ct);

    [HttpGet("count")]
    public async Task<int> GetCount([FromQuery] GetSuitesQuery query, CancellationToken ct)
        => await _service.GetCountAsync(query, ct);

    [HttpGet("{id}")]
    public async Task<ActionResult<SuiteDto?>> Get(Guid id, CancellationToken ct)
    {
        var dto = await _service.GetByIdAsync(id, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<SuiteDto> Create(CreateSuiteRequest request, CancellationToken ct)
        => await _service.CreateAsync(request, ct);

    [HttpPut("{id}")]
    public async Task<SuiteDto> Update(Guid id, UpdateSuiteRequest request, CancellationToken ct)
        => await _service.UpdateAsync(id, request, ct);

    [HttpDelete("{id}")]
    public async Task Delete(Guid id, CancellationToken ct)
        => await _service.DeleteAsync(id, ct);

    [HttpPost("{id}/activate")]
    public async Task<SuiteDto> Activate(Guid id, CancellationToken ct)
        => await _service.ActivateAsync(id, ct);

    [HttpPost("{id}/archive")]
    public async Task<SuiteDto> Archive(Guid id, CancellationToken ct)
        => await _service.ArchiveAsync(id, ct);
}
