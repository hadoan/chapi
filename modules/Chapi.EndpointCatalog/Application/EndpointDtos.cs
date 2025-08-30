namespace Chapi.EndpointCatalog.Application;

public record EndpointBriefDto(Guid Id, string Method, string Path, string? Summary, string[] Tags);
public record EndpointDto(Guid Id, Guid SpecId, string Method, string Path, string? OperationId,
                          string? Summary, string? Description, string[] Tags,
                          object Servers, object Security, object Parameters, object? Request, object Responses);
