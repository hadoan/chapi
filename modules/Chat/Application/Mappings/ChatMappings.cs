using Chat.Application.Dtos;
using Chat.Domain;

namespace Chat.Application.Mappings;

public static class ChatMappings
{
    // For detailed conversation retrieval with messages
    public static ConversationDto ToDto(this Conversation c, IEnumerable<Message> messages, Dictionary<Guid, Guid>? messageRunPackMap = null) =>
        new(c.Id, c.Title, c.ProjectId, c.CreatedAt, c.UpdatedAt ?? DateTime.UtcNow, 
            messages.Select(m => m.ToDto(messageRunPackMap?.GetValueOrDefault(m.Id))));

    // For conversation list operations without messages
    public static ConversationDto ToDto(this Conversation c) =>
        new(c.Id, c.Title, c.ProjectId, c.CreatedAt, c.UpdatedAt ?? DateTime.UtcNow, Enumerable.Empty<MessageDto>());

    public static MessageDto ToDto(this Message m, Guid? runPackId = null) => 
        new(m.Id, m.Role.ToString().ToLower(), m.Content, m.CardType.ToString(), m.CardPayload, m.CreatedAt, 
            runPackId == Guid.Empty ? null : runPackId);
}
