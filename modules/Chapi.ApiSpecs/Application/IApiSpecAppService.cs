namespace Chapi.ApiSpecs.Application;

public interface IApiSpecAppService
{
    Task<ApiSpecDto> ImportAsync(ImportOpenApiInputDto input);
    Task<ApiSpecDto?> GetAsync(Guid specId);
}
