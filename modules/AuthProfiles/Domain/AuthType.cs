using System;

namespace AuthProfiles.Domain
{
    /// <summary>
    /// Authentication type for an AuthProfile.
    /// </summary>
    public enum AuthType
    {
        OAuth2ClientCredentials,
        BearerStatic,
        ApiKeyHeader,
        SessionCookie
    }
}
