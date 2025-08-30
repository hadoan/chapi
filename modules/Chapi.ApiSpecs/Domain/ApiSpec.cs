using System.Text.Json;
using ShipMvp.Core.Entities;

namespace Chapi.ApiSpecs.Domain;

public class ApiSpec : Entity<Guid>
{
    public Guid? TenantId { get; private set; }
    public Guid ProjectId { get; private set; }
    public string SourceUrl { get; private set; } = default!;
    public string? Version { get; private set; }
    public string Sha256 { get; private set; } = default!;
    public JsonDocument Raw { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private ApiSpec() : base(Guid.Empty) { }

    public ApiSpec(Guid id, Guid projectId, string sourceUrl, string sha, string? version, JsonDocument raw) : base(id)
    {
        ProjectId = projectId;
        SourceUrl = sourceUrl;
        Sha256 = sha;
        Version = version;
        Raw = raw;
        CreatedAt = DateTime.UtcNow;
    }
}
