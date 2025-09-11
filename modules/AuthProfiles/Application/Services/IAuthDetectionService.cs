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

        // New API: accept richer detection request (OpenAPI/Postman/raw JSON)
        Task<IReadOnlyList<AuthProfiles.Application.Dtos.DetectionCandidateDto>> DetectAsync(AuthProfiles.Application.Dtos.DetectTokenRequest req, CancellationToken ct);

        // AI/Freeform detection: accept raw code or a prompt and return detection candidates
        Task<IReadOnlyList<AuthProfiles.Application.Dtos.DetectionCandidateDto>> DetectByCodeAsync(string code, Guid? projectId, CancellationToken ct);

        Task<IReadOnlyList<AuthProfiles.Application.Dtos.DetectionCandidateDto>> DetectByPromptAsync(string prompt, Guid? projectId, CancellationToken ct);
    }
}
