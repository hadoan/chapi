using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Modules;
using RunPack.Infrastructure;

namespace RunPack;

[Module]
public sealed class RunPackModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(RunPackModule).Assembly);
        services.AddRunPackInfrastructure();
    }
    public void Configure(IApplicationBuilder app, IHostEnvironment env) { }
}
