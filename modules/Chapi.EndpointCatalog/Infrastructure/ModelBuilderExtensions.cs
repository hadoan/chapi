using Microsoft.EntityFrameworkCore;
using Chapi.EndpointCatalog.Domain;

namespace Chapi.EndpointCatalog.Infrastructure;

public static class ModelBuilderExtensions
{
    public static void ConfigureEndpointCatalogEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ApiEndpointConfig());
    }
}
