using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Modules;
using Integrations.GitHub.Infrastructure;

namespace Integrations.GitHub;

[Module]
public sealed class GitHubIntegrationModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(GitHubIntegrationModule).Assembly);
        services.AddGitHubIntegrationInfrastructure();
    }
    public void Configure(IApplicationBuilder app, IHostEnvironment env) { }
}
