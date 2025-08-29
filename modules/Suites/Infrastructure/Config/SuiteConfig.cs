using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Suites.Domain;

namespace Suites.Infrastructure.Config;

public class SuiteConfig : IEntityTypeConfiguration<Suite>
{
    public void Configure(EntityTypeBuilder<Suite> builder)
    {
        builder.ToTable("Suites");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Description).HasMaxLength(2000);
        builder.HasMany<SuiteFile>("_files").WithOne().HasForeignKey(f => f.SuiteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany<TestCase>("_testCases").WithOne().HasForeignKey(tc => tc.SuiteId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SuiteFileConfig : IEntityTypeConfiguration<SuiteFile>
{
    public void Configure(EntityTypeBuilder<SuiteFile> builder)
    {
        builder.ToTable("SuiteFiles");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Path).IsRequired().HasMaxLength(400);
        builder.Property(f => f.Kind).IsRequired().HasMaxLength(100);
    }
}

public class TestCaseConfig : IEntityTypeConfiguration<TestCase>
{
    public void Configure(EntityTypeBuilder<TestCase> builder)
    {
        builder.ToTable("TestCases");
        builder.HasKey(tc => tc.Id);
        builder.Property(tc => tc.Name).IsRequired().HasMaxLength(200);
        builder.Property(tc => tc.Command).IsRequired().HasMaxLength(1000);
    }
}
