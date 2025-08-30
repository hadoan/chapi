using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Chapi.EndpointCatalog.Domain;

namespace Chapi.EndpointCatalog.Infrastructure;

public sealed class ApiEndpointConfig : IEntityTypeConfiguration<ApiEndpoint>
{
    public void Configure(EntityTypeBuilder<ApiEndpoint> builder)
    {
        builder.ToTable("ApiEndpoints", "EndpointCatalog");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.SpecId, x.Method, x.Path }).IsUnique();

        builder.Property(x => x.Method).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Path).IsRequired().HasMaxLength(1024);
        builder.Property(x => x.OperationId).HasMaxLength(256);
        builder.Property(x => x.Summary).HasMaxLength(1024);
        builder.Property(x => x.Description).HasColumnType("text");

        builder.Property(x => x.Tags).HasColumnType("text[]");
        builder.Property(x => x.Servers).HasColumnType("jsonb");
        builder.Property(x => x.Security).HasColumnType("jsonb");
        builder.Property(x => x.Parameters).HasColumnType("jsonb");
        builder.Property(x => x.Request).HasColumnType("jsonb");
        builder.Property(x => x.Responses).HasColumnType("jsonb");

        builder.Property(x => x.Source).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Deprecated).IsRequired();
        
        // Project and spec relationships
        builder.Property(x => x.ProjectId).IsRequired();
        builder.Property(x => x.SpecId).IsRequired();
    }
}
