using ShipMvp.Core.Abstractions;

namespace Chapi.ApiSpecs.Domain;

public interface IApiSpecRepository : IRepository<ApiSpec, Guid>
{
    Task<ApiSpec?> FindByShaAsync(string sha);
    Task<Guid> UpsertAsync(ApiSpec spec);
    // Return raw JSON strings of specs for a given project id.
    Task<IEnumerable<string>> GetRawJsonByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}
