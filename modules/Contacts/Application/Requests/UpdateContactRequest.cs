using System;

namespace Contacts.Application.Requests
{
    /// <summary>
    /// Request to update a contact.
    /// </summary>
    public record UpdateContactRequest(Guid Id, string Name, string Email, string? Company, byte[]? RowVersion);
}
