namespace RunPack.Application.Requests;

public record BuildRunPackRequest(Guid ProjectId, string? Mode = null);
