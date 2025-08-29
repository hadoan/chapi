using Microsoft.Extensions.DependencyInjection;
using RunPack.Application.Services;
using RunPack.Domain;
using RunPack.Infrastructure.Persistence;

namespace RunPack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRunPackInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IRunPackRepository, RunPackRepository>();
        services.AddScoped<IRunPackAppService, RunPackAppService>();
        return services;
    }
}
