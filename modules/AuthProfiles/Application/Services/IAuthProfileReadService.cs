using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuthProfiles.Application.Dtos;
using AuthProfiles.Application.Requests;

namespace AuthProfiles.Application.Services
{
    public interface IAuthProfileReadService
    {
        Task<AuthProfileDto?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<(IEnumerable<AuthProfileDto> Items, int Total)> ListAsync(GetAuthProfilesQuery q, CancellationToken ct);
        Task<AuthProfileDto?> GetFirstForProjectEnvironmentAsync(Guid projectId, string environmentKey, CancellationToken ct);
    }
}
