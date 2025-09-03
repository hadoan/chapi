using Microsoft.EntityFrameworkCore;
using Contacts.Domain;

namespace Contacts.Infrastructure;

public static class ModelBuilderExtensions
{
    public static void ConfigureContactsEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Persistence.ContactEntityTypeConfiguration());
    }
}
