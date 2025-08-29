using Microsoft.EntityFrameworkCore;

namespace Environments.Infrastructure;

public static class ModelBuilderExtensions
{
    public static void ConfigureEnvironmentEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EnvironmentConfig());
        modelBuilder.ApplyConfiguration(new EnvironmentHeaderConfig());
        modelBuilder.ApplyConfiguration(new SecretVaultRefConfig());
    }
}
