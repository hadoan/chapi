using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using System.Text.Json;

namespace Chapi.EndpointCatalog.Application
{
    public static class OpenApiNormalization
    {
        public static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public static JsonDocument? ToJsonDoc<T>(T value)
        {
            if (value == null) return null;
            var json = System.Text.Json.JsonSerializer.Serialize(value, JsonOpts);
            return JsonDocument.Parse(json);
        }

        public static List<string>? NormalizeServers(OpenApiDocument doc)
        {
            var servers = doc.Servers?.Select(s => s.Url).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();
            return servers?.Count > 0 ? servers : null;
        }

        public static List<Dictionary<string, List<string>>>? NormalizeSecurity(OpenApiDocument doc, OpenApiOperation op)
        {
            var reqs = (op.Security != null && op.Security.Count > 0) ? op.Security : doc.SecurityRequirements;
            if (reqs == null || reqs.Count == 0) return null;

            var list = new List<Dictionary<string, List<string>>>();
            foreach (var req in reqs)
            {
                var dict = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                foreach (var kv in req)
                {
                    var schemeKey = kv.Key.Reference?.Id ?? kv.Key.Name ?? kv.Key.Scheme ?? "unknown";
                    dict[schemeKey] = kv.Value?.ToList() ?? new List<string>();
                }
                list.Add(dict);
            }
            return list.Count > 0 ? list : null;
        }

        public static List<ParameterDto>? NormalizeParameters(OpenApiPathItem pathItem, OpenApiOperation op)
        {
            var all = new List<OpenApiParameter>();
            if (pathItem?.Parameters != null) all.AddRange(pathItem.Parameters);
            if (op?.Parameters != null) all.AddRange(op.Parameters);

            var map = new Dictionary<(string, ParameterLocation), OpenApiParameter>();
            foreach (var p in all)
            {
                var loc = p.In.GetValueOrDefault();
                map[(p.Name, loc)] = p;
            }

            var result = new List<ParameterDto>();
            foreach (var p in map.Values)
            {
                result.Add(new ParameterDto
                {
                    Name = p.Name,
                    In = p.In?.ToString().ToLowerInvariant() ?? "query",
                    Required = p.Required,
                    Deprecated = p.Deprecated,
                    Description = p.Description,
                    Schema = NormalizeSchema(p.Schema),
                    Example = p.Example,
                    Examples = p.Examples?.ToDictionary(k => k.Key, v => (object?)v.Value?.Value ?? v.Value?.Value?.ToString())
                });
            }
            return result.Count > 0 ? result : null;
        }

        public static RequestBodyDto? NormalizeRequest(OpenApiOperation op)
        {
            var rb = op?.RequestBody;
            if (rb == null) return null;

            var dto = new RequestBodyDto { Required = rb.Required };
            foreach (var kv in rb.Content)
            {
                dto.Content[kv.Key] = new BodyMediaDto
                {
                    Schema = NormalizeSchema(kv.Value.Schema),
                    Example = kv.Value.Example ?? kv.Value.Schema?.Example
                };
            }
            return dto.Content.Count > 0 ? dto : null;
        }

        public static Dictionary<string, ResponseDto>? NormalizeResponses(OpenApiOperation op)
        {
            if (op?.Responses == null || op.Responses.Count == 0) return null;

            var dict = new Dictionary<string, ResponseDto>(StringComparer.Ordinal);
            foreach (var kv in op.Responses)
            {
                var code = kv.Key;
                var resp = kv.Value;
                dict[code] = new ResponseDto
                {
                    Description = resp.Description,
                    Content = resp.Content?.ToDictionary(
                        c => c.Key,
                        c => new BodyMediaDto { Schema = NormalizeSchema(c.Value.Schema), Example = c.Value.Example ?? c.Value.Schema?.Example }),
                    Headers = resp.Headers?.ToDictionary(
                        h => h.Key,
                        h => new ParameterDto
                        {
                            Name = h.Key,
                            In = "header",
                            Required = false,
                            Description = h.Value.Description,
                            Schema = NormalizeSchema(h.Value.Schema)
                        })
                };
            }
            return dict.Count > 0 ? dict : null;
        }

        public static SchemaDto? NormalizeSchema(OpenApiSchema? s)
        {
            if (s is null) return null;

            var dto = new SchemaDto
            {
                Ref = s.Reference?.Id is string id ? $"#/components/schemas/{id}" : null,
                Type = s.Type,
                Format = s.Format,
                Nullable = s.Nullable,
                Description = s.Description,
                Enum = s.Enum?.Select(e => e?.ToString() ?? "").Where(x => x != "").ToArray(),
                Pattern = s.Pattern,
                Default = s.Default,
                Example = s.Example
            };

            // If the schema is a reference to a component schema, don't traverse into its children to avoid
            // infinite recursion caused by circular references. Return the reference-only DTO.
            if (s.Reference != null)
                return dto;

            if (s.Type == "array" && s.Items != null)
                dto.Items = NormalizeSchema(s.Items);

            if (s.Type == "object" && s.Properties?.Count > 0)
            {
                dto.Properties = s.Properties.ToDictionary(kv => kv.Key, kv => NormalizeSchema(kv.Value)!);
                if (s.Required?.Count > 0) dto.Required = s.Required.ToArray();
            }

            return dto;
        }
    }
}
