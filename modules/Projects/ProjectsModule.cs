using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShipMvp.Core.Modules;
using ShipMvp.Core.Attributes;

namespace Projects;

[Module]
public sealed class ProjectsModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(ProjectsModule).Assembly);
        services.AddScoped<Projects.Domain.IProjectRepository, Projects.Infrastructure.Data.ProjectRepository>();
        services.AddTransient<Projects.Application.IProjectService, Projects.Application.ProjectService>();
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
        // Module specific middleware configuration placeholder
    }
}
