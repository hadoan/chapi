using System;
using System.Threading;
using System.Threading.Tasks;
using AuthProfiles.Application.Dtos;
using AuthProfiles.Application.Requests;

namespace AuthProfiles.Application.Services
{
    public interface IAuthProfileService
    {
        Task<AuthProfileDto> CreateAsync(CreateAuthProfileRequest r, CancellationToken ct);
        Task<AuthProfileDto> UpdateAsync(Guid id, UpdateAuthProfileRequest r, CancellationToken ct);
        Task DeleteAsync(Guid id, CancellationToken ct);
        Task EnableAsync(Guid id, CancellationToken ct);
        Task DisableAsync(Guid id, CancellationToken ct);
    }
}
