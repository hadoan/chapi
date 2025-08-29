using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RunPack.Infrastructure.Persistence;

public class RunPackConfig : IEntityTypeConfiguration<RunPack.Domain.RunPack>
{
    public void Configure(EntityTypeBuilder<RunPack.Domain.RunPack> b)
    {
        b.ToTable("RunPacks");
        b.HasKey(r => r.Id);
        b.HasIndex(r => new { r.ProjectId, r.SuiteId, r.Version }).IsUnique();
        b.Property(r => r.ZipBlob).HasMaxLength(512).IsRequired();
    }
}
