using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Chapi.ApiSpecs.Infrastructure.Configuration;
using Chapi.ApiSpecs.Domain;
using Chapi.ApiSpecs.Application;
using ShipMvp.Core.Modules;
using ShipMvp.Core.Attributes;

namespace Chapi.ApiSpecs;

[Module]
public sealed class ApiSpecsModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(ApiSpecsModule).Assembly);
        
        // Repository registration
        services.AddScoped<IApiSpecRepository, Infrastructure.Persistence.ApiSpecRepository>();
        
        // Application service registration
        services.AddTransient<IApiSpecAppService, ApiSpecAppService>();
        
        // OpenAPI reader registration
        services.AddTransient<Infrastructure.OpenApi.OpenApiReader>();
        
        // Configuration
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        var section = configuration.GetSection(ApiSpecsOptions.SectionName);
        services.Configure<ApiSpecsOptions>(section);

        // HTTP client for OpenAPI reader
        services.AddHttpClient("OpenApiReader", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(configuration.GetValue<int>("ApiSpecs:HttpClientTimeoutSeconds", 30));
            var ua = configuration.GetValue<string>("ApiSpecs:HttpClientUserAgent", "chapi-openapi-reader/1.0");
            if (!string.IsNullOrWhiteSpace(ua)) c.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
        });
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
        // Module specific middleware configuration placeholder
    }
}
