namespace Artifacts.Application.Requests;

public record UpdateArtifactRequest(string? Checksums, string? RedactionProfile, DateTime? ExpiresAt);
