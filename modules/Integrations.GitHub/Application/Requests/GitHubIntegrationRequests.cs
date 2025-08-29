namespace Integrations.GitHub.Application.Requests;

public record ConnectInstallationRequest(long InstallationId, Guid OrgId);
public record EnableChecksRequest(long InstallationId, bool Enable);
public record AddRepoRequest(long InstallationId, long RepoId, string Name);
public record RemoveRepoRequest(long InstallationId, long RepoId);
