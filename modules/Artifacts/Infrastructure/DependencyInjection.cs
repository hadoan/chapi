using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Artifacts.Domain;
using Artifacts.Infrastructure.Persistence;

namespace Artifacts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddArtifactsInfrastructure(this IServiceCollection services)
    {
        // Register EF configuration via ModelBuilder scanning if supported elsewhere (AppDbContext OnModelCreating)
        services.AddScoped<IArtifactRepository, ArtifactRepository>();
        services.AddScoped<IArtifactEfRepository, ArtifactRepository>();
        return services;
    }
}
