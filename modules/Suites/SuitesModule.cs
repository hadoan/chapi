using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShipMvp.Core.Modules;
using ShipMvp.Core.Attributes;
using Suites.Application;
using Suites.Infrastructure.Data;
using Suites.Infrastructure.Config;

namespace Suites;

[Module]
public sealed class SuitesModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(SuitesModule).Assembly);
        services.AddScoped<Suites.Domain.ISuiteRepository, SuiteRepository>();
        services.AddTransient<Suites.Application.ISuiteAppService, Suites.Application.SuiteAppService>();
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
        // Module specific middleware placeholder
    }
}
