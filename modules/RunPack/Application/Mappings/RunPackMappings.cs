using RunPack.Application.Dtos;

namespace RunPack.Application.Mappings;

public static class RunPackMappings
{
    public static RunPackDto ToDto(this Domain.RunPack p) => new(p.Id, p.ProjectId, p.SuiteId, p.Version, p.ZipBlob, p.CreatedAt);
}
