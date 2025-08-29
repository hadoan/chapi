using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Policies.Application.Services;
using Policies.Infrastructure;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Modules;

namespace Policies;

[Module]
public sealed class PoliciesModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(PoliciesModule).Assembly);
        services.AddPoliciesInfrastructure();
        services.AddScoped<IPolicyService, PolicyService>();
    }
    public void Configure(IApplicationBuilder app, IHostEnvironment env) { }
}
