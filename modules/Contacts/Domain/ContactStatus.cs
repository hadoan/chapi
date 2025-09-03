namespace Contacts.Domain
{
    /// <summary>
    /// Status for a contact. Extend as business requires.
    /// </summary>
    public enum ContactStatus
    {
        Active = 0,
        Inactive = 1,
        Archived = 2
    }
}
