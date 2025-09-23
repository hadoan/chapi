using System.Threading.Tasks;
using AuthProfiles.Application.Dtos;
using Chapi.AI.Dto;
using Chapi.EndpointCatalog.Application;

namespace Chapi.AI.Services
{
    public interface ITestGenService
    {
        Task<TestGenResponse> GenerateTestsAsync(TestGenInput input, bool generateJson = true);
        Task<TestGenResponse> GenerateEnpointTestsAsync(EndpointDto input, AuthProfileDto authProfile);
    }
}
