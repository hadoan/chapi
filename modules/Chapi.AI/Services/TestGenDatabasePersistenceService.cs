using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Chapi.AI.Dto;
using Chat.Domain;
using RunPack.Domain;
using Chat.Infrastructure;
using RunPack.Infrastructure;
using ShipMvp.Core.Persistence;
using ShipMvp.Core.Abstractions;

namespace Chapi.AI.Services;

public interface ITestGenDatabasePersistenceService
{
    Task<DatabaseOperations> SaveDatabaseOperationsAsync(TestGenInput input, ChapiCard card, string conversationId, string messageId, string timestamp, List<TestGenFile>? files, CancellationToken cancellationToken = default);
}

public class TestGenDatabasePersistenceService : ITestGenDatabasePersistenceService
{
    private readonly IDbContext _dbContext;
    private readonly IConversationRepository _conversationRepository;
    private readonly IRunPackRepository _runPackRepository;
    private readonly ITestGenValidationService _validationService;
    private readonly IGuidGenerator _guidGenerator;

    public TestGenDatabasePersistenceService(
        IDbContext dbContext,
        IConversationRepository conversationRepository,
        IRunPackRepository runPackRepository,
        ITestGenValidationService validationService,
        IGuidGenerator guidGenerator)
    {
        _dbContext = dbContext;
        _conversationRepository = conversationRepository;
        _runPackRepository = runPackRepository;
        _validationService = validationService;
        _guidGenerator = guidGenerator;
    }

    public async Task<DatabaseOperations> SaveDatabaseOperationsAsync(TestGenInput input, ChapiCard card, string conversationId, string messageId, string timestamp, List<TestGenFile>? files, CancellationToken cancellationToken = default)
    {
        var dbOps = new DatabaseOperations();

        try
        {
            // Parse GUIDs from string IDs
            var convGuid = Guid.Parse(conversationId);
            var msgGuid = Guid.Parse(messageId);

            // For now, use a default project ID since we don't have it in the input
            var projectId = _guidGenerator.Create(); // This should come from the actual project context

            // Create or get conversation using repository
            var conversation = await CreateOrGetConversationAsync(input, convGuid, projectId, timestamp, cancellationToken);
            if (conversation != null)
            {
                dbOps.Conversations = new List<ConversationDto> { CreateConversationDto(conversation) };

                // Create message using domain methods
                var messageContent = CreateMessageContent(card);
                var message = conversation.Append(MessageRole.Assistant, messageContent, MessageCardType.Diff, JsonSerializer.Serialize(card));

                // Save conversation with new message
                await _dbContext.SaveChangesAsync(cancellationToken);
                dbOps.Messages = new List<MessageDto> { CreateMessageDto(message) };

                // Create RunPack if files are provided
                if (files?.Any() == true)
                {
                    await CreateRunPackAsync(dbOps, input, card, convGuid, msgGuid, projectId, timestamp, files, cancellationToken);
                }
            }

            return dbOps;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save database operations: {ex.Message}", ex);
        }
    }

    private string CreateMessageContent(ChapiCard card)
    {
        // Create a meaningful message content from the card
        var content = card.Heading;
        if (!string.IsNullOrEmpty(content)) return content;

        // Fallback to plan summary
        if (card.Plan.Any())
        {
            return string.Join("\n", card.Plan);
        }

        // Fallback to file list
        if (card.Files.Any())
        {
            return $"Generated {card.Files.Count} files";
        }

        return "Test generation completed";
    }

    private ConversationDto CreateConversationDto(Conversation conversation)
    {
        return new ConversationDto
        {
            Id = conversation.Id.ToString(),
            Title = conversation.Title,
            ProjectId = conversation.ProjectId.ToString(),
            CreatedAt = conversation.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            UpdatedAt = conversation.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    private MessageDto CreateMessageDto(Message message)
    {
        return new MessageDto
        {
            Id = message.Id.ToString(),
            ConversationId = message.ConversationId.ToString(),
            Role = message.Role.ToString(),
            Content = message.Content,
            CardType = message.CardType.ToString(),
            CardPayload = message.CardPayload,
            CreatedAt = message.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    private async Task<Conversation?> CreateOrGetConversationAsync(TestGenInput input, Guid conversationId, Guid projectId, string timestamp, CancellationToken cancellationToken)
    {
        // Check if conversation ID is provided in input and exists
        if (!string.IsNullOrEmpty(input.Chat.ConversationId) && Guid.TryParse(input.Chat.ConversationId, out var existingConvGuid))
        {
            var existing = await _conversationRepository.GetByIdAsync(existingConvGuid, cancellationToken);
            if (existing != null)
            {
                return existing;
            }
        }

        // Create new conversation using the domain factory method
        var title = input.Chat.ConversationTitle ?? $"{input.SelectedEndpoint.Method} {input.SelectedEndpoint.Path} — tests";
        var conversation = Conversation.Create(title, projectId);

        await _conversationRepository.AddAsync(conversation, cancellationToken);
        return conversation;
    }

    private async Task CreateRunPackAsync(DatabaseOperations dbOps, TestGenInput input, ChapiCard card, Guid conversationId, Guid messageId, Guid projectId, string timestamp, List<TestGenFile> files, CancellationToken cancellationToken)
    {
        // Create RunPack using domain factory method
        var runPack = RunPack.Domain.RunPack.CreateFromConversation(_guidGenerator.Create(), projectId, conversationId, "json-ir");

        // Set additional properties using domain methods
        runPack.MessageId = messageId;
        runPack.SetGeneratorVersion(input.Options.GeneratorVersion);

        // Create hashes
        var cardHash = JsonSerializer.Serialize(card).GetHashCode().ToString();
        var inputsHash = JsonSerializer.Serialize(input).GetHashCode().ToString();
        runPack.SetHashes(cardHash, inputsHash);

        // Add files to RunPack
        foreach (var file in files)
        {
            var fileId = _guidGenerator.Create(); // In real implementation, this would come from a File entity
            var role = _validationService.ClassifyFileRole(file.Path);
            var runPackFile = RunPackFile.Create(runPack.Id, _guidGenerator.Create(), fileId, role);
            runPack.AddFile(runPackFile);
        }

        // Set RunPack input using domain method
        var fileRoles = files.ToDictionary(f => f.Path, f => _validationService.ClassifyFileRole(f.Path));
        var roleContexts = files.ToDictionary(f => f.Path, f => f.Content ?? "");
        var endpointsContext = JsonSerializer.Serialize(input.SelectedEndpoint);
        var allowedOps = string.Join(",", input.Options.EnvPlaceholders);
        var environment = input.Options.FileBaseDir;

        runPack.SetInput(
            JsonSerializer.Serialize(fileRoles),
            JsonSerializer.Serialize(roleContexts),
            endpointsContext,
            allowedOps,
            environment);

        // Save to repository
        await _runPackRepository.AddAsync(runPack, cancellationToken);

        // Create DTO for return
        dbOps.RunPacks = new List<RunPackDto> { CreateRunPackDto(runPack) };
        dbOps.RunPackFiles = runPack.Files.Select(CreateRunPackFileDto).ToList();
        dbOps.RunPackInputs = new List<RunPackInputDto> { CreateRunPackInputDto(runPack.Input!) };
    }

    private RunPackDto CreateRunPackDto(RunPack.Domain.RunPack runPack)
    {
        return new RunPackDto
        {
            Id = runPack.Id.ToString(),
            ProjectId = runPack.ProjectId.ToString(),
            ConversationId = runPack.ConversationId?.ToString(),
            MessageId = runPack.MessageId.ToString(),
            Mode = runPack.Mode,
            FilesCount = runPack.FilesCount,
            Status = runPack.Status,
            GeneratorVersion = runPack.GeneratorVersion,
            CardHash = runPack.CardHash,
            InputsHash = runPack.InputsHash,
            CreatedAt = runPack.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    private RunPackFileDto CreateRunPackFileDto(RunPackFile runPackFile)
    {
        return new RunPackFileDto
        {
            Id = runPackFile.Id.ToString(),
            RunpackId = runPackFile.RunPackId.ToString(), // Note: DTO uses "Runpack" not "RunPack"
            Path = "", // This would need to be derived from the File entity
            Content = "", // This would need to be derived from the File entity
            SizeBytes = 0, // This would need to be calculated
            Role = runPackFile.Role,
            CreatedAt = runPackFile.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    private RunPackInputDto CreateRunPackInputDto(RunPackInput input)
    {
        return new RunPackInputDto
        {
            Id = input.Id.ToString(),
            RunpackId = input.RunPackId.ToString(), // Note: DTO uses "Runpack" not "RunPack"
            FileRolesJson = JsonSerializer.Deserialize<Dictionary<string, string>>(input.FileRolesJson) ?? new Dictionary<string, string>(),
            RoleContextsJson = JsonSerializer.Deserialize<Dictionary<string, object>>(input.RoleContextsJson) ?? new Dictionary<string, object>(),
            EndpointsContext = input.EndpointsContext,
            AllowedOps = input.AllowedOps,
            Environment = input.Environment,
            SelectorOutputJson = new Dictionary<string, object>(), // Default empty
            Notes = $"Generated from test generation process", // Default note
            CreatedAt = input.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }
}
