using System;
using System.Collections.Generic;

namespace Chapi.EndpointCatalog.Application
{
    public sealed class EndpointDto
    {
        public string Method { get; set; } = default!;
        public string Path { get; set; } = default!;
        public string? OperationId { get; set; }
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public List<string>? Tags { get; set; }

        public List<string>? Servers { get; set; }
        public List<Dictionary<string, List<string>>>? Security { get; set; }

        public List<ParameterDto>? Parameters { get; set; }
        public RequestBodyDto? Request { get; set; }
        public Dictionary<string, ResponseDto>? Responses { get; set; }
    }

    public sealed class ParameterDto
    {
        public string Name { get; set; } = default!;
        public string In { get; set; } = default!;
        public bool Required { get; set; }
        public bool Deprecated { get; set; }
        public string? Description { get; set; }
        public SchemaDto? Schema { get; set; }
        public object? Example { get; set; }
        public Dictionary<string, object>? Examples { get; set; }
    }

    public sealed class RequestBodyDto
    {
        public bool Required { get; set; }
        public Dictionary<string, BodyMediaDto> Content { get; set; } = new();
    }

    public sealed class BodyMediaDto
    {
        public SchemaDto? Schema { get; set; }
        public object? Example { get; set; }
    }

    public sealed class ResponseDto
    {
        public string? Description { get; set; }
        public Dictionary<string, BodyMediaDto>? Content { get; set; }
        public Dictionary<string, ParameterDto>? Headers { get; set; }
    }

    public sealed class SchemaDto
    {
        public string? Ref { get; set; }
        public string? Type { get; set; }
        public string? Format { get; set; }
        public bool? Nullable { get; set; }
        public string? Description { get; set; }
        public string[]? Enum { get; set; }
        public SchemaDto? Items { get; set; }
        public Dictionary<string, SchemaDto>? Properties { get; set; }
        public string[]? Required { get; set; }
        public string? Pattern { get; set; }
        public decimal? Minimum { get; set; }
        public decimal? Maximum { get; set; }
        public object? Default { get; set; }
        public object? Example { get; set; }
    }
}
