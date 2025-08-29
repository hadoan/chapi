using ShipMvp.Core.Entities;

namespace Artifacts.Domain;

/// <summary>
/// Kinds of artifacts produced during a run.
/// </summary>
public enum ArtifactKind
{
    Request,
    Response,
    JUnit,
    Html,
    Transcript
}

/// <summary>
/// Artifact aggregate root referencing a blob plus metadata for retention/redaction.
/// </summary>
public class Artifact : Entity<Guid>
{
    public Guid RunId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ArtifactKind Kind { get; private set; }
    public string BlobName { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string? Checksums { get; private set; } // Comma separated, e.g. sha256:...,md5:...
    public string? RedactionProfile { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; } // TODO: lifecycle policies

    private Artifact() : base(Guid.Empty) { }
    private Artifact(Guid id, Guid runId, string name, ArtifactKind kind, string blobName, long size, string? checksums, string? redactionProfile) : base(id)
    {
        RunId = runId;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name required", nameof(name)) : name.Trim();
        Kind = kind;
        BlobName = string.IsNullOrWhiteSpace(blobName) ? throw new ArgumentException("BlobName required", nameof(blobName)) : blobName.Trim();
        Size = size >= 0 ? size : throw new ArgumentOutOfRangeException(nameof(size));
        Checksums = checksums;
        RedactionProfile = redactionProfile;
        CreatedAt = DateTime.UtcNow;
    }

    public static Artifact Create(Guid runId, string name, ArtifactKind kind, string blobName, long size, string? checksums = null, string? redactionProfile = null)
        => new(Guid.NewGuid(), runId, name, kind, blobName, size, checksums, redactionProfile);

    public Artifact UpdateMetadata(string? checksums = null, string? redactionProfile = null)
    {
        if (checksums is { Length: > 0 }) Checksums = checksums;
        if (redactionProfile is { Length: > 0 }) RedactionProfile = redactionProfile;
        return this;
    }

    public Artifact SetExpiry(DateTime? expiresAt)
    {
        if (expiresAt.HasValue && expiresAt.Value <= CreatedAt)
            throw new InvalidOperationException("Expiry must be after creation");
        ExpiresAt = expiresAt;
        return this;
    }
}

/// <summary>
/// Repository abstraction for Artifacts.
/// </summary>
public interface IArtifactRepository : ShipMvp.Core.Abstractions.IRepository<Artifact, Guid>
{
    IQueryable<Artifact> Query();
    Task<IEnumerable<Artifact>> ListByRunAsync(Guid runId, ArtifactKind? kind = null, CancellationToken ct = default);
}
