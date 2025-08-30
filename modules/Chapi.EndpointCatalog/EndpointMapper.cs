using Microsoft.OpenApi.Models;
using System.Text.Json;

namespace Chapi.EndpointCatalog;

public static class EndpointMapper
{
    public static EndpointDto From(OpenApiDocument doc, List<string> servers, string path, string method, OpenApiOperation op, OpenApiPathItem item)
    {
        var dto = new EndpointDto(Guid.Empty, Guid.Empty, method, path, op.OperationId, op.Summary, op.Description, op.Tags?.Select(t => t.Name).ToArray() ?? Array.Empty<string>(),
            servers, op.Security?.Select(s => s.ToString()) ?? Array.Empty<string>(), op.Parameters?.Select(p => new { name = p.Name, in_ = p.In.ToString() }) ?? Array.Empty<object>(),
            op.RequestBody?.Content?.ToDictionary(k => k.Key, v => new { schema = v.Value.Schema?.SerializeAsV3(new Microsoft.OpenApi.Writers.OpenApiJsonWriter(new System.IO.StringWriter())) }),
            op.Responses?.ToDictionary(k => k.Key, v => new { description = v.Value.Description }) ?? new Dictionary<string, object?>());
        return dto;
    }
}
