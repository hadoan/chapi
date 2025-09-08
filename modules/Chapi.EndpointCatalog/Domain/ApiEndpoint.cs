using System.Text.Json;
using System.Text.Json.Serialization;
using ShipMvp.Core.Entities;
using Chapi.EndpointCatalog.Application;

namespace Chapi.EndpointCatalog.Domain;

public class ApiEndpoint : Entity<Guid>
{
    public Guid ProjectId { get; set; }
    public Guid SpecId { get; set; }
    public string Method { get; set; } = default!;
    public string Path { get; set; } = default!;
    public string? OperationId { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public JsonDocument Servers { get; set; } = JsonDocument.Parse("[]");
    public JsonDocument Security { get; set; } = JsonDocument.Parse("[]");
    public JsonDocument Parameters { get; set; } = JsonDocument.Parse("[]");
    public JsonDocument? Request { get; set; }
    public JsonDocument Responses { get; set; } = JsonDocument.Parse("[]");
    public bool Deprecated { get; set; }
    public string Source { get; set; } = "openapi";

    private ApiEndpoint() : base(Guid.Empty) { }

    public static ApiEndpoint Create(Guid id, Guid projectId, Guid specId, EndpointDto dto)
    {
        return new ApiEndpoint {
            Id = id, ProjectId = projectId, SpecId = specId,
            Method = dto.Method, Path = dto.Path, OperationId = dto.OperationId,
            Summary = dto.Summary, Description = dto.Description,
            Tags = dto.Tags?.ToArray() ?? Array.Empty<string>(),
            Servers = ToDoc(dto.Servers ?? new List<string>()), Security = ToDoc(dto.Security ?? new List<Dictionary<string, List<string>>>()),
            Parameters = ToDoc(dto.Parameters ?? new List<ParameterDto>()),
            Request = dto.Request != null ? JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(dto.Request, OpenApiNormalization.JsonOpts)) : null,
            Responses = ToDoc(dto.Responses ?? new Dictionary<string, ResponseDto>()),
            Deprecated = false, Source = "openapi"
        };
    }

    private static JsonDocument ToDoc(object o) => JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(o, OpenApiNormalization.JsonOpts));
}
