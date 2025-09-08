using Microsoft.EntityFrameworkCore;

namespace AuthProfiles.Infrastructure
{
    public static class ModelBuilderExtensions
    {
        public static void ConfigureAuthProfilessEntities(this ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new AuthProfileConfig());
            modelBuilder.ApplyConfiguration(new AuthProfileSecretRefConfig());
        }
    }
}
