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

        // Required properties with constraints
        builder.Property(r => r.SuiteName).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Version).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.Actor).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Trigger).IsRequired().HasMaxLength(50);

        // Optional properties
        builder.Property(r => r.IrPath).HasMaxLength(500);
        builder.Property(r => r.Error).HasMaxLength(2000);

        // Map the Steps navigation via the public property and instruct EF to use the
        // backing field '_steps' for storage to avoid conflicts between field/property.
        builder.HasMany(r => r.Steps)
               .WithOne()
               .HasForeignKey(s => s.RunId)
               .OnDelete(DeleteBehavior.Cascade);

        // Use field-backed access for the Steps navigation so EF won't attempt to
        // create a separate mapping for the field and the property.
        builder.Navigation(r => r.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Add indexes for efficient querying
        builder.HasIndex(r => r.ProjectId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.CreatedAt);
        builder.HasIndex(r => new { r.ProjectId, r.Status });
        builder.HasIndex(r => new { r.SuiteName, r.Version });
    }
}

public class RunStepConfig : IEntityTypeConfiguration<RunStep>
{
    public void Configure(EntityTypeBuilder<RunStep> builder)
    {
        builder.ToTable("RunSteps");
        builder.HasKey(s => s.Id);

        // Required properties with constraints
        builder.Property(s => s.RunId).IsRequired();
        builder.Property(s => s.StepId).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Type).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Order).IsRequired();
        builder.Property(s => s.Status).IsRequired();

        // Optional properties
        builder.Property(s => s.Error).HasMaxLength(2000);

        // Add indexes for efficient querying
        builder.HasIndex(s => s.RunId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => new { s.RunId, s.Order });
        builder.HasIndex(s => new { s.RunId, s.StepId });
    }
}

public class RunEventConfig : IEntityTypeConfiguration<RunEvent>
{
    public void Configure(EntityTypeBuilder<RunEvent> builder)
    {
        builder.ToTable("RunEvents");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RunId).IsRequired();
        builder.Property(e => e.StepId).HasMaxLength(200);
        builder.Property(e => e.Kind).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Payload).IsRequired();

        // Create index for efficient querying by RunId
        builder.HasIndex(e => e.RunId);

        // Create composite index for RunId + StepId queries
        builder.HasIndex(e => new { e.RunId, e.StepId });

        // Create index for querying by Kind
        builder.HasIndex(e => e.Kind);
    }
}
