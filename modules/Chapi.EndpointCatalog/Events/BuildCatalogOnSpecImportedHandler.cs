using Microsoft.OpenApi.Readers;
using Chapi.SharedKernel.Events;
using Chapi.ApiSpecs.Domain;
using Chapi.EndpointCatalog.Domain;

namespace Chapi.EndpointCatalog.Events;

public class BuildCatalogOnSpecImportedHandler : ShipMvp.Core.Events.IDistributedEventHandler<ApiSpecImportedEto>, ShipMvp.Core.DependencyInjection.ITransientDependency
{
    private readonly Chapi.ApiSpecs.Domain.IApiSpecRepository _specs;
    private readonly IApiEndpointRepository _endpoints;

    public BuildCatalogOnSpecImportedHandler(Chapi.ApiSpecs.Domain.IApiSpecRepository specs, IApiEndpointRepository endpoints)
    {
        _specs = specs; _endpoints = endpoints;
    }

    public async Task HandleEventAsync(ApiSpecImportedEto eto)
    {
        var spec = await _specs.FindAsync(eto.SpecId);
        if (spec == null) return;

        var doc = new OpenApiStringReader().Read(spec.Raw.RootElement.ToString(), out _);

        var servers = doc.Servers?.Select(s => s.Url).ToList() ?? new();
        foreach (var (path, item) in doc.Paths)
        {
            foreach (var (opKey, op) in item.Operations)
            {
                var method = opKey.ToString().ToUpperInvariant();
                var dto = EndpointMapper.From(doc, servers, path, method, op, item);
                await _endpoints.UpsertAsync(spec.Id, spec.ProjectId, dto);
            }
        }
    }
}
