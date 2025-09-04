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

        // Map the Steps navigation via the public property and instruct EF to use the
        // backing field '_steps' for storage to avoid conflicts between field/property.
        builder.HasMany(r => r.Steps)
               .WithOne()
               .HasForeignKey(s => s.RunId)
               .OnDelete(DeleteBehavior.Cascade);

        // Use field-backed access for the Steps navigation so EF won't attempt to
        // create a separate mapping for the field and the property.
        builder.Navigation(r => r.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
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
