using Microsoft.EntityFrameworkCore;

namespace Suites.Infrastructure.Config;

public static class ModelBuilderExtensions
{
    public static ModelBuilder AddSuitesModule(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SuiteConfig());
        modelBuilder.ApplyConfiguration(new SuiteFileConfig());
        modelBuilder.ApplyConfiguration(new TestCaseConfig());
        return modelBuilder;
    }
}
