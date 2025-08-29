using System.Security.Cryptography;
using System.Text;

namespace Environments.Infrastructure.SecretStores;

// Simple in-DB encrypted secret store (placeholder). In real scenario, encryption keys should be external.
public class InDbEncryptedSecretStore : Domain.ISecretStore
{
    private static readonly Dictionary<string,string> _store = new();

    public Task SetAsync(string provider, string keyPath, string value, CancellationToken ct = default)
    {
        var cipher = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser));
        _store[Key(provider,keyPath)] = cipher;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string provider, string keyPath, CancellationToken ct = default)
    {
        if (_store.TryGetValue(Key(provider,keyPath), out var cipher))
        {
            try
            {
                var bytes = ProtectedData.Unprotect(Convert.FromBase64String(cipher), null, DataProtectionScope.CurrentUser);
                return Task.FromResult<string?>(Encoding.UTF8.GetString(bytes));
            }
            catch { return Task.FromResult<string?>(null); }
        }
        return Task.FromResult<string?>(null);
    }

    public async Task RotateAsync(string provider, string keyPath, CancellationToken ct = default)
    {
        // For demo rotation = read + re-encrypt
        var current = await GetAsync(provider, keyPath, ct);
        if (current != null)
            await SetAsync(provider, keyPath, current, ct);
    }

    private static string Key(string provider,string keyPath) => provider+":"+keyPath;
}
