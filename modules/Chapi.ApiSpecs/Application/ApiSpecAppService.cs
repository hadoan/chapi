using System.Text.Json;
using Microsoft.Extensions.Logging;
using ShipMvp.Core.Application;
using ShipMvp.Core.Events;
using Chapi.SharedKernel.Events;
using Chapi.SharedKernel;
using Chapi.ApiSpecs.Domain;

namespace Chapi.ApiSpecs.Application;

public class ApiSpecAppService : ApplicationService, IApiSpecAppService
{
    private readonly IApiSpecRepository _repo;
    private readonly IOpenApiReader _reader;
    private readonly IDistributedEventBus _bus;

    public ApiSpecAppService(IApiSpecRepository repo, IOpenApiReader reader, IDistributedEventBus bus)
    {
        _repo = repo; _reader = reader; _bus = bus;
    }

    public async Task<ApiSpecDto> ImportAsync(ImportOpenApiInputDto input)
    {
        // await AuthorizationService.CheckAsync(ChapiPermissions.ApiSpecs.Import);

        var (doc, rawJson, version, sha) = await _reader.ReadAsync(input.Url);

        var existing = await _repo.FindByShaAsync(sha);
        var spec = existing ?? new ApiSpec(Guid.NewGuid(), input.ProjectId, input.Url, sha, version, JsonDocument.Parse(rawJson));

        var id = await _repo.UpsertAsync(spec);

        await _bus.PublishAsync(new ApiSpecImportedEto(id, input.ProjectId));

        return new ApiSpecDto(id, spec.ProjectId, spec.SourceUrl, spec.Version, spec.CreatedAt);
    }

    public async Task<ApiSpecDto?> GetAsync(Guid specId)
    {
        var spec = await _repo.FindAsync(specId);
        return spec == null ? null : new ApiSpecDto(spec.Id, spec.ProjectId, spec.SourceUrl, spec.Version, spec.CreatedAt);
    }
}
