using ShipMvp.Application.Infrastructure.Data;

namespace Chapi.Api.Data;

public sealed class AppDbContextFactory : BaseAppDbContextFactory<ChapiDbContext>
{
    public override ChapiDbContext CreateDbContext(string[] args)
    {
        var options = GetOptions();
        return new ChapiDbContext(options);
    }
}
