using Microsoft.EntityFrameworkCore;

using ShipMvp.Application.Infrastructure.Data;

namespace Chapi.Api.Data;

public sealed class ChapiDbContext : AppDbContext
{
    public ChapiDbContext(DbContextOptions<ChapiDbContext> options) : base(options)
    {
    }

    public override void ConfigureModules(ModelBuilder modelBuilder)
    {
        base.ConfigureModules(modelBuilder);
        // modelBuilder.ConfigureChapiEntities();
    }
    
}
