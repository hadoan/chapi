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
    public async Task AddAsync(Conversation entity, CancellationToken ct) { await _set.AddAsync(entity, ct); await _db.SaveChangesAsync(ct); }
    public Task UpdateAsync(Conversation entity, CancellationToken ct) { _set.Update(entity); return _db.SaveChangesAsync(ct); }
    public Task DeleteAsync(Conversation entity, CancellationToken ct) { _set.Remove(entity); return _db.SaveChangesAsync(ct); }
}
