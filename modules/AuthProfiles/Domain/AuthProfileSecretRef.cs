using System;
using System.ComponentModel.DataAnnotations;
using ShipMvp.Core.Entities;

namespace AuthProfiles.Domain
{
    /// <summary>
    /// Reference to a secret used by an AuthProfile. Only the reference is stored, not the secret value.
    /// </summary>
    public class AuthProfileSecretRef : Entity<Guid>
    {
        private AuthProfileSecretRef()
            : base(Guid.Empty)
        {
        }

        public AuthProfileSecretRef(Guid id, Guid authProfileId, string key, string secretRef, string? notes = null)
            : base(id)
        {
            AuthProfileId = authProfileId;
            Key = key ?? throw new ArgumentNullException(nameof(key));
            SecretRef = secretRef ?? throw new ArgumentNullException(nameof(secretRef));
            Notes = notes;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid AuthProfileId { get; private set; }

        [Required]
        [MaxLength(100)]
        public string Key { get; private set; } = null!;

        [Required]
        [MaxLength(500)]
        public string SecretRef { get; private set; } = null!;

        public string? Notes { get; private set; }

        // Navigation property for EF
        public virtual AuthProfile? Profile { get; private set; }

        public void UpdateSecretRef(string secretRef, string? notes = null)
        {
            SecretRef = secretRef ?? throw new ArgumentNullException(nameof(secretRef));
            Notes = notes;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
