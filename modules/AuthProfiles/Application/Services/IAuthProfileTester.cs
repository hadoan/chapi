using System.Threading;
using System.Threading.Tasks;
using AuthProfiles.Application.Dtos;

namespace AuthProfiles.Application.Services
{
    public interface IAuthProfileTester
    {
        Task<TestAuthResult> TestAsync(TestAuthRequest request, CancellationToken ct);
    }
}
