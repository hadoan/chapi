using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthProfiles.Domain;

namespace AuthProfiles.Infrastructure
{
    public class AuthProfileSecretRefConfig : IEntityTypeConfiguration<AuthProfileSecretRef>
    {
        public void Configure(EntityTypeBuilder<AuthProfileSecretRef> builder)
        {
            builder.ToTable("AuthProfileSecretRefs");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.Key).IsRequired().HasMaxLength(100);
            builder.Property(x => x.SecretRef).IsRequired().HasMaxLength(500);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();

            builder.HasIndex(x => new { x.AuthProfileId, x.Key }).IsUnique();
        }
    }
}
