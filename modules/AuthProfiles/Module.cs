using Microsoft.Extensions.DependencyInjection;

namespace AuthProfiles
{
    public static class Module
    {
        public static IServiceCollection AddAuthProfilesModule(this IServiceCollection services)
        {
            // Mirror what's in AuthProfilesModule.ConfigureServices
            services.AddScoped<Domain.IAuthProfileRepository, Infrastructure.Data.AuthProfileRepository>();
            services.AddTransient<Application.Services.IAuthProfileService, Application.Services.AuthProfileService>();
            services.AddTransient<Application.Services.IAuthProfileReadService, Application.Services.AuthProfileService>();
            services.AddTransient<Application.Services.IAuthDetectionService, Infrastructure.Services.AuthDetectionService>();
            services.AddHttpClient<Application.Services.IAuthTokenService, Infrastructure.Services.AuthTokenService>();
            services.AddTransient<Application.Services.IInjectionComposer, Infrastructure.Services.InjectionComposer>();
            services.AddTransient<Application.Services.ITokenCache, Infrastructure.Services.TokenCache>();
            return services;
        }
    }
}
