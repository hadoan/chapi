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

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid projectId, CancellationToken ct)
    {
        var list = await _service.ListAsync(projectId, ct);
        return Ok(list);
    }

    [HttpGet("conversation/{conversationId:guid}")]
    public async Task<IActionResult> ListByConversation(Guid conversationId, CancellationToken ct)
    {
        var list = await _service.ListByConversationAsync(conversationId, ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var dto = await _service.GetAsync(id, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Build([FromBody] BuildRunPackRequest request, CancellationToken ct)
    {
        var result = await _service.BuildAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("from-conversation")]
    public async Task<IActionResult> BuildFromConversation([FromBody] BuildRunPackFromConversationRequest request, CancellationToken ct)
    {
        var result = await _service.BuildFromConversationAsync(request, ct);
        return Ok(result);
    }
}
