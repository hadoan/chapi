namespace Chat.Application.Requests;

public record CreateConversationRequest(
    Guid ProjectId,
    string Title,
    string FirstUserMessage,
    // Optional: Additional messages to create the conversation with (user + assistant response)
    List<AppendMessageRequest>? AdditionalMessages = null);

public class AppendMessageRequest
{
    public Guid ConversationId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? CardType { get; set; }
    public string? CardPayload { get; set; }

    // Parameterless constructor for JSON deserialization
    public AppendMessageRequest() { }

    // Convenience constructor
    public AppendMessageRequest(Guid conversationId, string role, string content, string? cardType = null, string? cardPayload = null)
    {
        ConversationId = conversationId;
        Role = role;
        Content = content;
        CardType = cardType;
        CardPayload = cardPayload;
    }

    // For use during conversation creation, ConversationId will be set later
    public AppendMessageRequest(string role, string content, string? cardType = null, string? cardPayload = null)
        : this(Guid.Empty, role, content, cardType, cardPayload) { }
}
public record AppendMessagesRequest(Guid ConversationId, List<AppendMessageRequest> Messages);
public record SaveDiffAsSuiteRequest(Guid ConversationId, string DiffPayload, Guid ProjectId);
