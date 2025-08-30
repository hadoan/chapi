using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShipMvp.Api;
using ShipMvp.Api.Auth;
using ShipMvp.Application;
using ShipMvp.Core;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Modules;
using ShipMvp.Domain.Shared.Constants;
using ShipMvp.Integration.SemanticKernel;
using System.Text.RegularExpressions;
using System.Reflection;
using Chapi.Api.Data;
using Projects;
using Environments;

namespace Chapi.Api;



[Module]
[DependsOn<HostModule>]
[DependsOn<ChapiDbModule>]
[DependsOn<ProjectsModule>]
[DependsOn<EnvironmentsModule>]
[DependsOn<Chapi.ApiSpecs.ApiSpecsModule>]
[DependsOn<Chapi.EndpointCatalog.EndpointCatalogModule>]
public class ChapiHostModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {

    }


    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {

    }
}
