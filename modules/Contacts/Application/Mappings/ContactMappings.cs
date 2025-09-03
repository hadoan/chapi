using System;
using Contacts.Application.Dtos;
using Contacts.Application.Requests;
using Contacts.Domain;

namespace Contacts.Application.Mappings
{
    public static class ContactMappings
    {
        public static ContactDto ToDto(this Contact contact)
            => new(contact.Id, contact.Name, contact.Email, contact.Company, contact.Status.ToString());

        public static void UpdateFrom(this Contact contact, UpdateContactRequest request)
        {
            // Concurrency/RowVersion handling should be enforced at repository/DbContext level
            contact.Update(request.Name, request.Email, request.Company);
            // TODO: handle RowVersion validation if using EF concurrency checks
        }
    }
}
