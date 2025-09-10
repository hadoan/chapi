using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Chapi.AI.Dto;

namespace Chapi.AI.Services
{
    public interface ITestGenDatabaseService
    {
        DatabaseOperations CreateDatabaseOperations(TestGenInput input, ChapiCard card, string conversationId, string messageId, string timestamp, List<TestGenFile>? files);
    }

    public class TestGenDatabaseService : ITestGenDatabaseService
    {
        private readonly ILogger<TestGenDatabaseService> _logger;
        private readonly ITestGenValidationService _validationService;

        public TestGenDatabaseService(ILogger<TestGenDatabaseService> logger, ITestGenValidationService validationService)
        {
            _logger = logger;
            _validationService = validationService;
        }

        public DatabaseOperations CreateDatabaseOperations(TestGenInput input, ChapiCard card, string conversationId, string messageId, string timestamp, List<TestGenFile>? files)
        {
            var dbOps = new DatabaseOperations();

            // Create conversation if needed
            CreateConversationIfNeeded(dbOps, input, conversationId, timestamp);

            // Create message
            CreateMessage(dbOps, conversationId, messageId, input, card, timestamp);

            // Create run pack if in FILES mode
            if (input.Mode == "FILES" && files != null)
            {
                CreateRunPackOperations(dbOps, input, card, conversationId, messageId, timestamp, files);
            }

            return dbOps;
        }

        private void CreateConversationIfNeeded(DatabaseOperations dbOps, TestGenInput input, string conversationId, string timestamp)
        {
            if (string.IsNullOrEmpty(input.Chat.ConversationId))
            {
                dbOps.Conversations = new List<ConversationRow>
                {
                    new ConversationRow
                    {
                        Id = conversationId,
                        ProjectId = input.Project.Id,
                        Title = input.Chat.ConversationTitle ?? $"{input.SelectedEndpoint.Method} {input.SelectedEndpoint.Path} — tests",
                        CreatedAt = timestamp,
                        UpdatedAt = timestamp
                    }
                };
            }
        }

        private void CreateMessage(DatabaseOperations dbOps, string conversationId, string messageId, TestGenInput input, ChapiCard card, string timestamp)
        {
            dbOps.Messages = new List<MessageRow>
            {
                new MessageRow
                {
                    Id = messageId,
                    ConversationId = conversationId,
                    Role = "assistant",
                    Content = $"Generated {input.SelectedEndpoint.Method} tests for {input.SelectedEndpoint.Path} using {input.AuthProfile.Type} authentication.",
                    CardType = input.Mode == "CARD" ? "plan" : "run",
                    CardPayload = card,
                    CreatedAt = timestamp
                }
            };
        }

        private void CreateRunPackOperations(DatabaseOperations dbOps, TestGenInput input, ChapiCard card, string conversationId, string messageId, string timestamp, List<TestGenFile> files)
        {
            var runPackId = Guid.NewGuid().ToString();

            // Calculate hashes
            var cardJson = JsonSerializer.Serialize(card, new JsonSerializerOptions { WriteIndented = false });
            var cardHash = ComputeSha256Hash(cardJson);

            var inputsJson = JsonSerializer.Serialize(new { input.SelectedEndpoint, input.AuthProfile, input.Options });
            var inputsHash = ComputeSha256Hash(inputsJson);

            // Create run pack
            CreateRunPack(dbOps, runPackId, input, conversationId, messageId, timestamp, files, cardHash, inputsHash);

            // Create run pack files
            CreateRunPackFiles(dbOps, runPackId, files, timestamp);

            // Create run pack inputs
            CreateRunPackInputs(dbOps, runPackId, input, files, timestamp);

            // Create validations
            CreateRunPackValidations(dbOps, runPackId, files, input, timestamp);
        }

        private void CreateRunPack(DatabaseOperations dbOps, string runPackId, TestGenInput input, string conversationId, string messageId, string timestamp, List<TestGenFile> files, string cardHash, string inputsHash)
        {
            dbOps.RunPacks = new List<RunPackRow>
            {
                new RunPackRow
                {
                    Id = runPackId,
                    ProjectId = input.Project.Id,
                    ConversationId = conversationId,
                    MessageId = messageId,
                    Mode = "json-ir",
                    FilesCount = files.Count,
                    Status = "draft",
                    GeneratorVersion = input.Options.GeneratorVersion,
                    CardHash = cardHash,
                    InputsHash = inputsHash,
                    CreatedAt = timestamp
                }
            };
        }

        private void CreateRunPackFiles(DatabaseOperations dbOps, string runPackId, List<TestGenFile> files, string timestamp)
        {
            dbOps.RunPackFiles = files.Select(file => new RunPackFileRow
            {
                Id = Guid.NewGuid().ToString(),
                RunpackId = runPackId,
                Path = file.Path,
                Content = file.Content,
                SizeBytes = Encoding.UTF8.GetByteCount(file.Content),
                Role = _validationService.ClassifyFileRole(file.Path),
                CreatedAt = timestamp
            }).ToList();
        }

        private void CreateRunPackInputs(DatabaseOperations dbOps, string runPackId, TestGenInput input, List<TestGenFile> files, string timestamp)
        {
            var fileRoles = files.ToDictionary(f => f.Path, f => _validationService.ClassifyFileRole(f.Path));

            dbOps.RunPackInputs = new List<RunPackInputRow>
            {
                new RunPackInputRow
                {
                    Id = Guid.NewGuid().ToString(),
                    RunpackId = runPackId,
                    FileRolesJson = fileRoles,
                    RoleContextsJson = new Dictionary<string, object>
                    {
                        ["ENDPOINT"] = new
                        {
                            method = input.SelectedEndpoint.Method,
                            path = input.SelectedEndpoint.Path,
                            requiresAuth = input.SelectedEndpoint.RequiresAuth
                        }
                    },
                    EndpointsContext = $"{input.SelectedEndpoint.Method} {input.SelectedEndpoint.Path} (auth: {input.AuthProfile.Type.ToLower()})",
                    AllowedOps = "curl,bash",
                    Env = "local",
                    SelectorOutputJson = new Dictionary<string, object>
                    {
                        ["selectedEndpointId"] = input.SelectedEndpoint.Id ?? ""
                    },
                    Notes = $"Auth profile: {input.AuthProfile.Name} ({input.AuthProfile.Type})",
                    CreatedAt = timestamp
                }
            };
        }

        private void CreateRunPackValidations(DatabaseOperations dbOps, string runPackId, List<TestGenFile> files, TestGenInput input, string timestamp)
        {
            dbOps.RunPackValidations = _validationService.CreateValidations(runPackId, files, input.SelectedEndpoint, input.Options, timestamp);
        }

        private string ComputeSha256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }
    }
}
