using Integrations.GitHub.Application.Dtos;
using Integrations.GitHub.Application.Requests;

namespace Integrations.GitHub.Application.Services;

public interface IGitHubIntegrationAppService
{
    Task<GitHubInstallationDto> ConnectAsync(ConnectInstallationRequest request, CancellationToken ct);
    Task<GitHubInstallationDto?> GetAsync(long installationId, CancellationToken ct);
    Task<GitHubInstallationDto?> EnableChecksAsync(EnableChecksRequest request, CancellationToken ct);
    Task<GitHubInstallationDto?> AddRepoAsync(AddRepoRequest request, CancellationToken ct);
    Task<GitHubInstallationDto?> RemoveRepoAsync(RemoveRepoRequest request, CancellationToken ct);
}
