namespace Chapi.EndpointCatalog.Application;

public interface IEndpointAppService
{
    Task<List<EndpointBriefDto>> ListAsync(Guid projectId, string? tag = null, string? search = null);
    Task<EndpointDto> GetAsync(Guid endpointId);
}
