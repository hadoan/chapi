namespace RunPack.Application.Dtos;

public record RunPackDto(Guid Id, Guid ProjectId, string Mode, int FilesCount, string? ZipUrl, string Status, DateTime CreatedAt);
