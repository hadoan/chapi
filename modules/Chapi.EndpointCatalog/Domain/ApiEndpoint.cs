using System.Text.Json;
using System.Text.Json.Serialization;
using ShipMvp.Core.Entities;
using Chapi.EndpointCatalog.Application;

namespace Chapi.EndpointCatalog.Domain;

public class ApiEndpoint : Entity<Guid>
{
    public Guid ProjectId { get; private set; }
    public Guid SpecId { get; private set; }
    public string Method { get; private set; } = default!;
    public string Path { get; private set; } = default!;
    public string? OperationId { get; private set; }
    public string? Summary { get; private set; }
    public string? Description { get; private set; }
    public string[] Tags { get; private set; } = Array.Empty<string>();
    public JsonDocument Servers { get; private set; } = JsonDocument.Parse("[]");
    public JsonDocument Security { get; private set; } = JsonDocument.Parse("[]");
    public JsonDocument Parameters { get; private set; } = JsonDocument.Parse("[]");
    public JsonDocument? Request { get; private set; }
    public JsonDocument Responses { get; private set; } = JsonDocument.Parse("[]");
    public bool Deprecated { get; private set; }
    public string Source { get; private set; } = "openapi";

    private ApiEndpoint() : base(Guid.Empty) { }

    public static ApiEndpoint Create(Guid id, Guid projectId, Guid specId, EndpointDto dto)
    {
        return new ApiEndpoint {
            Id = id, ProjectId = projectId, SpecId = specId,
            Method = dto.Method, Path = dto.Path, OperationId = dto.OperationId,
            Summary = dto.Summary, Description = dto.Description,
            Tags = dto.Tags.ToArray(),
            Servers = ToDoc(dto.Servers), Security = ToDoc(dto.Security),
            Parameters = ToDoc(dto.Parameters),
            Request = dto.Request != null ? JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(dto.Request)) : null,
            Responses = ToDoc(dto.Responses),
            Deprecated = false, Source = "openapi"
        };
    }

    private static JsonDocument ToDoc(object o) => JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o));
}
