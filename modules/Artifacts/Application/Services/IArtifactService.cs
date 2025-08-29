using Artifacts.Application.Dtos;
using Artifacts.Application.Requests;

namespace Artifacts.Application.Services;

/// <summary>
/// Write + read operations for artifacts.
/// </summary>
public interface IArtifactService
{
    Task<ArtifactDto> CreateAsync(CreateArtifactRequest request, CancellationToken ct);
    Task<ArtifactDto?> GetAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<ArtifactDto>> ListAsync(Guid? runId, string? kind, int page, int pageSize, CancellationToken ct);
    Task<ArtifactDto?> UpdateAsync(Guid id, UpdateArtifactRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<string> GenerateSignedUrlAsync(Guid id, TimeSpan ttl, CancellationToken ct); // TODO: integrate blob signing provider
    Task<IEnumerable<ArtifactDto>> ListByRunAsync(Guid runId, string? kind, CancellationToken ct);
}
