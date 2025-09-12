using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Modules;
using Runs.Application.Services;
using Runs.Application.Ports;
using Runs.Domain;
using Runs.Infrastructure.Eventing;
using Runs.Infrastructure.Storage;
using Runs.Infrastructure.Repositories;
using Runs.Infrastructure.EfCore;

namespace Runs;

[Module]
public class RunsModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register controllers
        services.AddControllers().AddApplicationPart(typeof(RunsModule).Assembly);

        // Register application services
        services.AddScoped<IRunsAppService, Application.Services.RunsAppService>();

        // Register domain repositories
        services.AddScoped<IRunRepository, RunRepository>();
        services.AddScoped<IRunEventRepository, RunEventRepository>();

        // Register ports and their implementations
        services.AddScoped<IRunScheduler, DistributedRunScheduler>();
        services.AddScoped<IRunIrStorage, FileRunIrStorage>();

        // Note: IDistributedEventBus and IFileStorageService are registered by infrastructure packages
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
        // Module-specific middleware if needed
    }
}
