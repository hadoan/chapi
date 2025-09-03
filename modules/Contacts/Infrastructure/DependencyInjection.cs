using System;
using Contacts.Application.Services;
using Contacts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Contacts.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers Contacts infrastructure services.
        /// </summary>
        public static IServiceCollection AddContactsInfrastructure(this IServiceCollection services)
        {

            services.AddScoped<Application.Services.IContactRepository, ContactRepository>();

            return services;
        }
    }
}
