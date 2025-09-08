using System.Threading;
using System.Threading.Tasks;
using AuthProfiles.Application.Services;

namespace AuthProfiles.Infrastructure.Services
{
    /// <summary>
    /// Fallback secret store used when no environment-level secret store is registered.
    /// Returns null for any lookup. This avoids startup DI failures and allows callers
    /// to handle missing secrets explicitly.
    /// </summary>
    public class NullSecretStore : ISecretStore
    {
        public Task<string?> GetAsync(string secretRef, CancellationToken ct)
        {
            return Task.FromResult<string?>(null);
        }
    }
}
