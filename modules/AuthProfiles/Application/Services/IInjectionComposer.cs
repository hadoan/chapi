using System.Collections.Generic;
using AuthProfiles.Application.Dtos;
using AuthProfiles.Domain;

namespace AuthProfiles.Application.Services
{
    public interface IInjectionComposer
    {
        ExportInjectionResult Compose(AuthProfile profile, TestAuthResult token, IReadOnlyDictionary<string, string> resolvedSecrets);
    }
}
