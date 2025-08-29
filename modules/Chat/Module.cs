using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Modules;
using Chat.Infrastructure;

namespace Chat;

[Module]
public sealed class ChatModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(ChatModule).Assembly);
        services.AddChatInfrastructure();
    }
    public void Configure(IApplicationBuilder app, IHostEnvironment env) { }
}
