using ShipMvp.Core.Application;

namespace Chapi.ApiSpecs.Application;

public interface IApiSpecAppService : IApplicationService
{
    Task<ApiSpecDto> ImportAsync(ImportOpenApiInputDto input);
    Task<ApiSpecDto?> GetAsync(Guid specId);
}
