using Microsoft.Extensions.DependencyInjection;
using Integrations.GitHub.Domain;
using Integrations.GitHub.Infrastructure.Persistence;
using Integrations.GitHub.Application.Services;

namespace Integrations.GitHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGitHubIntegrationInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IGitHubInstallationRepository, GitHubInstallationRepository>();
        services.AddScoped<IPrCheckRepository, PrCheckRepository>();
        services.AddSingleton<IGitHubWebhookVerifier, HmacGitHubWebhookVerifier>();
        services.AddScoped<IGitHubIntegrationAppService, GitHubIntegrationAppService>();
        // TODO: register IGitHubApiClient implementation
        return services;
    }
}
