using ShipMvp.Core.Abstractions;
using System.Linq;

namespace Chat.Domain;

public interface IConversationRepository : IRepository<Conversation, Guid>
{
    IQueryable<Conversation> Query();
}
