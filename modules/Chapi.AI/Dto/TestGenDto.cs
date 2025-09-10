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

        [JsonPropertyName("db_ops")]
        public DatabaseOperations DbOps { get; set; } = new();
    }

    public class TestGenFile
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

    public class DatabaseOperations
    {
        [JsonPropertyName("conversations")]
        public List<ConversationRow>? Conversations { get; set; }

        [JsonPropertyName("messages")]
        public List<MessageRow> Messages { get; set; } = new();

        [JsonPropertyName("run_packs")]
        public List<RunPackRow>? RunPacks { get; set; }

        [JsonPropertyName("run_pack_files")]
        public List<RunPackFileRow>? RunPackFiles { get; set; }

        [JsonPropertyName("run_pack_inputs")]
        public List<RunPackInputRow>? RunPackInputs { get; set; }

        [JsonPropertyName("run_pack_validations")]
        public List<RunPackValidationRow>? RunPackValidations { get; set; }
    }

    public class ConversationRow
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = "";

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = "";

        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; } = "";
    }

    public class MessageRow
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("conversation_id")]
        public string ConversationId { get; set; } = "";

        [JsonPropertyName("role")]
        public string Role { get; set; } = "assistant";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("card_type")]
        public string CardType { get; set; } = "";

        [JsonPropertyName("card_payload")]
        public object? CardPayload { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = "";
    }

    public class RunPackRow
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = "";

        [JsonPropertyName("conversation_id")]
        public string ConversationId { get; set; } = "";

        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = "";

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "bash-curl";

        [JsonPropertyName("files_count")]
        public int FilesCount { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "draft";

        [JsonPropertyName("generator_version")]
        public string GeneratorVersion { get; set; } = "";

        [JsonPropertyName("card_hash")]
        public string CardHash { get; set; } = "";

        [JsonPropertyName("inputs_hash")]
        public string InputsHash { get; set; } = "";

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = "";
    }

    public class RunPackFileRow
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("runpack_id")]
        public string RunpackId { get; set; } = "";

        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("size_bytes")]
        public int SizeBytes { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = "";
    }

    public class RunPackInputRow
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("runpack_id")]
        public string RunpackId { get; set; } = "";

        [JsonPropertyName("file_roles_json")]
        public Dictionary<string, string> FileRolesJson { get; set; } = new();

        [JsonPropertyName("role_contexts_json")]
        public Dictionary<string, object> RoleContextsJson { get; set; } = new();

        [JsonPropertyName("endpoints_context")]
        public string EndpointsContext { get; set; } = "";

        [JsonPropertyName("allowed_ops")]
        public string AllowedOps { get; set; } = "curl,bash";

        [JsonPropertyName("env")]
        public string Env { get; set; } = "local";

        [JsonPropertyName("selector_output_json")]
        public Dictionary<string, object> SelectorOutputJson { get; set; } = new();

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = "";

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = "";
    }

    public class RunPackValidationRow
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("runpack_id")]
        public string RunpackId { get; set; } = "";

        [JsonPropertyName("file_path")]
        public string FilePath { get; set; } = "";

        [JsonPropertyName("rule")]
        public string Rule { get; set; } = "";

        [JsonPropertyName("passed")]
        public bool Passed { get; set; }

        [JsonPropertyName("details")]
        public object? Details { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = "";
    }
}
