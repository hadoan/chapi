namespace Chapi.ApiSpecs.Application;

public record ImportOpenApiInputDto(Guid ProjectId, string Url);
public record ApiSpecDto(Guid Id, Guid ProjectId, string SourceUrl, string? Version, DateTime CreatedAt);
