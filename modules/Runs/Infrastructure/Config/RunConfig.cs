using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Runs.Domain;

namespace Runs.Infrastructure.Config;

public class RunConfig : IEntityTypeConfiguration<Run>
{
    public void Configure(EntityTypeBuilder<Run> builder)
    {
        builder.ToTable("Runs");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Status).IsRequired();
        builder.HasMany<RunStep>("_steps").WithOne().HasForeignKey(s => s.RunId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RunStepConfig : IEntityTypeConfiguration<RunStep>
{
    public void Configure(EntityTypeBuilder<RunStep> builder)
    {
        builder.ToTable("RunSteps");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
    }
}
