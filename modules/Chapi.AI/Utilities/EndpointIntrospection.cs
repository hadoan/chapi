using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Chapi.AI.Services;
using Chapi.EndpointCatalog.Domain;

namespace Chapi.AI.Utilities;

public static class EndpointIntrospection
{
    public static string ExtractAuth(ApiEndpoint e)
    {
        try
        {
            if (e.Security is null) return "none";
            var root = e.Security.RootElement;
            if (root.ValueKind != JsonValueKind.Array) return "none";

            var schemes = new List<string>();
            foreach (var item in root.EnumerateArray())
            {
                if (TryGetString(item, "scheme", out var s) && !string.IsNullOrWhiteSpace(s))
                    schemes.Add(s.Trim().ToLowerInvariant());
            }

            if (schemes.Count == 0) return "none";
            if (schemes.Contains("bearer")) return "bearer";
            if (schemes.Contains("oauth2")) return "oauth2";
            if (schemes.Contains("apikey")) return "apikey";
            if (schemes.Contains("basic")) return "basic";
            return string.Join("/", schemes.Distinct());
        }
        catch
        {
            return "none";
        }
    }

    public static string ExtractReq(ApiEndpoint e)
    {
        try
        {
            if (e.Request is null) return "-";
            var root = e.Request.RootElement;
            if (TryGetString(root, "contentType", out var ct) && !string.IsNullOrWhiteSpace(ct))
                return ct!;
            return "-";
        }
        catch
        {
            return "-";
        }
    }

    public static string ExtractSuccess(ApiEndpoint e)
    {
        try
        {
            if (e.Responses is null) return "-";
            var root = e.Responses.RootElement;
            if (root.ValueKind != JsonValueKind.Array) return "-";

            // Choose the lowest 2xx status; fallback to "default" or first entry.
            JsonElement? best = null;
            int bestCode = int.MaxValue;

            foreach (var r in root.EnumerateArray())
            {
                if (TryGetString(r, "status", out var statusStr) && !string.IsNullOrWhiteSpace(statusStr))
                {
                    if (int.TryParse(statusStr, out var code) && code >= 200 && code < 300)
                    {
                        if (code < bestCode) { bestCode = code; best = r; }
                    }
                    else if (best is null && string.Equals(statusStr, "default", StringComparison.OrdinalIgnoreCase))
                    {
                        best = r; bestCode = 200; // treat default as 200-ish
                    }
                    else if (best is null && !int.TryParse(statusStr, out _))
                    {
                        best = r; // weird status label; keep as last resort
                    }
                }
            }

            if (best is null)
            {
                // fallback to first response
                var first = root.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Undefined) return "-";
                best = first;
                TryGetString(first, "status", out var s); int.TryParse(s, out bestCode);
            }

            var chosen = best.Value;
            var status = TryGetString(chosen, "status", out var stat) ? stat! : "-";
            var ct = TryGetString(chosen, "contentType", out var ctype) && !string.IsNullOrWhiteSpace(ctype) ? ctype! : "-";
            var schema = TryGetProperty(chosen, "schema", out var sch) ? sch : default;
            var name = SchemaDisplayName(schema);

            return $"{status}:{ct} {name}";
        }
        catch
        {
            return "-";
        }
    }

    // ----------------- helpers -----------------

    private static bool TryGetString(JsonElement obj, string prop, out string? value)
    {
        value = null;
        if (obj.ValueKind != JsonValueKind.Object) return false;
        foreach (var p in obj.EnumerateObject())
        {
            if (string.Equals(p.Name, prop, StringComparison.Ordinal)) { value = p.Value.GetString(); return true; }
        }
        return false;
    }

    private static bool TryGetProperty(JsonElement obj, string prop, out JsonElement value)
    {
        value = default;
        if (obj.ValueKind != JsonValueKind.Object) return false;
        foreach (var p in obj.EnumerateObject())
        {
            if (string.Equals(p.Name, prop, StringComparison.Ordinal)) { value = p.Value; return true; }
        }
        return false;
    }

    private static string SchemaDisplayName(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object) return "-";

        // $ref takes precedence
        if (TryGetString(schema, "$ref", out var rref) && !string.IsNullOrWhiteSpace(rref))
        {
            // "#/components/schemas/UserDto" -> "UserDto"
            var last = rref!.Split('/').LastOrDefault();
            return string.IsNullOrWhiteSpace(last) ? "Object" : last!;
        }

        // type-based summary
        if (TryGetString(schema, "type", out var type) && !string.IsNullOrWhiteSpace(type))
        {
            var t = type!.ToLowerInvariant();
            if (t == "array")
            {
                if (TryGetProperty(schema, "items", out var items))
                {
                    var inner = SchemaDisplayName(items);
                    return inner == "-" ? "Array" : $"{inner}[]";
                }
                return "Array";
            }
            return t switch
            {
                "object" => "Object",
                "string" => "string",
                "integer" => "integer",
                "number" => "number",
                "boolean" => "boolean",
                _ => t
            };
        }

        // oneOf/anyOf/allOf quick label
        if (TryGetProperty(schema, "oneOf", out var one) && one.ValueKind == JsonValueKind.Array && one.GetArrayLength() > 0)
            return "oneOf";
        if (TryGetProperty(schema, "anyOf", out var any) && any.ValueKind == JsonValueKind.Array && any.GetArrayLength() > 0)
            return "anyOf";
        if (TryGetProperty(schema, "allOf", out var all) && all.ValueKind == JsonValueKind.Array && all.GetArrayLength() > 0)
            return "allOf";

        return "Object";
    }
}
