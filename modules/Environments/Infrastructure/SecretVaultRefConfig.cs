using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Environments.Domain;

namespace Environments.Infrastructure;

public sealed class SecretVaultRefConfig : IEntityTypeConfiguration<SecretVaultRef>
{
    public void Configure(EntityTypeBuilder<SecretVaultRef> b)
    {
        b.ToTable("SecretVaultRefs", "Environment");
        b.HasKey(x => x.Id);
        b.Property(x => x.Provider).HasMaxLength(100).IsRequired();
        b.Property(x => x.KeyPath).HasMaxLength(500).IsRequired();
    }
}
