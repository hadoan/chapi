using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Modules;
using Chapi.AI.Services;
using ShipMvp.Integration.SemanticKernel;
using ShipMvp.Integration.SemanticKernel.Infrastructure;
using RunPack.Infrastructure;
using System.Linq;

namespace Chapi.AI;

[Module]
[DependsOn<SemanticKernelModule>]
public class ChapiAIModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(ChapiAIModule).Assembly);

        // Register RunPack infrastructure (required for RunPack domain services)
        services.AddRunPackInfrastructure();

        // Register AI related services here
        services.AddTransient<Chapi.AI.Services.IApiTestGenerationService, Chapi.AI.Services.ApiTestGenerationService>();
        services.AddScoped<IEndpointContextService, EndpointContextService>();
        services.AddScoped<RunPackService>();
        services.AddScoped<EndpointSelectorService>();

        // Register new refactored services
        services.AddScoped<IEndpointAnalysisService, EndpointAnalysisService>();
        services.AddScoped<IRunPackFileService, RunPackFileService>();
        services.AddScoped<IRunPackGenerationService, RunPackGenerationService>();

        // Register RunPackPlugin as scoped so each request gets its own instance
        services.AddScoped<Chapi.AI.Plugins.RunPack.RunPackPlugin>();

        // Register RunPackBuilder as scoped so each request gets its own instance
        services.AddScoped<Chapi.AI.Utilities.RunPackBuilder>();

        // Replace the default Semantic Kernel service with our custom one
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISemanticKernelService));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
        services.AddScoped<ISemanticKernelService, ChapiSemanticKernelService>();
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
        // Module-specific middleware if needed
    }
}
