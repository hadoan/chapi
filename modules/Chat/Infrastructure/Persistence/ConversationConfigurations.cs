using Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chat.Infrastructure.Persistence;

public class ConversationConfig : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.ToTable("Conversations");
        b.HasKey(c => c.Id);
        b.Property(c => c.Title).HasMaxLength(256).IsRequired();
        b.HasIndex(c => c.ProjectId);

        // Ignore the Messages property to avoid conflict with the field mapping
        b.Ignore(c => c.Messages);

        // Configure relationship with Messages - use the private field "_messages" for EF Core mapping
        b.HasMany<Message>("_messages")
            .WithOne()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure concurrency token using ConcurrencyStamp from base Entity class (ABP pattern)
        b.Property(e => e.ConcurrencyStamp).IsConcurrencyToken().HasMaxLength(40);

        // Make sure to ignore the shadowed properties we removed from Conversation
        // EF will use the base Entity properties instead
    }
}

public class MessageConfig : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.ToTable("Messages");
        b.HasKey(m => m.Id);

        // Explicitly configure the foreign key relationship
        b.Property(m => m.ConversationId).IsRequired();
        b.HasIndex(m => m.ConversationId);

        b.Property(m => m.Role).HasConversion<string>().HasMaxLength(16);
        b.Property(m => m.CardType).HasConversion<string>().HasMaxLength(16);
        b.Property(m => m.Content).IsRequired();
        b.Property(m => m.CardPayload).HasMaxLength(8192);
    }
}
