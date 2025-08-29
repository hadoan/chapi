using Integrations.GitHub.Application.Dtos;
using Integrations.GitHub.Domain;

namespace Integrations.GitHub.Application.Mappings;

public static class GitHubMappings
{
    public static GitHubInstallationDto ToDto(this GitHubInstallation i) => new(i.Id, i.InstallationId, i.OrgId, i.ChecksEnabled, i.Repos.Select(r => new GitHubRepoDto(r.RepoId, r.Name)), i.ConnectedAt);
    public static PrCheckDto ToDto(this PrCheck c) => new(c.Id, c.RunId, c.Status.ToString(), c.Url, c.CreatedAt, c.CompletedAt);
}
