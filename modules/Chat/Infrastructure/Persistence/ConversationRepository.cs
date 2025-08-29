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
        _set.Update(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _set.FindAsync(new object?[] { id }, ct).AsTask();
        if (entity is null) return;
        _set.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
