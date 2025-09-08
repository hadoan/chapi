using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuthProfiles.Application.Dtos;

namespace AuthProfiles.Application.Services
{
    public interface IAuthDetectionService
    {
        Task<IReadOnlyList<AuthDetectionCandidateDto>> DetectAsync(Guid projectId, Guid serviceId, CancellationToken ct);
    }
}
