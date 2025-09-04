using Microsoft.EntityFrameworkCore;

namespace RunPack.Infrastructure;

public static class ModelBuilderExtensions
{
    public static void ConfigureRunPacksEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Persistence.RunPackConfig());
        modelBuilder.ApplyConfiguration(new Persistence.RunPackFileConfig());
        modelBuilder.ApplyConfiguration(new Persistence.RunPackInputConfig());
    }
}
