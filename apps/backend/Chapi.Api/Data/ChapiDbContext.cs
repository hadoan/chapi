using Microsoft.EntityFrameworkCore;

using ShipMvp.Application.Infrastructure.Data;

using Projects.Infrastructure;
using Environments.Infrastructure;
using Chat.Infrastructure;
using Chapi.ApiSpecs.Infrastructure;
using Chapi.EndpointCatalog.Infrastructure;
using Contacts.Infrastructure;

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
        modelBuilder.ConfigureProjectEntities();
        modelBuilder.ConfigureEnvironmentEntities();
        modelBuilder.ConfigureChatEntities();
        modelBuilder.ConfigureApiSpecsEntities();
        modelBuilder.ConfigureEndpointCatalogEntities();
        modelBuilder.ConfigureContactsEntities();
    }

}
