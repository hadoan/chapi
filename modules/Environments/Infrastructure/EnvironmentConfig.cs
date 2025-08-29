using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Environments.Domain;

namespace Environments.Infrastructure;

public sealed class EnvironmentConfig : IEntityTypeConfiguration<Environments.Domain.Environment>
{
    public void Configure(EntityTypeBuilder<Environments.Domain.Environment> b)
    {
        b.ToTable("Environments", "Environment");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.BaseUrl).HasMaxLength(1000).IsRequired();
    b.Property(x => x.ProjectId).IsRequired();
    b.HasIndex(x => x.ProjectId);
        b.Property(x => x.TimeoutMs).IsRequired();
        b.Property(x => x.FollowRedirects).IsRequired();
        b.HasMany(x => x.Headers).WithOne().HasForeignKey(h => h.EnvironmentId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Secrets).WithOne().HasForeignKey(s => s.EnvironmentId).OnDelete(DeleteBehavior.Cascade);
    }
}
