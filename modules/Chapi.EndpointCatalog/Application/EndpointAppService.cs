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

        try
        {
            if (e.Servers != null && e.Servers.RootElement.ValueKind == JsonValueKind.Array)
                dto.Servers = JsonSerializer.Deserialize<List<string>>(e.Servers.RootElement.GetRawText(), OpenApiNormalization.JsonOpts);

            if (e.Security != null && e.Security.RootElement.ValueKind == JsonValueKind.Array)
                dto.Security = JsonSerializer.Deserialize<List<Dictionary<string, List<string>>>>(e.Security.RootElement.GetRawText(), OpenApiNormalization.JsonOpts);

            if (e.Parameters != null && e.Parameters.RootElement.ValueKind == JsonValueKind.Array)
            {
                // Create a JsonSerializerOptions that can deserialize PascalCase JSON to PascalCase properties
                var deserializationOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var parameters = JsonSerializer.Deserialize<List<ParameterDto>>(e.Parameters.RootElement.GetRawText(), deserializationOptions);
                // Filter out invalid parameters with null names
                dto.Parameters = parameters?.Where(p => !string.IsNullOrEmpty(p.Name)).ToList();
            }

            if (e.Request != null && e.Request.RootElement.ValueKind == JsonValueKind.Object)
                dto.Request = JsonSerializer.Deserialize<RequestBodyDto>(e.Request.RootElement.GetRawText(), OpenApiNormalization.JsonOpts);

            if (e.Responses != null && e.Responses.RootElement.ValueKind == JsonValueKind.Object)
                dto.Responses = JsonSerializer.Deserialize<Dictionary<string, ResponseDto>>(e.Responses.RootElement.GetRawText(), OpenApiNormalization.JsonOpts);
        }
        catch (JsonException ex)
        {
            // Log the exception and continue with empty collections
            // You might want to add proper logging here
            System.Diagnostics.Debug.WriteLine($"Failed to deserialize endpoint data: {ex.Message}");
        }

        return dto;
    }
}
