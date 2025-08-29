using Chat.Application.Requests;
using Chat.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Chat.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatAppService _service;
    public ChatController(IChatAppService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult> Create(CreateConversationRequest request, CancellationToken ct)
    {
        var dto = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> Get(Guid id, CancellationToken ct)
    {
        var dto = await _service.GetAsync(id, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    public async Task<ActionResult> List([FromQuery] Guid projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var list = await _service.ListAsync(projectId, page, pageSize, ct);
        return Ok(list);
    }

    [HttpPost("append")]
    public async Task<ActionResult> Append(AppendMessageRequest request, CancellationToken ct)
    {
        var msg = await _service.AppendAsync(request, ct);
        return msg == null ? NotFound() : Ok(msg);
    }

    [HttpPost("save-diff-suite")]
    public async Task<ActionResult> SaveDiff(SaveDiffAsSuiteRequest request, CancellationToken ct)
    {
        await _service.SaveDiffAsSuiteAsync(request, ct);
        return Accepted();
    }
}
