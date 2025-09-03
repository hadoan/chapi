using System;
using System.Threading;
using System.Threading.Tasks;
using Contacts.Application.Dtos;
using Contacts.Application.Requests;

namespace Contacts.Application.Services
{
    public interface IContactService
    {
        Task<ContactDto> CreateAsync(CreateContactRequest request, CancellationToken ct = default);
        Task<ContactDto?> UpdateAsync(UpdateContactRequest request, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<bool> SetStatusAsync(Guid id, string status, CancellationToken ct = default);
    }
}
