using System.Text.Json;
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

    // Store as true jsonb (EF: jsonb)
    public JsonDocument Servers { get; set; } = ToJsonDoc(Array.Empty<string>());
    public JsonDocument Security { get; set; } = ToJsonDoc(Array.Empty<object>());
    public JsonDocument Parameters { get; set; } = ToJsonDoc(Array.Empty<object>());
    public JsonDocument? Request { get; set; }   // null when no body
    public JsonDocument Responses { get; set; } = ToJsonDoc(new Dictionary<string, object>());

    // Derived flags (plain columns)
    public bool RequiresAuth { get; set; }
    public bool HasRequestBody { get; set; }
    public bool ConsumesJson { get; set; }
    public bool ProducesJson { get; set; }

    public bool Deprecated { get; set; }
    public string Source { get; set; } = "openapi";

    private ApiEndpoint() : base(Guid.Empty) { }

    public static ApiEndpoint Create(Guid id, Guid projectId, Guid specId, EndpointDto dto)
    {
        var entity = new ApiEndpoint {
            Id = id, ProjectId = projectId, SpecId = specId,
            Method = dto.Method, Path = dto.Path, OperationId = dto.OperationId,
            Summary = dto.Summary, Description = dto.Description,
            Tags = dto.Tags?.ToArray() ?? Array.Empty<string>(),

            Servers    = ToJsonDoc(dto.Servers ?? new List<string>()),
            Security   = ToJsonDoc(dto.Security ?? new List<Dictionary<string, List<string>>>()),
            Parameters = ToJsonDoc(dto.Parameters ?? new List<ParameterDto>()),
            Request    = dto.Request != null ? ToJsonDoc(dto.Request) : null,
            Responses  = ToJsonDoc(dto.Responses ?? new Dictionary<string, ResponseDto>()),

            // defaults; recomputed below
            RequiresAuth = false,
            HasRequestBody = false,
            ConsumesJson = false,
            ProducesJson = false,

            Deprecated = false, Source = "openapi"
        };

        entity.ComputeDerivedFlags(dto);
        return entity;
    }

    // Helper: one serializer (camelCase, ignore nulls)
    private static readonly JsonSerializerOptions JsonOpts = OpenApiNormalization.JsonOpts;

    private static JsonDocument ToJsonDoc<T>(T value)
        => JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(value, JsonOpts));

    public void ComputeDerivedFlags(EndpointDto dto)
    {
        RequiresAuth = (dto.Security?.Count ?? 0) > 0;
        HasRequestBody = dto.Request is not null;

        ConsumesJson = dto.Request?.Content?.Keys
            .Any(mt => mt.Contains("json", StringComparison.OrdinalIgnoreCase)) == true;

        ProducesJson = dto.Responses?.Values.Any(r =>
            r.Content?.Keys.Any(mt => mt.Contains("json", StringComparison.OrdinalIgnoreCase)) == true
        ) == true;
    }
}
