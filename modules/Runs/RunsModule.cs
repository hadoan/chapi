using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Modules;

namespace Runs;

[Module]
public sealed class RunsModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(RunsModule).Assembly);
        services.AddScoped<Runs.Domain.IRunRepository, Runs.Infrastructure.Data.RunRepository>();
        services.AddTransient<Runs.Application.IRunAppService, Runs.Application.RunAppService>();
    }
    public void Configure(IApplicationBuilder app, IHostEnvironment env) { }
}
