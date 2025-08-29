using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShipMvp.Core.Modules;
using ShipMvp.Core.Attributes;

namespace Environments;

[Module]
public sealed class EnvironmentsModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(EnvironmentsModule).Assembly);
        services.AddScoped<Environments.Domain.IEnvironmentRepository, Environments.Infrastructure.Data.EnvironmentRepository>();
        services.AddTransient<Environments.Application.IEnvironmentAppService, Environments.Application.EnvironmentAppService>();
        services.AddTransient<Environments.Application.ISecretAppService, Environments.Application.SecretAppService>();
        // Secret store providers (basic registration). Consumers can choose which to use.
        services.AddSingleton<Environments.Domain.ISecretStore, Environments.Infrastructure.SecretStores.InDbEncryptedSecretStore>();
        // TODO: Register AWS/GCP/Azure providers conditionally
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
        // Module-specific middleware configuration placeholder
    }
}
