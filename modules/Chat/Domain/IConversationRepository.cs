using ShipMvp.Core.Abstractions;
using System.Linq;

namespace Chat.Domain;

public interface IConversationRepository : IRepository<Conversation, Guid>
{
    IQueryable<Conversation> Query();
    Task<bool> ExistsAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task AddMessageAsync(Message message, CancellationToken cancellationToken = default);
    Task AddMessagesAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default);
}
