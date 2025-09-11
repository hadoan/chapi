using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Chapi.AI.Dto;

namespace Chapi.AI.Services
{
    public interface IAuthAiDetectionService
    {
        Task<DetectionResponseDto> DetectByCodeAsync(string code, Guid? projectId, CancellationToken ct);
        Task<DetectionResponseDto> DetectByPromptAsync(string prompt, Guid? projectId, CancellationToken ct);
    }
}
