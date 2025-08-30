using Microsoft.OpenApi.Models;
using System.Text.Json;
using Chapi.EndpointCatalog.Application;

namespace Chapi.EndpointCatalog;

public static class EndpointMapper
{
    public static EndpointDto From(OpenApiDocument doc, List<string> servers, string path, string method, OpenApiOperation op, OpenApiPathItem item)
    {
        var tags = op.Tags?.Select(t => t.Name).ToArray() ?? Array.Empty<string>();
        var securities = op.Security?.Select(s => s.ToString()).ToArray() ?? Array.Empty<string>();
        var parameters = op.Parameters?.Select(p => new { name = p.Name, in_ = p.In?.ToString() }).ToArray() ?? Array.Empty<object>();
        var requestBody = op.RequestBody?.Content?.ToDictionary(k => k.Key, v => new { schema = "TODO" }) as object;
        var responses = op.Responses?.ToDictionary(k => k.Key, v => new { description = v.Value.Description }) as object ?? new Dictionary<string, object>();
        
        var dto = new EndpointDto(Guid.Empty, Guid.Empty, method, path, op.OperationId, op.Summary, op.Description, tags,
            servers, securities, parameters, requestBody, responses);
        return dto;
    }
}
