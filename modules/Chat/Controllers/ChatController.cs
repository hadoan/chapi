using Chat.Application.Dtos;
using Chat.Application.Requests;
using Chat.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chat.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatAppService _service;
    public ChatController(IChatAppService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult<ConversationDto>> Create(CreateConversationRequest request, CancellationToken ct)
    {
        var dto = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ConversationDto>> Get(Guid id, CancellationToken ct)
    {
        var dto = await _service.GetAsync(id, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConversationDto>>> List([FromQuery] Guid projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var list = await _service.ListAsync(projectId, page, pageSize, ct);
        return Ok(list);
    }

    [HttpPost("append")]
    public async Task<ActionResult<MessageDto>> Append(AppendMessageRequest request, CancellationToken ct)
    {
        var msg = await _service.AppendAsync(request, ct);
        return msg == null ? NotFound() : Ok(msg);
    }

    [HttpPost("append-messages")]
    public async Task<ActionResult<List<MessageDto>>> AppendMessages(AppendMessagesRequest request, CancellationToken ct)
    {
        var messages = await _service.AppendMessagesAsync(request, ct);
        return messages.Count == 0 ? NotFound() : Ok(messages);
    }

    [HttpPost("save-diff-suite")]
    public async Task<ActionResult> SaveDiff(SaveDiffAsSuiteRequest request, CancellationToken ct)
    {
        await _service.SaveDiffAsSuiteAsync(request, ct);
        return Accepted();
    }
}
