using ShipMvp.Core.Entities;

namespace Chat.Domain;

public enum MessageRole { User, Assistant }
public enum MessageCardType { None, Plan, Diff, Run }

public class Conversation : Entity<Guid>
{
    private readonly List<Message> _messages = new();
    public string Title { get; private set; } = string.Empty;
    public Guid ProjectId { get; private set; }
    public IReadOnlyCollection<Message> Messages => _messages;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Conversation() : base(Guid.Empty) { }
    private Conversation(Guid id, string title, Guid projectId) : base(id)
    { Title = title; ProjectId = projectId; CreatedAt = UpdatedAt = DateTime.UtcNow; }
    public static Conversation Create(string title, Guid projectId)
    { if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title required", nameof(title)); return new(Guid.NewGuid(), title.Trim(), projectId); }
    public Message Append(MessageRole role, string content, MessageCardType cardType = MessageCardType.None, string? cardPayload = null)
    { var msg = Message.Create(Id, role, content, cardType, cardPayload); _messages.Add(msg); UpdatedAt = DateTime.UtcNow; return msg; }
}

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

public interface IConversationRepository : ShipMvp.Core.Abstractions.IRepository<Conversation, Guid>
{
    IQueryable<Conversation> Query();
}

// Ports (to be implemented later by LLM and diff engines)
public interface ISpecGenerator { Task<string> GenerateSpecAsync(string prompt, CancellationToken ct); }
public interface IOpenApiDiff { Task<string> DiffAsync(string oldSpec, string newSpec, CancellationToken ct); }
