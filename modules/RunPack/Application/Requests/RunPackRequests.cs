namespace RunPack.Application.Requests;

public record BuildRunPackRequest(Guid ProjectId, Guid SuiteId);
