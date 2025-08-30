using System.Text;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;
using Microsoft.Extensions.Http;
using System.Security.Cryptography;

namespace Chapi.ApiSpecs.Infrastructure.OpenApi;

public class OpenApiReader
{
    private readonly IHttpClientFactory _http;
    public OpenApiReader(IHttpClientFactory http) { _http = http; }

    public async Task<(OpenApiDocument, string, string?, string)> ReadAsync(string url)
    {
        var client = _http.CreateClient();
        var content = await client.GetStringAsync(url);

        var reader = new OpenApiStreamReader(new OpenApiReaderSettings {
            ReferenceResolution = ReferenceResolutionSetting.ResolveLocalReferences
        });

        // Try reading as-is
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var doc = reader.Read(ms, out var diag);

        if (diag?.Errors?.Count > 0)
        {
            // Attempt a best-effort repair: sanitize component schema keys and update $ref occurrences in the serialized output
            try
            {
                var raw = Serialize(doc);

                // Build mapping of old -> sanitized keys
                var schemas = doc.Components?.Schemas?.Keys?.ToList() ?? new List<string>();
                var mapping = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var key in schemas)
                {
                    // Allowed chars: A-Z a-z 0-9 . - _
                    var sanitized = System.Text.RegularExpressions.Regex.Replace(key, "[^A-Za-z0-9.\\-_]", "_");
                    var unique = sanitized;
                    var i = 1;
                    while (mapping.Values.Contains(unique))
                    {
                        unique = sanitized + "_" + i++;
                    }
                    mapping[key] = unique;
                }

                if (mapping.Count > 0)
                {
                    // Replace schema keys and references in the raw JSON/YAML text
                    foreach (var kv in mapping)
                    {
                        raw = raw.Replace($"\"{kv.Key}\"", $"\"{kv.Value}\"");
                        raw = raw.Replace($"#/components/schemas/{kv.Key}", $"#/components/schemas/{kv.Value}");
                    }

                    // Reparse the repaired document
                    using var ms2 = new MemoryStream(Encoding.UTF8.GetBytes(raw));
                    var doc2 = reader.Read(ms2, out var diag2);
                    if (diag2?.Errors?.Count > 0)
                    {
                        throw new InvalidOperationException("OpenAPI parse errors: " + string.Join(" | ", diag.Errors.Select(e => e.Message)));
                    }

                    var sha2 = Sha256(raw);
                    return (doc2, raw, doc2.Info?.Version, sha2);
                }
            }
            catch
            {
                // Fall through to original exception below
            }

            throw new InvalidOperationException("OpenAPI parse errors: " + string.Join(" | ", diag.Errors.Select(e => e.Message)));
        }

        var rawOk = Serialize(doc);
        var shaOk = Sha256(rawOk);
        return (doc, rawOk, doc.Info?.Version, shaOk);
    }

    private static string Serialize(OpenApiDocument doc)
    {
        var sb = new StringBuilder(); using var sw = new StringWriter(sb);
        var w = new OpenApiJsonWriter(sw); doc.SerializeAsV3(w); return sb.ToString();
    }
    private static string Sha256(string s){ using var sha=SHA256.Create(); return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s))); }
}
