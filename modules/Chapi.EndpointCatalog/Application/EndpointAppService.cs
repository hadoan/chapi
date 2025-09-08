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

        var dto = new EndpointDto
        {
            Method = e.Method,
            Path = e.Path,
            OperationId = e.OperationId,
            Summary = e.Summary,
            Description = e.Description,
            Tags = e.Tags?.ToList()
        };

        if (e.Servers != null)
            dto.Servers = JsonSerializer.Deserialize<List<string>>(e.Servers.RootElement.GetRawText(), OpenApiNormalization.JsonOpts);
        if (e.Security != null)
            dto.Security = JsonSerializer.Deserialize<List<Dictionary<string, List<string>>>>(e.Security.RootElement.GetRawText(), OpenApiNormalization.JsonOpts);
        if (e.Parameters != null)
            dto.Parameters = JsonSerializer.Deserialize<List<ParameterDto>>(e.Parameters.RootElement.GetRawText(), OpenApiNormalization.JsonOpts);
        if (e.Request != null)
            dto.Request = JsonSerializer.Deserialize<RequestBodyDto>(e.Request.RootElement.GetRawText(), OpenApiNormalization.JsonOpts);
        if (e.Responses != null)
            dto.Responses = JsonSerializer.Deserialize<Dictionary<string, ResponseDto>>(e.Responses.RootElement.GetRawText(), OpenApiNormalization.JsonOpts);

        return dto;
    }
}
