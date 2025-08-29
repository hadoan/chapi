using Microsoft.Extensions.DependencyInjection;
using Policies.Domain;
using Policies.Infrastructure.Persistence;

namespace Policies.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPoliciesInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IPolicyEfRepository, PolicyRepository>();
        services.AddSingleton<IPolicyEvaluator, PolicyEvaluator>();
        return services;
    }
}
