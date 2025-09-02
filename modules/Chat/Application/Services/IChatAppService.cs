using Chat.Application.Dtos;
using Chat.Application.Requests;

namespace Chat.Application.Services;

public interface IChatAppService
{
    Task<ConversationDto> CreateAsync(CreateConversationRequest request, CancellationToken ct);
    Task<ConversationDto?> GetAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<ConversationDto>> ListAsync(Guid projectId, int page, int pageSize, CancellationToken ct);
    Task<MessageDto?> AppendAsync(AppendMessageRequest request, CancellationToken ct);
    Task<List<MessageDto>> AppendMessagesAsync(AppendMessagesRequest request, CancellationToken ct);
    Task SaveDiffAsSuiteAsync(SaveDiffAsSuiteRequest request, CancellationToken ct);
}
