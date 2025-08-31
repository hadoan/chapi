using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Modules;
using Chapi.AI.Services;

namespace Chapi.AI;

[Module]
public class ChapiAIModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(ChapiAIModule).Assembly);

        // Register AI related services here
        services.AddTransient<Chapi.AI.Services.IApiTestGenerationService, Chapi.AI.Services.ApiTestGenerationService>();
        services.AddScoped<IEndpointContextService, EndpointContextService>();
        services.AddScoped<RunPackService>();
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
        // Module-specific middleware if needed
    }
}
