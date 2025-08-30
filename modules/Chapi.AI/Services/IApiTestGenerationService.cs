using System.Threading.Tasks;
using Chapi.AI.Dto;

namespace Chapi.AI.Services
{
    public interface IApiTestGenerationService
    {
        Task<ChapiCard> GenerateTestAsync(string openApiJson);
        Task<ChapiCard> GenerateTestAsync(string? userQuery, string? endpointsContext, int? maxFiles, string? openApiJson);
    }
}
