using Integrations.GitHub.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integrations.GitHub.Infrastructure.Persistence;

public class GitHubInstallationConfig : IEntityTypeConfiguration<GitHubInstallation>
{
    public void Configure(EntityTypeBuilder<GitHubInstallation> b)
    {
        b.ToTable("GitHubInstallations");
        b.HasKey(i => i.Id);
        b.HasIndex(i => i.InstallationId).IsUnique();
        b.Property(i => i.InstallationId).IsRequired();
        b.Property(i => i.ConnectedAt).IsRequired();
        b.Navigation("Repos");
    }
}

public class GitHubRepoConfig : IEntityTypeConfiguration<GitHubRepo>
{
    public void Configure(EntityTypeBuilder<GitHubRepo> b)
    {
        b.ToTable("GitHubRepos");
        b.HasKey(r => r.Id);
        b.HasIndex(r => new { r.InstallationId, r.RepoId }).IsUnique();
        b.Property(r => r.Name).HasMaxLength(256).IsRequired();
        b.Property(r => r.RepoId).IsRequired();
    }
}

public class PrCheckConfig : IEntityTypeConfiguration<PrCheck>
{
    public void Configure(EntityTypeBuilder<PrCheck> b)
    {
        b.ToTable("PrChecks");
        b.HasKey(c => c.Id);
        b.HasIndex(c => c.RunId);
        b.Property(c => c.Status).HasConversion<string>().HasMaxLength(32);
        b.Property(c => c.Url).HasMaxLength(512);
    }
}
