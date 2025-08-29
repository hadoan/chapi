using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Policies.Domain;

namespace Policies.Infrastructure.Persistence;

public class PolicyEntityTypeConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Scope).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.Format).HasMaxLength(8);
        builder.Property(p => p.Document).IsRequired();
        builder.HasIndex(p => new { p.Scope, p.ProjectId, p.EffectiveAt });
    }
}
