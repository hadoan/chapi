using Microsoft.EntityFrameworkCore;
using Projects.Domain;

namespace Projects.Infrastructure;

public static class ModelBuilderExtensions
{
    public static void ConfigureProjectEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProjectConfig());
        modelBuilder.ApplyConfiguration(new ProjectTaskConfig());
    }
}
