namespace RunPack.Application.Dtos;

public record RunPackDto(Guid Id, Guid ProjectId, Guid SuiteId, int Version, string ZipBlob, DateTime CreatedAt);
