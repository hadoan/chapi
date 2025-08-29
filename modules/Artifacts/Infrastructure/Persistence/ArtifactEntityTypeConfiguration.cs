using Artifacts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Artifacts.Infrastructure.Persistence;

public class ArtifactEntityTypeConfiguration : IEntityTypeConfiguration<Artifact>
{
    public void Configure(EntityTypeBuilder<Artifact> builder)
    {
        builder.ToTable("Artifacts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(256);
        builder.Property(a => a.BlobName).IsRequired().HasMaxLength(512);
        builder.Property(a => a.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(a => a.Checksums).HasMaxLength(1024);
        builder.Property(a => a.RedactionProfile).HasMaxLength(128);
        builder.HasIndex(a => a.RunId);
        builder.HasIndex(a => new { a.RunId, a.Kind });
    }
}
