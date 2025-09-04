using Chat.Application.Dtos;
using Chat.Application.Mappings;
using Chat.Application.Requests;
using Chat.Domain;
using Microsoft.EntityFrameworkCore;
using ShipMvp.Core.Persistence;

namespace Chat.Application.Services;

public class ChatAppService : IChatAppService
{
    private readonly IConversationRepository _repo;
    private readonly IDbContext _db;
    private readonly ISpecGenerator _specGenerator; // TODO implement
    private readonly IOpenApiDiff _openApiDiff; // TODO implement

    public ChatAppService(
        IConversationRepository repo,
        IDbContext db,
        ISpecGenerator specGenerator,
        IOpenApiDiff openApiDiff)
    {
        _repo = repo;
        _db = db;
        _specGenerator = specGenerator;
        _openApiDiff = openApiDiff;
    }
    public async Task<ConversationDto> CreateAsync(CreateConversationRequest request, CancellationToken ct)
    {
        var conv = Conversation.Create(request.Title, request.ProjectId);

        // Add the first user message
        conv.Append(MessageRole.User, request.FirstUserMessage);

        // Add any additional messages (e.g., assistant response) in the same transaction
        if (request.AdditionalMessages != null)
        {
            foreach (var messageRequest in request.AdditionalMessages)
            {
                if (Enum.TryParse<MessageRole>(messageRequest.Role, true, out var role))
                {
                    Enum.TryParse<MessageCardType>(messageRequest.CardType, true, out var cardType);
                    conv.Append(role, messageRequest.Content, cardType, messageRequest.CardPayload);
                }
            }
        }

        // Single atomic database operation for conversation + all messages
        await _repo.AddAsync(conv, ct);
        return conv.ToDto();
    }

    public async Task<ConversationDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var conv = await _repo.GetByIdAsync(id, ct);
        if (conv == null) return null;

        // Load messages separately since we no longer use navigation properties
        var messages = await _db.Set<Message>().Where(m => m.ConversationId == id).ToListAsync(ct);
        return conv.ToDto(messages);
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
        // Check if conversation exists without loading the full aggregate
        var conversationExists = await _repo.ExistsAsync(request.ConversationId, ct);
        if (!conversationExists) return null;

        if (!Enum.TryParse<MessageRole>(request.Role, true, out var role))
            throw new ArgumentException("Invalid role", nameof(request.Role));
        Enum.TryParse<MessageCardType>(request.CardType, true, out var cardType);

        // Create message directly without loading conversation aggregate
        var message = Message.Create(request.ConversationId, role, request.Content, cardType, request.CardPayload);

        // Add message directly to database, bypassing conversation-level concurrency
        await _repo.AddMessageAsync(message, ct);

        return message.ToDto();
    }

    public async Task<List<MessageDto>> AppendMessagesAsync(AppendMessagesRequest request, CancellationToken ct)
    {
        // Check if conversation exists without loading the full aggregate
        var conversationExists = await _repo.ExistsAsync(request.ConversationId, ct);
        if (!conversationExists) return new List<MessageDto>();

        var messages = new List<Message>();

        foreach (var messageRequest in request.Messages)
        {
            if (!Enum.TryParse<MessageRole>(messageRequest.Role, true, out var role))
                throw new ArgumentException($"Invalid role: {messageRequest.Role}");

            Enum.TryParse<MessageCardType>(messageRequest.CardType, true, out var cardType);
            var message = Message.Create(request.ConversationId, role, messageRequest.Content, cardType, messageRequest.CardPayload);
            messages.Add(message);
        }

        // Add all messages in a single database operation, bypassing conversation-level concurrency
        await _repo.AddMessagesAsync(messages, ct);

        return messages.Select(m => m.ToDto()).ToList();
    }

    public async Task SaveDiffAsSuiteAsync(SaveDiffAsSuiteRequest request, CancellationToken ct)
    {
        // TODO: integrate with Suites module (create new Suite based on diff payload)

        // Check if conversation exists without loading the full aggregate
        var conversationExists = await _repo.ExistsAsync(request.ConversationId, ct);
        if (!conversationExists) throw new KeyNotFoundException("Conversation not found");

        // Create message directly without loading conversation aggregate
        var message = Message.Create(request.ConversationId, MessageRole.Assistant,
            $"Diff saved as suite placeholder for project {request.ProjectId}",
            MessageCardType.Diff, request.DiffPayload);

        // Add message directly to database, bypassing conversation-level concurrency
        await _repo.AddMessageAsync(message, ct);
    }
}
