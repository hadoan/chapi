using System;
using System.Text.Json.Serialization;

namespace Chapi.AI.Dto
{
    public class ApiTestGenerateRequest
    {
        [JsonPropertyName("user_query")]
        public string? UserQuery { get; set; }

        [JsonPropertyName("projectId")]
        public Guid ProjectId { get; set; }

        [JsonPropertyName("max_files")]
        public int? MaxFiles { get; set; }

        [JsonPropertyName("openApiJson")]
        public string? OpenApiJson { get; set; }
    }
}
