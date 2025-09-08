using System.Threading;
using System.Threading.Tasks;

namespace AuthProfiles.Application.Services
{
    public interface ISecretStore
    {
        Task<string?> GetAsync(string secretRef, CancellationToken ct);
    }
}
