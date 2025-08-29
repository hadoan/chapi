using Chat.Application.Dtos;
using Chat.Application.Mappings;
using Chat.Application.Requests;
using Chat.Domain;
using Microsoft.EntityFrameworkCore;

namespace Chat.Application.Services;

public class ChatAppService : IChatAppService
{
    private readonly IConversationRepository _repo;
    private readonly ISpecGenerator _specGenerator; // TODO implement
    private readonly IOpenApiDiff _openApiDiff; // TODO implement
    public ChatAppService(IConversationRepository repo, ISpecGenerator specGenerator, IOpenApiDiff openApiDiff)
    { _repo = repo; _specGenerator = specGenerator; _openApiDiff = openApiDiff; }

    public async Task<ConversationDto> CreateAsync(CreateConversationRequest request, CancellationToken ct)
    {
        var conv = Conversation.Create(request.Title, request.ProjectId);
        conv.Append(MessageRole.User, request.FirstUserMessage);
        await _repo.AddAsync(conv, ct);
        return conv.ToDto();
    }

    public async Task<ConversationDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var conv = await _repo.GetByIdAsync(id, ct);
        return conv?.ToDto();
    }

    public async Task<IEnumerable<ConversationDto>> ListAsync(Guid projectId, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) page = 1; if (pageSize <= 0) pageSize = 50;
        return await _repo.Query().Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.UpdatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => c.ToDto()).ToListAsync(ct);
    }

    public async Task<MessageDto?> AppendAsync(AppendMessageRequest request, CancellationToken ct)
    {
        var conv = await _repo.GetByIdAsync(request.ConversationId, ct);
        if (conv == null) return null;
        if (!Enum.TryParse<MessageRole>(request.Role, true, out var role)) throw new ArgumentException("Invalid role", nameof(request.Role));
        Enum.TryParse<MessageCardType>(request.CardType, true, out var cardType);
        var msg = conv.Append(role, request.Content, cardType, request.CardPayload);
        await _repo.UpdateAsync(conv, ct);
        return msg.ToDto();
    }

    public async Task SaveDiffAsSuiteAsync(SaveDiffAsSuiteRequest request, CancellationToken ct)
    {
        // TODO: integrate with Suites module (create new Suite based on diff payload)
        var conv = await _repo.GetByIdAsync(request.ConversationId, ct);
        if (conv == null) throw new KeyNotFoundException("Conversation not found");
        conv.Append(MessageRole.Assistant, $"Diff saved as suite placeholder for project {request.ProjectId}", MessageCardType.Diff, request.DiffPayload);
        await _repo.UpdateAsync(conv, ct);
    }
}
