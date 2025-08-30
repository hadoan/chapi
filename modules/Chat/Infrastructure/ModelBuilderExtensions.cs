using Microsoft.EntityFrameworkCore;
using Chat.Infrastructure.Persistence;

namespace Environments.Infrastructure;

public static class ModelBuilderExtensions
{
    public static void ConfigureChatEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new MessageConfig());
        modelBuilder.ApplyConfiguration(new ConversationConfig());
    }
}
