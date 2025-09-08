using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuthProfiles.Application.Dtos;
using AuthProfiles.Application.Services;

namespace AuthProfiles.Infrastructure.Services
{
    public class AuthDetectionService : IAuthDetectionService
    {
        public Task<IReadOnlyList<AuthDetectionCandidateDto>> DetectAsync(Guid projectId, Guid serviceId, CancellationToken ct)
        {
            // Lightweight heuristic: return empty list with TODOs for real parsing
            var list = new List<AuthDetectionCandidateDto>
            {
                new AuthDetectionCandidateDto(AuthProfiles.Domain.AuthType.OAuth2ClientCredentials, null, null, null, AuthProfiles.Domain.InjectionMode.Header, "Authorization", "Bearer {{access_token}}", "heuristic", 0.1)
            };

            return Task.FromResult((IReadOnlyList<AuthDetectionCandidateDto>)list);
        }
    }
}
