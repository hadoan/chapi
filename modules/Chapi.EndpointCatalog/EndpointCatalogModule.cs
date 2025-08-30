using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Chapi.EndpointCatalog.Domain;
using Chapi.EndpointCatalog.Application;
using Chapi.EndpointCatalog.Events;
using ShipMvp.Core.Modules;
using ShipMvp.Core.Attributes;

namespace Chapi.EndpointCatalog;

[Module]
public sealed class EndpointCatalogModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(EndpointCatalogModule).Assembly);
        
        // Repository registration
        services.AddScoped<IApiEndpointRepository, Infrastructure.EntityFrameworkCore.ApiEndpointRepository>();
        
        // Application service registration
        services.AddTransient<IEndpointAppService, EndpointAppService>();
        
        // Event handler registration
        services.AddTransient<BuildCatalogOnSpecImportedHandler>();
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
        // Module specific middleware configuration placeholder
    }
}
