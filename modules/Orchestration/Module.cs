using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Modules;
using Orchestration.Infrastructure;

namespace Orchestration;

[Module]
public sealed class OrchestrationModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(OrchestrationModule).Assembly);
        services.AddOrchestrationInfrastructure();
    }
    public void Configure(IApplicationBuilder app, IHostEnvironment env) { }
}
