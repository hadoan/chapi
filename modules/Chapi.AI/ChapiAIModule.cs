using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Modules;
using Chapi.AI.Services;
using ShipMvp.Integration.SemanticKernel;
using ShipMvp.Integration.SemanticKernel.Infrastructure;
using RunPack.Infrastructure;
using Chat.Infrastructure;
using ShipMvp.Core.Abstractions;
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

        // Register Chat infrastructure (required for Conversation and Message repositories)
        services.AddChatInfrastructure();

        // Register GUID generator
        // services.AddSingleton<IGuidGenerator, SequentialGuidGenerator>();

        // Register AI related services here
        services.AddTransient<Chapi.AI.Services.IApiTestGenerationService, Chapi.AI.Services.ApiTestGenerationService>();
        services.AddScoped<IEndpointContextService, EndpointContextService>();
        services.AddScoped<RunPackService>();
        services.AddScoped<EndpointSelectorService>();

        // Register TestGen service and all its dependencies
        services.AddScoped<ITestGenService, TestGenService>();
        services.AddScoped<ITestGenCardGenerator, TestGenCardGenerator>();
        services.AddScoped<ITestGenFileGenerator, TestGenFileGenerator>();
        services.AddScoped<ITestGenDatabaseService, TestGenDatabaseService>();
        services.AddScoped<ITestGenDatabasePersistenceService, TestGenDatabasePersistenceService>();
        services.AddScoped<ITestGenValidationService, TestGenValidationService>();
        services.AddScoped<IChapiIRGenerator, ChapiIRGenerator>();

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

        // Register SemanticKernelAuthDetectionService as the implementation for IAuthAiDetectionService
        services.AddScoped<IAuthAiDetectionService, SemanticKernelAuthDetectionService>();
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
        // Module-specific middleware if needed
    }
}
