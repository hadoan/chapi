namespace Chat.Application.Requests;

public record CreateConversationRequest(Guid ProjectId, string Title, string FirstUserMessage);
public record AppendMessageRequest(Guid ConversationId, string Role, string Content, string? CardType, string? CardPayload);
public record SaveDiffAsSuiteRequest(Guid ConversationId, string DiffPayload, Guid ProjectId);
