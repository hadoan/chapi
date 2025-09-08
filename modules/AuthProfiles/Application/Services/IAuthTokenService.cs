using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuthProfiles.Application.Dtos;
using AuthProfiles.Domain;

namespace AuthProfiles.Application.Services
{
    public interface IAuthTokenService
    {
        Task<TestAuthResult> AcquireAsync(AuthProfile profile, IReadOnlyDictionary<string, string> resolvedSecrets, CancellationToken ct);
    }
}
