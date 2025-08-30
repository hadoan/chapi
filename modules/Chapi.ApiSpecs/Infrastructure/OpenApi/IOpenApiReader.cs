using Microsoft.OpenApi.Models;

namespace Chapi.ApiSpecs.Infrastructure.OpenApi;

public interface IOpenApiReader
{
    Task<(OpenApiDocument Doc, string RawJson, string? Version, string Sha)> ReadAsync(string url);
}
