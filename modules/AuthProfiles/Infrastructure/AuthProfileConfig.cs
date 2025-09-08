using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthProfiles.Domain;

namespace AuthProfiles.Infrastructure
{
    public class AuthProfileConfig : IEntityTypeConfiguration<AuthProfile>
    {
        public void Configure(EntityTypeBuilder<AuthProfile> builder)
        {
            builder.ToTable("AuthProfiles");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.EnvironmentKey).IsRequired().HasMaxLength(100);
            builder.Property(x => x.InjectionName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.InjectionFormat).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.DetectSource).IsRequired().HasMaxLength(200);

            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.HasIndex(x => new { x.ProjectId, x.ServiceId, x.EnvironmentKey }).IsUnique();
            builder.HasIndex(x => new { x.ServiceId, x.EnvironmentKey, x.Enabled });

            // configure secret refs as a field-backed collection
            builder.Metadata.FindNavigation(nameof(AuthProfile.SecretRefs))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);

            builder.HasMany(e => ((IEnumerable<AuthProfileSecretRef>)e.SecretRefs))
                .WithOne(r => r.Profile)
                .HasForeignKey("AuthProfileId")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
