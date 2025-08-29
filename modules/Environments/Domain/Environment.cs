using ShipMvp.Core.Entities;

namespace Environments.Domain;

/// <summary>
/// Deployment/runtime environment aggregate root (e.g. local, staging, prod)
/// </summary>
public class Environment : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty; // local/staging/prod
    public string BaseUrl { get; private set; } = string.Empty;
    public int TimeoutMs { get; private set; }
    public bool FollowRedirects { get; private set; }
    public List<EnvironmentHeader> Headers { get; private set; } = new();
    public List<SecretVaultRef> Secrets { get; private set; } = new();

    private Environment() : base(Guid.Empty) { }

    private Environment(Guid id, string name, string baseUrl, int timeoutMs, bool followRedirects) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));
        if (!Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute)) throw new ArgumentException("BaseUrl must be absolute", nameof(baseUrl));
        if (timeoutMs <= 0) throw new ArgumentException("Timeout must be positive", nameof(timeoutMs));
        Name = name.Trim();
        BaseUrl = baseUrl.Trim();
        TimeoutMs = timeoutMs;
        FollowRedirects = followRedirects;
    }

    public static Environment Create(string name, string baseUrl, int timeoutMs = 30000, bool followRedirects = true)
        => new(Guid.NewGuid(), name, baseUrl, timeoutMs, followRedirects);

    public Environment Update(string baseUrl, int timeoutMs, bool followRedirects)
    {
        if (!Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute)) throw new ArgumentException("BaseUrl must be absolute", nameof(baseUrl));
        if (timeoutMs <= 0) throw new ArgumentException("Timeout must be positive", nameof(timeoutMs));
        BaseUrl = baseUrl.Trim();
        TimeoutMs = timeoutMs;
        FollowRedirects = followRedirects;
        return this;
    }

    public Environment SetHeaders(IEnumerable<(string Key,string Value)> headers)
    {
        Headers.Clear();
        foreach (var (k,v) in headers)
        {
            if (string.IsNullOrWhiteSpace(k)) continue;
            Headers.Add(new EnvironmentHeader(Guid.NewGuid(), k.Trim(), v));
        }
        return this;
    }

    public Environment SyncSecrets(IEnumerable<SecretVaultRef> refs)
    {
        Secrets = refs.ToList();
        return this;
    }
}

/// <summary>
/// Header key/value pair
/// </summary>
public class EnvironmentHeader : Entity<Guid>
{
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public Guid EnvironmentId { get; private set; }

    private EnvironmentHeader() : base(Guid.Empty) { }
    public EnvironmentHeader(Guid id, string key, string value) : base(id)
    {
        Key = key;
        Value = value;
    }
}

/// <summary>
/// Secret reference value object kept in-environment aggregate.
/// </summary>
public class SecretVaultRef : Entity<Guid> // Using Entity for persistence convenience
{
    public string Provider { get; private set; } = string.Empty; // InDbEncrypted, AwsSealed, GcpSM, AzureKV
    public string KeyPath { get; private set; } = string.Empty;
    public DateTime? LastRotatedAt { get; private set; }
    public Guid EnvironmentId { get; private set; }

    private SecretVaultRef() : base(Guid.Empty) { }
    public SecretVaultRef(Guid id, string provider, string keyPath, DateTime? lastRotatedAt = null) : base(id)
    {
        Provider = provider.Trim();
        KeyPath = keyPath.Trim();
        LastRotatedAt = lastRotatedAt;
    }

    public SecretVaultRef Rotated()
    {
        LastRotatedAt = DateTime.UtcNow;
        return this;
    }
}
