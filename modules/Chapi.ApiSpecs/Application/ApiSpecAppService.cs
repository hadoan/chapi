using System.Text.Json;
using Microsoft.Extensions.Logging;
using Chapi.SharedKernel.Events;
using Chapi.SharedKernel;
using Chapi.ApiSpecs.Domain;
using Chapi.ApiSpecs.Infrastructure.OpenApi;
using ShipMvp.Core.EventBus;

namespace Chapi.ApiSpecs.Application;

public class ApiSpecAppService : IApiSpecAppService
{
    private readonly IApiSpecRepository _repo;
    private readonly OpenApiReader _reader;
    private readonly IDistributedEventBus _eventBus;

    public ApiSpecAppService(IApiSpecRepository repo, OpenApiReader reader, IDistributedEventBus eventBus)
    {
        _repo = repo;
        _reader = reader;
        _eventBus = eventBus;
    }

    public async Task<ApiSpecDto> ImportAsync(ImportOpenApiInputDto input)
    {
        // TODO: Add authorization check when available
        // await AuthorizationService.CheckAsync(ChapiPermissions.ApiSpecs.Import);

        var (doc, rawJson, version, sha) = await _reader.ReadAsync(input.Url);

        var existing = await _repo.FindByShaAsync(sha);
        var spec = existing ?? new ApiSpec(Guid.NewGuid(), input.ProjectId, input.Url, sha, version, JsonDocument.Parse(rawJson));

        var id = await _repo.UpsertAsync(spec);

        await _eventBus.PublishAsync(new ApiSpecImportedEto(id, input.ProjectId));

        return new ApiSpecDto(id, spec.ProjectId, spec.SourceUrl, spec.Version, spec.CreatedAt);
    }

    public async Task<ApiSpecDto?> GetAsync(Guid specId)
    {
        var spec = await _repo.GetByIdAsync(specId);
        return spec == null ? null : new ApiSpecDto(spec.Id, spec.ProjectId, spec.SourceUrl, spec.Version, spec.CreatedAt);
    }

    public async Task<IEnumerable<ApiSpecDto>> ListByProjectAsync(Guid projectId)
    {
        var specs = await _repo.GetAllAsync();
        return specs.Where(s => s.ProjectId == projectId)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new ApiSpecDto(s.Id, s.ProjectId, s.SourceUrl, s.Version, s.CreatedAt));
    }
}
