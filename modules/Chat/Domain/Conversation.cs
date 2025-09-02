using ShipMvp.Core.Entities;

namespace Chat.Domain;

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

    public void Touch() => UpdatedAt = DateTime.UtcNow;
}

// Ports (to be implemented later by LLM and diff engines)
public interface ISpecGenerator { Task<string> GenerateSpecAsync(string prompt, CancellationToken ct); }
public interface IOpenApiDiff { Task<string> DiffAsync(string oldSpec, string newSpec, CancellationToken ct); }
