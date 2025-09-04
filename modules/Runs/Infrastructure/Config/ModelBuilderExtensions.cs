using Microsoft.EntityFrameworkCore;
using Runs.Infrastructure.Config;

namespace Runs.Infrastructure;

public static class ModelBuilderExtensions
{
   
    // New extension method expected by ChapiDbContext
    public static void ConfigureRunsEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RunConfig());
        modelBuilder.ApplyConfiguration(new RunStepConfig());
    }
}
