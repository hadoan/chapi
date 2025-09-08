using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using Chapi.EndpointCatalog.Application;

namespace Chapi.EndpointCatalog;

public static class EndpointMapper
{
    public static EndpointDto From(OpenApiDocument doc, List<string>? servers, string path, string method, OpenApiOperation op, OpenApiPathItem item)
    {
        var tags = op.Tags?.Select(t => t.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();

        var dto = new EndpointDto
        {
            Method = method,
            Path = path,
            OperationId = op.OperationId,
            Summary = op.Summary,
            Description = op.Description,
            Tags = tags,

            Servers = servers ?? OpenApiNormalization.NormalizeServers(doc),
            Security = OpenApiNormalization.NormalizeSecurity(doc, op),
            Parameters = OpenApiNormalization.NormalizeParameters(item, op),
            Request = OpenApiNormalization.NormalizeRequest(op),
            Responses = OpenApiNormalization.NormalizeResponses(op)
        };

        return dto;
    }
}
