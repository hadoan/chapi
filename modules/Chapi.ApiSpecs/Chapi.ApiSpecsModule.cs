using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Chapi.ApiSpecs.Infrastructure.Configuration;
using ShipMvp.Core.DependencyInjection;

namespace Chapi.ApiSpecs;

[DependsOn(
    typeof(ShipMvp.Core.ShipMvpCoreModule)
)]
public class ChapiApiSpecsModule : ShipMvp.Core.AbpModuleBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var conf = context.Services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        var section = conf.GetSection(ApiSpecsOptions.SectionName);
        context.Services.Configure<ApiSpecsOptions>(section);

        // register reader with configured HttpClient
        context.Services.AddHttpClient("OpenApiReader", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(conf.GetValue<int>("ApiSpecs:HttpClientTimeoutSeconds", 30));
            var ua = conf.GetValue<string>("ApiSpecs:HttpClientUserAgent", "chapi-openapi-reader/1.0");
            if (!string.IsNullOrWhiteSpace(ua)) c.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
        });

        context.Services.AddTransient<Infrastructures.OpenApi.IOpenApiReader, Infrastructures.OpenApi.OpenApiReader>();
    }
}
