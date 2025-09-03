namespace Contacts.Application.Requests
{
    /// <summary>
    /// Request to create a contact.
    /// </summary>
    public record CreateContactRequest(string Name, string Email, string? Company);
}
