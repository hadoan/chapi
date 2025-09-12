using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chapi.AI.Dto
{
    public class TestGenRequest
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

    public class TestGenInput
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "FILES";

        [JsonPropertyName("project")]
        public ProjectInfo Project { get; set; } = new();

        [JsonPropertyName("chat")]
        public ChatInfo Chat { get; set; } = new();

        [JsonPropertyName("selectedEndpoint")]
        public SelectedEndpoint SelectedEndpoint { get; set; } = new();

        [JsonPropertyName("authProfile")]
        public AuthProfile AuthProfile { get; set; } = new();

        [JsonPropertyName("options")]
        public TestGenOptions Options { get; set; } = new();

        [JsonPropertyName("user_query")]
        public string UserQuery { get; set; } = "";
    }

    public class ProjectInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
    }

    public class ChatInfo
    {
        [JsonPropertyName("conversation_id")]
        public string? ConversationId { get; set; }

        [JsonPropertyName("conversation_title")]
        public string? ConversationTitle { get; set; }
    }

    public class SelectedEndpoint
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; } = "GET";

        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("requiresAuth")]
        public bool RequiresAuth { get; set; }

        [JsonPropertyName("successCode")]
        public int SuccessCode { get; set; } = 200;

        [JsonPropertyName("requestSchemaHint")]
        public string RequestSchemaHint { get; set; } = "none";
    }

    public class AuthProfile
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "NONE";

        [JsonPropertyName("config")]
        public Dictionary<string, string> Config { get; set; } = new();
    }

    public class TestGenOptions
    {
        [JsonPropertyName("includeForbidden")]
        public bool IncludeForbidden { get; set; } = true;

        [JsonPropertyName("envPlaceholders")]
        public List<string> EnvPlaceholders { get; set; } = new() { "BASE_URL" };

        [JsonPropertyName("fileBaseDir")]
        public string FileBaseDir { get; set; } = "tests/endpoint";

        [JsonPropertyName("useJq")]
        public bool UseJq { get; set; } = true;

        [JsonPropertyName("generator_version")]
        public string GeneratorVersion { get; set; } = "testgen@2025-09-10";
    }

    public class TestGenResponse
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "Chapi";

        [JsonPropertyName("card")]
        public ChapiCard Card { get; set; } = new();

        [JsonPropertyName("files")]
        public List<TestGenFile>? Files { get; set; }

        [JsonPropertyName("dbOps")]
        public DatabaseOperations? DbOps { get; set; }
    }

    public class TestGenFile
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

}
