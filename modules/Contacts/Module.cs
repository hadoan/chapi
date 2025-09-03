using Contacts.Application.Services;
using Contacts.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Contacts
{
    public static class ContactsServiceCollectionExtensions
    {
        public static IServiceCollection AddContactsModule(this IServiceCollection services)
        {
            // Register application services
            services.AddScoped<IContactService, Application.Services.ContactService>();
            services.AddScoped<IContactReadService, Application.Services.ContactService>();

            // Register infrastructure
            services.AddContactsInfrastructure();

            return services;
        }
    }
}
