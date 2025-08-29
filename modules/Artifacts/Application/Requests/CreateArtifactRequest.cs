namespace Artifacts.Application.Requests;

public record CreateArtifactRequest(Guid RunId, string Name, string Kind, string BlobName, long Size, string? Checksums, string? RedactionProfile);
