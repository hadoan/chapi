namespace RunPack.Application.Requests;

public record BuildRunPackRequest(Guid ProjectId, string? Mode = null);

public record BuildRunPackFromConversationRequest(
    Guid ProjectId, 
    Guid ConversationId, 
    string? Mode = null);
