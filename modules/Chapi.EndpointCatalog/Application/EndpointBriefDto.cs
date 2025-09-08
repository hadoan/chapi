namespace Chapi.EndpointCatalog.Application;

public record EndpointBriefDto(Guid Id, string Method, string Path, string? Summary, string[] Tags);