using Microsoft.EntityFrameworkCore;
using RunPack.Application.Dtos;
using RunPack.Application.Mappings;
using RunPack.Application.Requests;
using RunPack.Domain;

namespace RunPack.Application.Services;

public class RunPackAppService : IRunPackAppService
{
    private readonly IRunPackRepository _repo;
    public RunPackAppService(IRunPackRepository repo) => _repo = repo;
    public async Task<RunPackDto> BuildAsync(BuildRunPackRequest request, CancellationToken ct)
    {
        var latestVersion = await _repo.GetLatestVersionAsync(request.ProjectId, request.SuiteId, ct);
        var pack = Domain.RunPack.Build(request.ProjectId, request.SuiteId, latestVersion + 1, $"runpacks/{request.ProjectId}/{request.SuiteId}/{latestVersion + 1}.zip");
        await _repo.AddAsync(pack, ct);
        return pack.ToDto();
    }
    public async Task<RunPackDto?> GetAsync(Guid id, CancellationToken ct) => (await _repo.GetByIdAsync(id, ct))?.ToDto();
    public async Task<IEnumerable<RunPackDto>> ListAsync(Guid projectId, Guid suiteId, CancellationToken ct) => await _repo.Query().Where(p => p.ProjectId == projectId && p.SuiteId == suiteId).OrderByDescending(p => p.Version).Select(p => p.ToDto()).ToListAsync(ct);
    public async Task<string> GetSignedUrlAsync(Guid id, TimeSpan ttl, CancellationToken ct)
    { var p = await _repo.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException(); return $"/runpacks/{p.Id}?sig=TODO&exp={(DateTimeOffset.UtcNow + ttl).ToUnixTimeSeconds()}"; }
}
