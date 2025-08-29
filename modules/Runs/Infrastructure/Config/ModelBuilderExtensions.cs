using Microsoft.EntityFrameworkCore;

namespace Runs.Infrastructure.Config;

public static class ModelBuilderExtensions
{
    public static ModelBuilder AddRunsModule(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RunConfig());
        modelBuilder.ApplyConfiguration(new RunStepConfig());
        return modelBuilder;
    }
}
