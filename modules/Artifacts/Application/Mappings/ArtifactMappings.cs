using Artifacts.Application.Dtos;
using Artifacts.Domain;

namespace Artifacts.Application.Mappings;

public static class ArtifactMappings
{
    public static ArtifactDto ToDto(this Artifact a) => new(a.Id, a.RunId, a.Name, a.Kind.ToString(), a.BlobName, a.Size, a.Checksums, a.RedactionProfile, a.CreatedAt, a.ExpiresAt);
}
