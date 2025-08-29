using Policies.Application.Dtos;
using Policies.Domain;

namespace Policies.Application.Mappings;

public static class PolicyMappings
{
    public static PolicyDto ToDto(this Policy p) => new(p.Id, p.Scope.ToString(), p.ProjectId, p.Format, p.Document, p.EffectiveAt, p.CreatedAt, p.SupersededAt, p.SupersededByPolicyId);
}
