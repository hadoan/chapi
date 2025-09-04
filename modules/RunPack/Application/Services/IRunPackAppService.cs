using RunPack.Application.Dtos;
using RunPack.Application.Requests;

namespace RunPack.Application.Services;

public interface IRunPackAppService
{
    Task<RunPackDto> BuildAsync(BuildRunPackRequest request, CancellationToken ct);
    Task<RunPackDto> BuildFromConversationAsync(BuildRunPackFromConversationRequest request, CancellationToken ct);
    Task<RunPackDto?> GetAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<RunPackDto>> ListAsync(Guid projectId, CancellationToken ct);
    Task<IEnumerable<RunPackDto>> ListByConversationAsync(Guid conversationId, CancellationToken ct);
    Task<string> GetSignedUrlAsync(Guid id, TimeSpan ttl, CancellationToken ct);
}
