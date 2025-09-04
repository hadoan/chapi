using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RunPack.Infrastructure.Persistence;

public class RunPackConfig : IEntityTypeConfiguration<RunPack.Domain.RunPack>
{
    public void Configure(EntityTypeBuilder<RunPack.Domain.RunPack> b)
    {
        b.ToTable("run_packs");
        b.HasKey(r => r.Id);
        
        // Properties
        b.Property(r => r.ProjectId).IsRequired();
        b.Property(r => r.RunId);
        b.Property(r => r.Mode).HasMaxLength(50).IsRequired().HasDefaultValue("hybrid");
        b.Property(r => r.FilesCount).IsRequired().HasDefaultValue(0);
        b.Property(r => r.ZipUrl).HasMaxLength(512);
        b.Property(r => r.Status).HasMaxLength(50).IsRequired().HasDefaultValue("draft");
        b.Property(r => r.GeneratorVersion).HasMaxLength(100);
        b.Property(r => r.CardHash).HasMaxLength(64);
        b.Property(r => r.InputsHash).HasMaxLength(64);
        b.Property(r => r.CreatedAt).IsRequired();
        b.Property(r => r.FinalizedAt);
        
        // Relationships
        b.HasMany(r => r.Files)
          .WithOne()
          .HasForeignKey(f => f.RunPackId)
          .OnDelete(DeleteBehavior.Cascade);
          
        b.HasOne(r => r.Input)
          .WithOne()
          .HasForeignKey<Domain.RunPackInput>(i => i.RunPackId)
          .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        b.HasIndex(r => r.ProjectId);
        b.HasIndex(r => r.RunId);
        b.HasIndex(r => r.Status);
        b.HasIndex(r => r.CreatedAt);
    }
}

public class RunPackFileConfig : IEntityTypeConfiguration<RunPack.Domain.RunPackFile>
{
    public void Configure(EntityTypeBuilder<RunPack.Domain.RunPackFile> b)
    {
        b.ToTable("run_pack_files");
        b.HasKey(f => f.Id);
        
        // Properties
        b.Property(f => f.RunPackId).IsRequired();
        b.Property(f => f.FileId).IsRequired();
        b.Property(f => f.Role).HasMaxLength(50).IsRequired().HasDefaultValue("GENERATED");
        b.Property(f => f.CreatedAt).IsRequired();
        
        // Relationships
        b.HasOne(f => f.File)
          .WithMany()
          .HasForeignKey(f => f.FileId)
          .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        b.HasIndex(f => f.RunPackId);
        b.HasIndex(f => f.FileId);
        b.HasIndex(f => new { f.RunPackId, f.FileId }).IsUnique();
        b.HasIndex(f => f.Role);
    }
}

public class RunPackInputConfig : IEntityTypeConfiguration<RunPack.Domain.RunPackInput>
{
    public void Configure(EntityTypeBuilder<RunPack.Domain.RunPackInput> b)
    {
        b.ToTable("run_pack_inputs");
        b.HasKey(i => i.Id);
        
        // Properties
        b.Property(i => i.RunPackId).IsRequired();
        b.Property(i => i.FileRolesJson).IsRequired();
        b.Property(i => i.RoleContextsJson).IsRequired();
        b.Property(i => i.EndpointsContext).IsRequired();
        b.Property(i => i.AllowedOps).HasMaxLength(500).IsRequired();
        b.Property(i => i.Environment).HasMaxLength(100).IsRequired();
        b.Property(i => i.ProjectId);
        b.Property(i => i.SuiteId);
        b.Property(i => i.AiModel).HasMaxLength(100);
        b.Property(i => i.Temperature);
        b.Property(i => i.MaxTokens);
        b.Property(i => i.ContextSize);
        b.Property(i => i.StopSequences).HasMaxLength(500);
        b.Property(i => i.CreatedAt).IsRequired();
        
        // Indexes
        b.HasIndex(i => i.RunPackId).IsUnique();
        b.HasIndex(i => i.ProjectId);
        b.HasIndex(i => i.SuiteId);
        b.HasIndex(i => i.Environment);
    }
}
