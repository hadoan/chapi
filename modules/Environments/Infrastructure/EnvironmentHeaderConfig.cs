using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Environments.Domain;

namespace Environments.Infrastructure;

public sealed class EnvironmentHeaderConfig : IEntityTypeConfiguration<EnvironmentHeader>
{
    public void Configure(EntityTypeBuilder<EnvironmentHeader> b)
    {
        b.ToTable("EnvironmentHeaders", "Environment");
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).HasMaxLength(200).IsRequired();
        b.Property(x => x.Value).HasMaxLength(2000);
    }
}
