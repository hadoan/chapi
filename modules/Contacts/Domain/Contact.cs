using System;
using System.Collections.Generic;
using ShipMvp.Core.Entities;

namespace Contacts.Domain
{
    /// <summary>
    /// Aggregate root for Contact.
    /// </summary>
    public class Contact : AggregateRoot<Guid>
    {
        /// <summary>
        /// Concurrency token for optimistic concurrency. Configure EF mapping to use RowVersion.
        /// </summary>
        public byte[]? RowVersion { get; private set; }

        public string Name { get; private set; }

        public string Email { get; private set; }

        public string? Company { get; private set; }

        public ContactStatus Status { get; private set; }

        // EF Core requires a parameterless constructor. AggregateRoot requires an id in its constructor,
        // so we call base(Guid.Empty) here and rely on EF to populate the Id when materializing.
        // TODO: if your conventions require different handling, adapt accordingly.
        private Contact() : base(Guid.Empty)
        {
            Name = string.Empty;
            Email = string.Empty;
        }

        private Contact(Guid id, string name, string email, string? company)
            : base(id)
        {
            Name = name;
            Email = email;
            Company = company;
            Status = ContactStatus.Active;
        }

        /// <summary>
        /// Factory method to create a new contact with validation.
        /// </summary>
        public static Contact Create(string name, string email, string? company = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required", nameof(name));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required", nameof(email));
            // TODO: add proper email format validation

            return new Contact(Guid.NewGuid(), name.Trim(), email.Trim(), string.IsNullOrWhiteSpace(company) ? null : company.Trim());
        }

        /// <summary>
        /// Update contact fields.
        /// </summary>
        public void Update(string name, string email, string? company)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required", nameof(name));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required", nameof(email));
            // TODO: add email format validation and uniqueness checks in service layer

            Name = name.Trim();
            Email = email.Trim();
            Company = string.IsNullOrWhiteSpace(company) ? null : company.Trim();
        }

        public void SetStatus(ContactStatus status)
        {
            // Example invariant: cannot move from Archived back to Active
            if (Status == ContactStatus.Archived && status != ContactStatus.Archived)
                throw new InvalidOperationException("Cannot change status from Archived");

            Status = status;
        }
    }
}
