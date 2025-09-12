using System;
using System.Collections.Generic;

namespace Chapi.AI.Dto
{
    public class DatabaseOperations
    {
        public List<ConversationDto>? Conversations { get; set; }
        public List<MessageDto>? Messages { get; set; }
        public List<RunPackDto>? RunPacks { get; set; }
        public List<RunPackFileDto>? RunPackFiles { get; set; }
        public List<RunPackInputDto>? RunPackInputs { get; set; }
        public List<RunPackValidationDto>? RunPackValidations { get; set; }
    }

    public class ConversationDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class MessageDto
    {
        public string Id { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public string? CardPayload { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class RunPackDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string? ConversationId { get; set; }
        public string? MessageId { get; set; }
        public string Mode { get; set; } = string.Empty;
        public int FilesCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? GeneratorVersion { get; set; }
        public string? CardHash { get; set; }
        public string? InputsHash { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class RunPackFileDto
    {
        public string Id { get; set; } = string.Empty;
        public string RunpackId { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int SizeBytes { get; set; }
        public string Role { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class RunPackInputDto
    {
        public string Id { get; set; } = string.Empty;
        public string RunpackId { get; set; } = string.Empty;
        public Dictionary<string, string>? FileRolesJson { get; set; }
        public Dictionary<string, object>? RoleContextsJson { get; set; }
        public string EndpointsContext { get; set; } = string.Empty;
        public string AllowedOps { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public Dictionary<string, object>? SelectorOutputJson { get; set; }
        public string? Notes { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class RunPackValidationDto
    {
        public string Id { get; set; } = string.Empty;
        public string RunpackId { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Rule { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}
