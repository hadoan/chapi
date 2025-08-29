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
        b.Navigation("Messages");
    }
}

public class MessageConfig : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.ToTable("Messages");
        b.HasKey(m => m.Id);
        b.HasIndex(m => m.ConversationId);
        b.Property(m => m.Role).HasConversion<string>().HasMaxLength(16);
        b.Property(m => m.CardType).HasConversion<string>().HasMaxLength(16);
        b.Property(m => m.Content).IsRequired();
        b.Property(m => m.CardPayload).HasMaxLength(8192);
    }
}
