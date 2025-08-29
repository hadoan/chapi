using Chat.Application.Dtos;
using Chat.Domain;

namespace Chat.Application.Mappings;

public static class ChatMappings
{
    public static ConversationDto ToDto(this Conversation c) => new(c.Id, c.Title, c.ProjectId, c.CreatedAt, c.UpdatedAt, c.Messages.Select(m => m.ToDto()));
    public static MessageDto ToDto(this Message m) => new(m.Id, m.Role.ToString(), m.Content, m.CardType.ToString(), m.CardPayload, m.CreatedAt);
}
