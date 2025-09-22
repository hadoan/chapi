using System.Threading.Tasks;
using Chapi.AI.Dto;
using Chapi.IR;

namespace Chapi.AI.Services
{
    public interface IApiTestGenerationService
    {
        Task<ChapiCard> GenerateTestAsync(string openApiJson);
        Task<ChapiCard> GenerateTestAsync(string? userQuery, string? endpointsContext, int? maxFiles, string? openApiJson);
        Task<string> GenerateEndpointAsync(string authProfileJson, string endpointJson);
    }
}
