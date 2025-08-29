namespace Environments.Infrastructure.SecretStores;

// TODO: Implement GCP Secret Manager interaction
public class GcpSMSecretStore : Domain.ISecretStore
{
    public Task SetAsync(string provider, string keyPath, string value, CancellationToken ct = default) => Task.CompletedTask;
    public Task<string?> GetAsync(string provider, string keyPath, CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task RotateAsync(string provider, string keyPath, CancellationToken ct = default) => Task.CompletedTask;
}
