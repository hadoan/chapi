using System.Text.Json;
using Microsoft.Extensions.Logging;
using Chapi.SharedKernel.Events;
using Chapi.SharedKernel;
using Chapi.ApiSpecs.Domain;
using Chapi.ApiSpecs.Infrastructure.OpenApi;

namespace Chapi.ApiSpecs.Application;

public class ApiSpecAppService : IApiSpecAppService
{
    private readonly IApiSpecRepository _repo;
    private readonly OpenApiReader _reader;

    public ApiSpecAppService(IApiSpecRepository repo, OpenApiReader reader)
    {
        _repo = repo; 
        _reader = reader;
    }

    public async Task<ApiSpecDto> ImportAsync(ImportOpenApiInputDto input)
    {
        // TODO: Add authorization check when available
        // await AuthorizationService.CheckAsync(ChapiPermissions.ApiSpecs.Import);

        var (doc, rawJson, version, sha) = await _reader.ReadAsync(input.Url);

        var existing = await _repo.FindByShaAsync(sha);
        var spec = existing ?? new ApiSpec(Guid.NewGuid(), input.ProjectId, input.Url, sha, version, JsonDocument.Parse(rawJson));

        var id = await _repo.UpsertAsync(spec);

        // TODO: Publish event when event bus is available
        // await _bus.PublishAsync(new ApiSpecImportedEto(id, input.ProjectId));

        return new ApiSpecDto(id, spec.ProjectId, spec.SourceUrl, spec.Version, spec.CreatedAt);
    }

    public async Task<ApiSpecDto?> GetAsync(Guid specId)
    {
        var spec = await _repo.GetByIdAsync(specId);
        return spec == null ? null : new ApiSpecDto(spec.Id, spec.ProjectId, spec.SourceUrl, spec.Version, spec.CreatedAt);
    }
}
