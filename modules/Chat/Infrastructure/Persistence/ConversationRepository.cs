using Chat.Domain;
using Microsoft.EntityFrameworkCore;
using ShipMvp.Core.Persistence;

namespace Chat.Infrastructure.Persistence;

public class ConversationRepository : IConversationRepository
{
    private readonly IDbContext _db; private readonly DbSet<Conversation> _set;
    public ConversationRepository(IDbContext db) { _db = db; _set = db.Set<Conversation>(); }
    public IQueryable<Conversation> Query() => _set.Include(c => c.Messages).AsQueryable();
    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct) => await _set.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == id, ct);
    public async Task<IEnumerable<Conversation>> GetAllAsync(CancellationToken ct) => await _set.Include(c => c.Messages).ToListAsync(ct);
    public async Task<Conversation> AddAsync(Conversation entity, CancellationToken ct)
    {
        await _set.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return entity;
    }
    public async Task<Conversation> UpdateAsync(Conversation entity, CancellationToken ct)
    {
        const int maxRetries = 3;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                _set.Update(entity);
                await _db.SaveChangesAsync(ct);
                return entity;
            }
            catch (DbUpdateConcurrencyException) when (retryCount < maxRetries - 1)
            {
                retryCount++;

                // Reload the entity from the database to get the latest version
                var freshEntity = await _set.Include(c => c.Messages).AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == entity.Id, ct);

                if (freshEntity == null)
                    throw new InvalidOperationException("Entity no longer exists in database");

                // Update the timestamp to reflect the new version
                entity.Touch();

                // Clear change tracking and try again
                _db.ChangeTracker.Clear();

                // Add a small delay before retry
                await Task.Delay(TimeSpan.FromMilliseconds(50 * retryCount), ct);
            }
        }

        throw new InvalidOperationException($"Failed to update conversation after {maxRetries} attempts");
    }
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _set.FindAsync(new object?[] { id }, ct).AsTask();
        if (entity is null) return;
        _set.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
