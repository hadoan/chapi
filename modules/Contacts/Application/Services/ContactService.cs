using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contacts.Application.Dtos;
using Contacts.Application.Mappings;
using Contacts.Application.Requests;
using Contacts.Domain;

namespace Contacts.Application.Services
{
    /// <summary>
    /// Application service implementing contact workflows. Depends on repository abstractions.
    /// </summary>
    public class ContactService : IContactService, IContactReadService
    {
        private readonly IContactRepository _repository;

        public ContactService(IContactRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<ContactDto> CreateAsync(CreateContactRequest request, CancellationToken ct = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var contact = Contact.Create(request.Name, request.Email, request.Company);

            await _repository.AddAsync(contact, ct).ConfigureAwait(false);

            return contact.ToDto();
        }

        public async Task<ContactDto?> UpdateAsync(UpdateContactRequest request, CancellationToken ct = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var existing = await _repository.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
            if (existing is null) return null;

            existing.Update(request.Name, request.Email, request.Company);

            await _repository.UpdateAsync(existing, ct).ConfigureAwait(false);

            return existing.ToDto();
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var existing = await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (existing is null) return false;

            await _repository.DeleteAsync(existing, ct).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> SetStatusAsync(Guid id, string status, CancellationToken ct = default)
        {
            var existing = await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (existing is null) return false;

            if (!Enum.TryParse<ContactStatus>(status, true, out var parsed))
                throw new ArgumentException("Invalid status", nameof(status));

            existing.SetStatus(parsed);
            await _repository.UpdateAsync(existing, ct).ConfigureAwait(false);
            return true;
        }

        public async Task<ContactDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var existing = await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);
            return existing?.ToDto();
        }

        public async Task<IReadOnlyList<ContactDto>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var query = _repository.Query();

            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // If repository returns IQueryable of domain entities, mapping is synchronous here.
            return items.Select(x => x.ToDto()).ToList();
        }
    }

    // Repository contract expected by the service - declared here to avoid infra dependency leaks.
    // Infrastructure will provide concrete implementation.
    public interface IContactRepository
    {
        IQueryable<Contact> Query();
        Task<Contact?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task AddAsync(Contact entity, CancellationToken ct = default);
        Task UpdateAsync(Contact entity, CancellationToken ct = default);
        Task DeleteAsync(Contact entity, CancellationToken ct = default);
    }
}
