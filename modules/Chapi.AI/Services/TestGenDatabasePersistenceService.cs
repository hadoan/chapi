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
using ShipMvp.Core.Attributes;
using System.IO.Compression;
using System.IO;

namespace Chapi.AI.Services;

public interface ITestGenDatabasePersistenceService
{
    Task<DatabaseOperations> SaveDatabaseOperationsAsync(TestGenInput input, ChapiCard card, string timestamp, List<TestGenFile>? files, CancellationToken cancellationToken = default);
}

public class TestGenDatabasePersistenceService : ITestGenDatabasePersistenceService
{
    private readonly IDbContext _dbContext;
    private readonly IConversationRepository _conversationRepository;
    private readonly IRunPackRepository _runPackRepository;
    private readonly ITestGenValidationService _validationService;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IRunPackFileService _runPackFileService;

    public TestGenDatabasePersistenceService(
        IDbContext dbContext,
        IConversationRepository conversationRepository,
        IRunPackRepository runPackRepository,
        ITestGenValidationService validationService,
        IGuidGenerator guidGenerator,
        IRunPackFileService runPackFileService)
    {
        _dbContext = dbContext;
        _conversationRepository = conversationRepository;
        _runPackRepository = runPackRepository;
        _validationService = validationService;
        _guidGenerator = guidGenerator;
        _runPackFileService = runPackFileService;
    }

    [UnitOfWork]
    public async Task<DatabaseOperations> SaveDatabaseOperationsAsync(TestGenInput input, ChapiCard card,  string timestamp, List<TestGenFile>? files, CancellationToken cancellationToken = default)
    {
        var dbOps = new DatabaseOperations();

        try
        {
            var projectId = Guid.Parse(input.Project.Id);
            var convGuid = _guidGenerator.Create();
            // Create or get conversation using repository
            var conversation = await CreateOrGetConversationAsync(input, convGuid, projectId, timestamp, cancellationToken);
            if (conversation != null)
            {
                dbOps.Conversations = new List<ConversationDto> { CreateConversationDto(conversation) };

                // Create message using domain methods
                var messageContent = CreateMessageContent(card);
                var message = conversation.Append(MessageRole.Assistant, messageContent, MessageCardType.Diff, JsonSerializer.Serialize(card));

                _dbContext.Set<Message>().Add(message);
                // Save conversation with new message
                await _dbContext.SaveChangesAsync(cancellationToken);
                dbOps.Messages = new List<MessageDto> { CreateMessageDto(message) };

                // Create RunPack if files are provided
                if (files?.Any() == true)
                {
                    await CreateRunPackAsync(dbOps, input, card, convGuid, message.Id, projectId, timestamp, files, cancellationToken);
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
        var conversation = new Conversation(conversationId,title, projectId);

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

        // Save RunPack to repository first
        await _runPackRepository.AddAsync(runPack, cancellationToken);

        // Create ZIP from files and use RunPackFileService to handle file storage
        var zipData = CreateZipFromFiles(files);
        var fileResult = await _runPackFileService.SaveRunPackAsync(
            zipData, 
            projectId, 
            environment, 
            runPack.Id, 
            cancellationToken);

        // Create DTOs for return
        dbOps.RunPacks = new List<RunPackDto> { CreateRunPackDto(runPack) };
        dbOps.RunPackFiles = files.Select(f => CreateRunPackFileDto(f, runPack.Id)).ToList();
        dbOps.RunPackInputs = new List<RunPackInputDto> { CreateRunPackInputDto(runPack.Input!) };
    }

    private byte[] CreateZipFromFiles(List<TestGenFile> files)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.Path);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream);
                writer.Write(file.Content ?? string.Empty);
            }
        }
        return memoryStream.ToArray();
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

    private RunPackFileDto CreateRunPackFileDto(TestGenFile file, Guid runPackId)
    {
        return new RunPackFileDto
        {
            Id = _guidGenerator.Create().ToString(),
            RunpackId = runPackId.ToString(), // Note: DTO uses "Runpack" not "RunPack"
            Path = file.Path,
            Content = file.Content ?? "",
            SizeBytes = System.Text.Encoding.UTF8.GetByteCount(file.Content ?? ""),
            Role = _validationService.ClassifyFileRole(file.Path),
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
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
