namespace Environments.Infrastructure.SecretStores;

// TODO: Implement Azure Key Vault interaction
public class AzureKVSecretStore : Domain.ISecretStore
{
    public Task SetAsync(string provider, string keyPath, string value, CancellationToken ct = default) => Task.CompletedTask;
    public Task<string?> GetAsync(string provider, string keyPath, CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task RotateAsync(string provider, string keyPath, CancellationToken ct = default) => Task.CompletedTask;
}
