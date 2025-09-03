using Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShipMvp.Core.Persistence;

namespace Chat.Infrastructure.Persistence;

public class ConversationRepository : IConversationRepository
{
    private readonly IDbContext _db;
    private readonly DbSet<Conversation> _set;
    private readonly ILogger<ConversationRepository> _logger;

    public ConversationRepository(IDbContext db, ILogger<ConversationRepository> logger)
    {
        _db = db;
        _set = db.Set<Conversation>();
        _logger = logger;
    }
    public IQueryable<Conversation> Query() => _set.AsQueryable();
    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct) => await _set.FirstOrDefaultAsync(c => c.Id == id, ct);
    public async Task<IEnumerable<Conversation>> GetAllAsync(CancellationToken ct) => await _set.ToListAsync(ct);
    public async Task<Conversation> AddAsync(Conversation entity, CancellationToken ct)
    {
        await _set.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return entity;
    }
    public async Task<Conversation> UpdateAsync(Conversation entity, CancellationToken ct)
    {
        _logger.LogInformation("Starting UpdateAsync for Conversation {ConversationId} with ConcurrencyStamp {ConcurrencyStamp}",
            entity.Id, entity.ConcurrencyStamp);

        // Update the conversation's UpdatedAt timestamp
        entity.GetType().GetProperty("UpdatedAt")?.SetValue(entity, DateTime.UtcNow);
        _set.Update(entity);

        try
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Successfully updated Conversation {ConversationId}", entity.Id);
            return entity;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict for Conversation {ConversationId}", entity.Id);

            // Reload the conversation to get the current state
            _db.ChangeTracker.Clear();
            var reloadedEntity = await _set.FirstOrDefaultAsync(c => c.Id == entity.Id, ct);

            if (reloadedEntity == null)
            {
                _logger.LogError("Conversation {ConversationId} not found after concurrency conflict", entity.Id);
                throw new InvalidOperationException("Conversation not found after update attempt");
            }

            _logger.LogInformation("Reloaded Conversation {ConversationId} after concurrency conflict",
                reloadedEntity.Id);

            return reloadedEntity;
        }
    }
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _set.FindAsync(new object?[] { id }, ct).AsTask();
        if (entity is null) return;
        _set.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await _set.AnyAsync(c => c.Id == conversationId, cancellationToken);
    }

    public async Task AddMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding single message to Conversation {ConversationId}", message.ConversationId);
        await _db.Set<Message>().AddAsync(message, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMessagesAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();
        if (!messageList.Any()) return;

        var conversationId = messageList.First().ConversationId;
        _logger.LogInformation("Adding {MessageCount} messages to Conversation {ConversationId}",
            messageList.Count, conversationId);

        await _db.Set<Message>().AddRangeAsync(messageList, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
