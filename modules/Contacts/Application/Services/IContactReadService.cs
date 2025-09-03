using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Contacts.Application.Dtos;

namespace Contacts.Application.Services
{
    public interface IContactReadService
    {
        Task<ContactDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<ContactDto>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    }
}
