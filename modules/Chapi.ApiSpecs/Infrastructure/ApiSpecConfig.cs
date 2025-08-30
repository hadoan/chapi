using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Chapi.ApiSpecs.Domain;

namespace Chapi.ApiSpecs.Infrastructure;

public sealed class ApiSpecConfig : IEntityTypeConfiguration<ApiSpec>
{
    public void Configure(EntityTypeBuilder<ApiSpec> builder)
    {
        builder.ToTable("ApiSpecs", "ApiSpecs");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.SourceUrl).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.Version).HasMaxLength(50);
        builder.Property(x => x.Sha256).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.Sha256).IsUnique();
        builder.Property(x => x.Raw).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        
        // Project relationship
        builder.Property(x => x.ProjectId).IsRequired();
    }
}
