using Microsoft.OpenApi.Readers;
using Chapi.SharedKernel.Events;
using Chapi.ApiSpecs.Domain;
using Chapi.EndpointCatalog.Domain;
using Microsoft.Extensions.Logging;

namespace Chapi.EndpointCatalog.Events;


public class BuildCatalogOnSpecImportedHandler : ShipMvp.Core.EventBus.IDistributedEventHandler<ApiSpecImportedEto>
{
    private readonly Chapi.ApiSpecs.Domain.IApiSpecRepository _specs;
    private readonly IApiEndpointRepository _endpoints;
    private readonly Microsoft.Extensions.Logging.ILogger<BuildCatalogOnSpecImportedHandler> _logger;

    public BuildCatalogOnSpecImportedHandler(Chapi.ApiSpecs.Domain.IApiSpecRepository specs, IApiEndpointRepository endpoints,
        Microsoft.Extensions.Logging.ILogger<BuildCatalogOnSpecImportedHandler> logger)
    {
        _specs = specs;
        _endpoints = endpoints;
        _logger = logger;
    }

    // Re-enabled handler implementing the project's distributed event handler interface.
    // Note: If the distributed event bus isn't wired at runtime this will simply not be invoked.
    public async Task HandleAsync(ApiSpecImportedEto eto)
    {
        _logger.LogInformation("BuildCatalogOnSpecImportedHandler triggered for SpecId={SpecId} ProjectId={ProjectId}", eto.SpecId, eto.ProjectId);

        var spec = await _specs.GetByIdAsync(eto.SpecId);
        if (spec == null)
        {
            _logger.LogInformation("Spec {SpecId} not found, aborting catalog build.", eto.SpecId);
            return;
        }

        var doc = new OpenApiStringReader().Read(spec.Raw.RootElement.ToString(), out _);

        var servers = doc.Servers?.Select(s => s.Url).ToList() ?? new();
        var processed = 0;
        foreach (var (path, item) in doc.Paths)
        {
            foreach (var (opKey, op) in item.Operations)
            {
                var method = opKey.ToString().ToUpperInvariant();
                var dto = EndpointMapper.From(doc, servers, path, method, op, item);
                await _endpoints.UpsertAsync(spec.Id, spec.ProjectId, dto);
                processed++;
            }
        }

        _logger.LogInformation("Processed {Count} endpoints for SpecId={SpecId}.", processed, spec.Id);
    }
}
