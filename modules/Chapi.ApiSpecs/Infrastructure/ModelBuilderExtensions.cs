using Microsoft.EntityFrameworkCore;
using Chapi.ApiSpecs.Domain;

namespace Chapi.ApiSpecs.Infrastructure;

public static class ModelBuilderExtensions
{
    public static void ConfigureApiSpecsEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ApiSpecConfig());
    }
}
