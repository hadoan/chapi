using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Runs.Domain;

namespace Runs.Infrastructure.EfCore;

public static class RunsDbContextModelBuilder
{
    public static void ConfigureRuns(this ModelBuilder builder)
    {
        builder.Entity<Run>(entity =>
        {
            entity.ToTable("Runs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            
            entity.Property(e => e.ProjectId);
            entity.Property(e => e.SuiteName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Version).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Actor).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Trigger).HasMaxLength(50).IsRequired();
            entity.Property(e => e.IrPath).HasMaxLength(500);
            entity.Property(e => e.Error).HasMaxLength(2000);
            
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.StartedAt);
            entity.Property(e => e.FinishedAt);
            entity.Property(e => e.UpdatedAt);

            entity.HasMany(r => r.Steps)
                .WithOne()
                .HasForeignKey(s => s.RunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        builder.Entity<RunStep>(entity =>
        {
            entity.ToTable("RunSteps");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            
            entity.Property(e => e.RunId).IsRequired();
            entity.Property(e => e.StepId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Order).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.StatusCode);
            entity.Property(e => e.DurationMs);
            entity.Property(e => e.Error).HasMaxLength(2000);
            
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.StartedAt);
            entity.Property(e => e.FinishedAt);
            entity.Property(e => e.UpdatedAt);

            entity.HasIndex(e => e.RunId);
            entity.HasIndex(e => new { e.RunId, e.Order });
        });

        builder.Entity<RunEvent>(entity =>
        {
            entity.ToTable("RunEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            
            entity.Property(e => e.RunId).IsRequired();
            entity.Property(e => e.StepId).HasMaxLength(100);
            entity.Property(e => e.Kind).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Payload).HasColumnType("jsonb").IsRequired();
            
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);

            entity.HasIndex(e => e.RunId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}