namespace Environments.Infrastructure.SecretStores;

// TODO: Implement AWS Secrets Manager interaction
public class AwsSealedSecretStore : Domain.ISecretStore
{
    public Task SetAsync(string provider, string keyPath, string value, CancellationToken ct = default) => Task.CompletedTask;
    public Task<string?> GetAsync(string provider, string keyPath, CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task RotateAsync(string provider, string keyPath, CancellationToken ct = default) => Task.CompletedTask;
}
