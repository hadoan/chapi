using System.Text;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;
using Microsoft.Extensions.Http;
using ShipMvp.Core.Exceptions;
using System.Security.Cryptography;

namespace Chapi.ApiSpecs.Infrastructure.OpenApi;

public class OpenApiReader : IOpenApiReader, ShipMvp.Core.DependencyInjection.ITransientDependency
{
    private readonly IHttpClientFactory _http;
    public OpenApiReader(IHttpClientFactory http) { _http = http; }

    public async Task<(OpenApiDocument, string, string?, string)> ReadAsync(string url)
    {
        var client = _http.CreateClient();
        await using var stream = await client.GetStreamAsync(url);

        var reader = new OpenApiStreamReader(new OpenApiReaderSettings {
            ReferenceResolution = ReferenceResolutionSetting.ResolveLocalReferences
        });
        var doc = reader.Read(stream, out var diag);
        if (diag?.Errors?.Count > 0) throw new UserFriendlyException("OpenAPI parse errors: " + string.Join(" | ", diag.Errors.Select(e => e.Message)));

        var raw = Serialize(doc); var sha = Sha256(raw);
        return (doc, raw, doc.Info?.Version, sha);
    }

    private static string Serialize(OpenApiDocument doc)
    {
        var sb = new StringBuilder(); using var sw = new StringWriter(sb);
        var w = new OpenApiJsonWriter(sw); doc.SerializeAsV3(w); return sb.ToString();
    }
    private static string Sha256(string s){ using var sha=SHA256.Create(); return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s))); }
}
