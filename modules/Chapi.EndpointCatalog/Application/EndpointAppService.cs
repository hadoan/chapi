using System.Text.Json;
using Chapi.EndpointCatalog.Domain;

namespace Chapi.EndpointCatalog.Application;

public class EndpointAppService : IEndpointAppService
{
    private readonly IApiEndpointRepository _repo;

    public EndpointAppService(IApiEndpointRepository repo) { _repo = repo; }

    public async Task<List<EndpointBriefDto>> ListAsync(Guid projectId, string? tag = null, string? search = null)
    {
        var items = await _repo.ListByProjectAsync(projectId, tag, search);
        return items.Select(x => new EndpointBriefDto(x.Id, x.Method, x.Path, x.Summary, x.Tags)).ToList();
    }

    public async Task<EndpointDto> GetAsync(Guid endpointId)
    {
        var e = await _repo.FindAsync(endpointId);
        if (e == null) throw new KeyNotFoundException();
        return new EndpointDto(e.Id, e.SpecId, e.Method, e.Path, e.OperationId, e.Summary, e.Description, e.Tags, e.Servers.RootElement.ToString(), e.Security.RootElement.ToString(), e.Parameters.RootElement.ToString(), e.Request?.RootElement.ToString(), e.Responses.RootElement.ToString());
    }
}
