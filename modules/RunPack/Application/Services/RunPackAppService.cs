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
        var pack = Domain.RunPack.Create(request.ProjectId, request.Mode ?? "hybrid");
        pack.SetGeneratorVersion("1.0.0");
        await _repo.AddAsync(pack, ct);
        return pack.ToDto();
    }

    public async Task<RunPackDto> BuildFromConversationAsync(BuildRunPackFromConversationRequest request, CancellationToken ct)
    {
        var pack = Domain.RunPack.CreateFromConversation(request.ProjectId, request.ConversationId, request.Mode ?? "hybrid");
        pack.SetGeneratorVersion("1.0.0");
        await _repo.AddAsync(pack, ct);
        return pack.ToDto();
    }
    
    public async Task<RunPackDto?> GetAsync(Guid id, CancellationToken ct) => (await _repo.GetByIdAsync(id, ct))?.ToDto();
    
    public async Task<IEnumerable<RunPackDto>> ListAsync(Guid projectId, CancellationToken ct) => 
        await _repo.Query()
            .Where(p => p.ProjectId == projectId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => p.ToDto())
            .ToListAsync(ct);

    public async Task<IEnumerable<RunPackDto>> ListByConversationAsync(Guid conversationId, CancellationToken ct) => 
        await _repo.Query()
            .Where(p => p.ConversationId == conversationId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => p.ToDto())
            .ToListAsync(ct);
            
    public async Task<string> GetSignedUrlAsync(Guid id, TimeSpan ttl, CancellationToken ct)
    { 
        var p = await _repo.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException(); 
        return p.ZipUrl ?? $"/runpacks/{p.Id}?sig=TODO&exp={(DateTimeOffset.UtcNow + ttl).ToUnixTimeSeconds()}"; 
    }
}
