using Artifacts.Application.Dtos;
using Artifacts.Application.Mappings;
using Artifacts.Application.Requests;
using Artifacts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Artifacts.Application.Services;

public class ArtifactService : IArtifactService
{
    private readonly Domain.IArtifactRepository _repo;
    // TODO: inject blob signer / storage abstraction
    public ArtifactService(Domain.IArtifactRepository repo) => _repo = repo;

    public async Task<ArtifactDto> CreateAsync(CreateArtifactRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<ArtifactKind>(request.Kind, true, out var kind)) throw new ArgumentException("Invalid kind", nameof(request.Kind));
        var entity = Artifact.Create(request.RunId, request.Name, kind, request.BlobName, request.Size, request.Checksums, request.RedactionProfile);
        await _repo.AddAsync(entity, ct);
        return entity.ToDto();
    }

    public async Task<ArtifactDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var ent = await _repo.GetByIdAsync(id, ct);
        return ent?.ToDto();
    }

    public async Task<IEnumerable<ArtifactDto>> ListAsync(Guid? runId, string? kind, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) page = 1; if (pageSize <= 0) pageSize = 50;
        var query = _repo.Query();
        if (runId.HasValue) query = query.Where(a => a.RunId == runId.Value);
        if (!string.IsNullOrWhiteSpace(kind) && Enum.TryParse<ArtifactKind>(kind, true, out var k)) query = query.Where(a => a.Kind == k);
        return await query.OrderByDescending(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(a => a.ToDto()).ToListAsync(ct);
    }

    public async Task<ArtifactDto?> UpdateAsync(Guid id, UpdateArtifactRequest request, CancellationToken ct)
    {
        var ent = await _repo.GetByIdAsync(id, ct);
        if (ent == null) return null;
        ent.UpdateMetadata(request.Checksums, request.RedactionProfile).SetExpiry(request.ExpiresAt);
        await _repo.UpdateAsync(ent, ct);
        return ent.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var ent = await _repo.GetByIdAsync(id, ct);
        if (ent != null)
            await _repo.DeleteAsync(ent, ct);
    }

    public async Task<string> GenerateSignedUrlAsync(Guid id, TimeSpan ttl, CancellationToken ct)
    {
        var ent = await _repo.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Artifact not found");
        // TODO: integrate blob signing logic; placeholder deterministic pseudo-URL
        return $"/artifacts/{ent.Id}?sig=TODO&exp={(DateTimeOffset.UtcNow + ttl).ToUnixTimeSeconds()}";
    }

    public async Task<IEnumerable<ArtifactDto>> ListByRunAsync(Guid runId, string? kind, CancellationToken ct)
    {
        ArtifactKind? k = null;
        if (!string.IsNullOrWhiteSpace(kind) && Enum.TryParse<ArtifactKind>(kind, true, out var parsed)) k = parsed;
        var list = await _repo.ListByRunAsync(runId, k, ct);
        return list.Select(a => a.ToDto());
    }
}
