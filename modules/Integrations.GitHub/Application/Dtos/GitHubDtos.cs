namespace Integrations.GitHub.Application.Dtos;

public record GitHubInstallationDto(Guid Id, long InstallationId, Guid OrgId, bool ChecksEnabled, IEnumerable<GitHubRepoDto> Repos, DateTime ConnectedAt);
public record GitHubRepoDto(long RepoId, string Name);
public record PrCheckDto(Guid Id, Guid RunId, string Status, string Url, DateTime CreatedAt, DateTime? CompletedAt);
