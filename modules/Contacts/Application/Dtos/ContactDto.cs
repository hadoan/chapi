using System;

namespace Contacts.Application.Dtos
{
    /// <summary>
    /// Data transfer object for Contact.
    /// </summary>
    public record ContactDto(Guid Id, string Name, string Email, string? Company, string Status);
}
