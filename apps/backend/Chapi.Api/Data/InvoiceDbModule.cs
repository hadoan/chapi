using ShipMvp.Application.Infrastructure.Data;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Modules;

namespace Chapi.Api.Data;


[Module]
public class InvoiceDbModule : DatabaseModule<InvoiceDbContext>, IModule
{    
}
