using Integrations.GitHub.Application.Dtos;
using Integrations.GitHub.Application.Mappings;
using Integrations.GitHub.Application.Requests;
using Integrations.GitHub.Domain;
using Microsoft.EntityFrameworkCore;

namespace Integrations.GitHub.Application.Services;

public class GitHubIntegrationAppService : IGitHubIntegrationAppService
{
    private readonly IGitHubInstallationRepository _installations;
    public GitHubIntegrationAppService(IGitHubInstallationRepository installations) => _installations = installations;

    public async Task<GitHubInstallationDto> ConnectAsync(ConnectInstallationRequest request, CancellationToken ct)
    {
        var existing = await _installations.GetByInstallationIdAsync(request.InstallationId, ct);
        if (existing != null) return existing.ToDto();
        var inst = GitHubInstallation.Connect(request.InstallationId, request.OrgId);
        await _installations.AddAsync(inst, ct);
        return inst.ToDto();
    }

    public async Task<GitHubInstallationDto?> GetAsync(long installationId, CancellationToken ct)
        => (await _installations.GetByInstallationIdAsync(installationId, ct))?.ToDto();

    public async Task<GitHubInstallationDto?> EnableChecksAsync(EnableChecksRequest request, CancellationToken ct)
    {
        var inst = await _installations.GetByInstallationIdAsync(request.InstallationId, ct);
        if (inst == null) return null;
        if (request.Enable) inst.EnableChecks(); else inst.DisableChecks();
        await _installations.UpdateAsync(inst, ct);
        return inst.ToDto();
    }

    public async Task<GitHubInstallationDto?> AddRepoAsync(AddRepoRequest request, CancellationToken ct)
    {
        var inst = await _installations.GetByInstallationIdAsync(request.InstallationId, ct);
        if (inst == null) return null;
        inst.AddRepo(request.RepoId, request.Name);
        await _installations.UpdateAsync(inst, ct);
        return inst.ToDto();
    }

    public async Task<GitHubInstallationDto?> RemoveRepoAsync(RemoveRepoRequest request, CancellationToken ct)
    {
        var inst = await _installations.GetByInstallationIdAsync(request.InstallationId, ct);
        if (inst == null) return null;
        inst.RemoveRepo(request.RepoId);
        await _installations.UpdateAsync(inst, ct);
        return inst.ToDto();
    }
}
