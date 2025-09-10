using System.Threading.Tasks;
using Chapi.AI.Dto;

namespace Chapi.AI.Services
{
    public interface ITestGenService
    {
        Task<TestGenResponse> GenerateTestsAsync(TestGenInput input);
    }
}
