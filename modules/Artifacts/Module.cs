using Artifacts.Application.Services;
using Artifacts.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Modules;

namespace Artifacts;

[Module]
public sealed class ArtifactsModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(ArtifactsModule).Assembly);
        services.AddArtifactsInfrastructure();
        services.AddScoped<IArtifactService, ArtifactService>();
    }
    public void Configure(IApplicationBuilder app, IHostEnvironment env) { }
}
