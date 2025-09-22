using System.Threading;
using System.Threading.Tasks;
using Chapi.AI.Dto;

namespace Chapi.AI.Services.Auth;

public interface ITokenGenerator
{
    /// <summary>Generate or return a cached access token string (without "Bearer "), or null if not applicable.</summary>
    Task<string?> GetAccessTokenAsync(AuthProfile profile, CancellationToken ct = default);
}
