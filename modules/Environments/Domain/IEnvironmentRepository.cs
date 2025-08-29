using ShipMvp.Core.Abstractions;

namespace Environments.Domain;

public interface IEnvironmentRepository : IRepository<Environment, Guid>
{
    Task<Environment?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Environment> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search = null, CancellationToken cancellationToken = default);
}
