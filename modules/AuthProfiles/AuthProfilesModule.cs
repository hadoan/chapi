using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Abstractions;
using ShipMvp.Core.Modules;

namespace AuthProfiles
{
    [Module]
    public class AuthProfilesModule : IModule
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddApplicationPart(typeof(AuthProfilesModule).Assembly);

            services.AddScoped<Domain.IAuthProfileRepository, Infrastructure.Data.AuthProfileRepository>();
            services.AddTransient<Application.Services.IAuthProfileService, Application.Services.AuthProfileService>();
            services.AddTransient<Application.Services.IAuthProfileReadService, Application.Services.AuthProfileService>();
            services.AddTransient<Application.Services.IAuthDetectionService, Infrastructure.Services.AuthDetectionService>();
            services.AddHttpClient<Application.Services.IAuthTokenService, Infrastructure.Services.AuthTokenService>();
            services.AddTransient<Application.Services.IInjectionComposer, Infrastructure.Services.InjectionComposer>();
            services.AddTransient<Application.Services.ITokenCache, Infrastructure.Services.TokenCache>();

            // Note: ISecretStore should be provided by Secrets/Environments modules; if not, user must register one.
        }

        public void Configure(IApplicationBuilder app, Microsoft.Extensions.Hosting.IHostEnvironment env)
        {
            // no-op
        }
    }
}
