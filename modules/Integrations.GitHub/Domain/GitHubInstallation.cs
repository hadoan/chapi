using ShipMvp.Core.Entities;

namespace Integrations.GitHub.Domain;

public enum PrCheckStatus { Pending, InProgress, Success, Failure }

/// <summary>
/// Aggregate representing a GitHub App installation within an org containing multiple repos.
/// </summary>
public class GitHubInstallation : Entity<Guid>
{
    private readonly List<GitHubRepo> _repos = new();
    public long InstallationId { get; private set; }
    public Guid OrgId { get; private set; }
    public IReadOnlyCollection<GitHubRepo> Repos => _repos;
    public DateTime ConnectedAt { get; private set; }
    public bool ChecksEnabled { get; private set; }

    private GitHubInstallation() : base(Guid.Empty) { }
    private GitHubInstallation(Guid id, long installationId, Guid orgId) : base(id)
    {
        InstallationId = installationId;
        OrgId = orgId;
        ConnectedAt = DateTime.UtcNow;
    }

    public static GitHubInstallation Connect(long installationId, Guid orgId) => new(Guid.NewGuid(), installationId, orgId);
    public GitHubInstallation EnableChecks() { ChecksEnabled = true; return this; }
    public GitHubInstallation DisableChecks() { ChecksEnabled = false; return this; }
    public void AddRepo(long repoId, string name)
    {
        if (_repos.Any(r => r.RepoId == repoId)) return;
        _repos.Add(GitHubRepo.Create(Id, repoId, name));
    }
    public void RemoveRepo(long repoId) => _repos.RemoveAll(r => r.RepoId == repoId);
}

/// <summary>
/// Repository for an installation (repo) under a GitHub installation aggregate.
/// </summary>
public class GitHubRepo : Entity<Guid>
{
    public Guid InstallationId { get; private set; }
    public long RepoId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    private GitHubRepo() : base(Guid.Empty) { }
    private GitHubRepo(Guid id, Guid installationId, long repoId, string name) : base(id)
    { InstallationId = installationId; RepoId = repoId; Name = name; }
    public static GitHubRepo Create(Guid installationId, long repoId, string name) => new(Guid.NewGuid(), installationId, repoId, name);
}

/// <summary>
/// PR Check entity associated with a run.
/// </summary>
public class PrCheck : Entity<Guid>
{
    public Guid RunId { get; private set; }
    public PrCheckStatus Status { get; private set; } = PrCheckStatus.Pending;
    public string Url { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    private PrCheck() : base(Guid.Empty) { }
    private PrCheck(Guid id, Guid runId, string url) : base(id) { RunId = runId; Url = url; CreatedAt = DateTime.UtcNow; }
    public static PrCheck Create(Guid runId, string url) => new(Guid.NewGuid(), runId, url);
    public void MarkInProgress() { if (Status == PrCheckStatus.Pending) Status = PrCheckStatus.InProgress; }
    public void MarkResult(bool success) { if (Status is PrCheckStatus.Success or PrCheckStatus.Failure) return; Status = success ? PrCheckStatus.Success : PrCheckStatus.Failure; CompletedAt = DateTime.UtcNow; }
}

public interface IGitHubInstallationRepository : ShipMvp.Core.Abstractions.IRepository<GitHubInstallation, Guid>
{
    IQueryable<GitHubInstallation> Query();
    Task<GitHubInstallation?> GetByInstallationIdAsync(long installationId, CancellationToken ct);
}

public interface IPrCheckRepository : ShipMvp.Core.Abstractions.IRepository<PrCheck, Guid>
{
    IQueryable<PrCheck> Query();
    Task<PrCheck?> GetByRunAsync(Guid runId, CancellationToken ct);
}

/// <summary>
/// Outgoing port for GitHub API operations.
/// </summary>
public interface IGitHubApiClient
{
    Task CreateCheckRunAsync(long installationId, long repoId, string name, string status, string detailsUrl, CancellationToken ct);
    Task PostPrCommentAsync(long installationId, long repoId, int prNumber, string body, CancellationToken ct);
}

/// <summary>
/// Service for verifying webhook signatures.
/// </summary>
public interface IGitHubWebhookVerifier { bool Verify(string payload, string signature); }

public class HmacGitHubWebhookVerifier : IGitHubWebhookVerifier
{
    private readonly string _secret; // TODO: inject secure secret provider
    public HmacGitHubWebhookVerifier() { _secret = "CHANGE_ME"; }
    public bool Verify(string payload, string signature)
    {
        // TODO: real HMAC SHA256 verification
        return true;
    }
}
