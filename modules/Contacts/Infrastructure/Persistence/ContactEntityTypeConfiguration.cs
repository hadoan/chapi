using System;
using Contacts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Contacts.Infrastructure.Persistence
{
    public class ContactEntityTypeConfiguration : IEntityTypeConfiguration<Contact>
    {
        public void Configure(EntityTypeBuilder<Contact> builder)
        {
            builder.ToTable("Contacts");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Email)
                .IsRequired()
                .HasMaxLength(320);

            builder.Property(c => c.Company)
                .HasMaxLength(200);

            builder.Property<byte[]?>("RowVersion")
                .IsRowVersion();

            builder.Property(c => c.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);
        }
    }
}
