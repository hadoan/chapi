using System;

namespace AuthProfiles.Domain
{
    /// <summary>
    /// How an auth value should be injected into requests.
    /// </summary>
    public enum InjectionMode
    {
        Header,
        Query,
        Cookie
    }
}
