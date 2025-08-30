using ShipMvp.Core.Abstractions;

namespace Chapi.ApiSpecs.Domain;

public interface IApiSpecRepository : IRepository<ApiSpec, Guid>
{
    Task<ApiSpec?> FindByShaAsync(string sha);
    Task<Guid> UpsertAsync(ApiSpec spec);
}
