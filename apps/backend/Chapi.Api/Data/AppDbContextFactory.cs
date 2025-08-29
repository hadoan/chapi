using ShipMvp.Application.Infrastructure.Data;

namespace Chapi.Api.Data;

public sealed class AppDbContextFactory : BaseAppDbContextFactory<InvoiceDbContext>
{
    public override InvoiceDbContext CreateDbContext(string[] args)
    {
        var options = GetOptions();
        return new InvoiceDbContext(options);
    }
}
