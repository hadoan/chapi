using Microsoft.Extensions.DependencyInjection;
using Orchestration.Application.Services;
using Orchestration.Domain;

namespace Orchestration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrchestrationInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IRunScheduler, SimpleRunScheduler>();
        services.AddSingleton<IRunExecutor, SimpleRunExecutor>();
        services.AddSingleton<IFlakeClassifier, NaiveFlakeClassifier>();
        services.AddScoped<IOrchestrationAppService, OrchestrationAppService>();
        return services;
    }
}
