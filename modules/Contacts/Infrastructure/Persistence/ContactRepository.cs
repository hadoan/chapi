using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contacts.Domain;
using Microsoft.EntityFrameworkCore;
using ShipMvp.Application.Infrastructure.Data;

namespace Contacts.Infrastructure.Persistence
{
    /// <summary>
    /// EF Core implementation of IContactRepository. Assumes an AppDbContext exists in the application.
    /// TODO: if your solution uses a differently named DbContext, update the constructor and DI registration.
    /// </summary>
    public class ContactRepository : Application.Services.IContactRepository
    {
        private readonly AppDbContext _db;

        public ContactRepository(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public IQueryable<Contact> Query() => _db.Set<Contact>().AsQueryable();

        public Task<Contact?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _db.Set<Contact>().FirstOrDefaultAsync(c => c.Id == id, ct);

        public async Task AddAsync(Contact entity, CancellationToken ct = default)
        {
            await _db.Set<Contact>().AddAsync(entity, ct).ConfigureAwait(false);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        public async Task UpdateAsync(Contact entity, CancellationToken ct = default)
        {
            _db.Set<Contact>().Update(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        public async Task DeleteAsync(Contact entity, CancellationToken ct = default)
        {
            _db.Set<Contact>().Remove(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
