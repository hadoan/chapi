using ShipMvp.Core.Entities;

namespace Chat.Domain;

public enum MessageRole { User, Assistant }
public enum MessageCardType { None, Plan, Diff, Run }

public class Message : Entity<Guid>
{
    public Guid ConversationId { get; private set; }
    public MessageRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public MessageCardType CardType { get; private set; }
    public string? CardPayload { get; private set; }
    public DateTime CreatedAt { get; private set; }
    private Message() : base(Guid.Empty) { }
    private Message(Guid id, Guid conversationId, MessageRole role, string content, MessageCardType cardType, string? cardPayload) : base(id)
    { ConversationId = conversationId; Role = role; Content = content; CardType = cardType; CardPayload = cardPayload; CreatedAt = DateTime.UtcNow; }
    public static Message Create(Guid conversationId, MessageRole role, string content, MessageCardType cardType, string? cardPayload)
    { if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Content required", nameof(content)); return new(Guid.NewGuid(), conversationId, role, content.Trim(), cardType, cardPayload); }
}
