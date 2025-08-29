namespace Chat.Application.Dtos;

public record ConversationDto(Guid Id, string Title, Guid ProjectId, DateTime CreatedAt, DateTime UpdatedAt, IEnumerable<MessageDto> Messages);
public record MessageDto(Guid Id, string Role, string Content, string CardType, string? CardPayload, DateTime CreatedAt);
