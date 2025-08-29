namespace Artifacts.Application.Dtos;

public record ArtifactDto(Guid Id, Guid RunId, string Name, string Kind, string BlobName, long Size, string? Checksums, string? RedactionProfile, DateTime CreatedAt, DateTime? ExpiresAt);
