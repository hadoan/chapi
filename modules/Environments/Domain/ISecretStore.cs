namespace Environments.Domain;

/// <summary>
/// Abstraction for secret storage/lookup.
/// </summary>
public interface ISecretStore
{
    Task SetAsync(string provider, string keyPath, string value, CancellationToken ct = default);
    Task<string?> GetAsync(string provider, string keyPath, CancellationToken ct = default);
    Task RotateAsync(string provider, string keyPath, CancellationToken ct = default); // Implementation may re-encrypt or refresh version
}
