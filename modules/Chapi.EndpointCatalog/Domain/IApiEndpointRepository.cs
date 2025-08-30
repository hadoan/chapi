using ShipMvp.Core.Abstractions;
using Chapi.EndpointCatalog.Application;

namespace Chapi.EndpointCatalog.Domain;

public interface IApiEndpointRepository : IRepository<ApiEndpoint, Guid>
{
    Task UpsertAsync(Guid specId, Guid projectId, EndpointDto dto);
    Task<List<ApiEndpoint>> ListByProjectAsync(Guid projectId, string? tag = null, string? search = null);
    Task<ApiEndpoint?> FindByMethodPathAsync(Guid projectId, string method, string path);
    Task<ApiEndpoint?> FindAsync(Guid id);
}
