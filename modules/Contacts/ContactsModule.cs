using Microsoft.Extensions.DependencyInjection;
using ShipMvp.Core.Modules;
using ShipMvp.Core.Attributes;

namespace Contacts
{
    [Module]
    public class ContactsModule : IModule
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Register module services if needed at startup time
            services.AddContactsModule();
        }

        public void Configure(Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.Extensions.Hosting.IHostEnvironment env)
        {
            // nothing for now
        }
    }
}
