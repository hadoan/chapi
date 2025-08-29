namespace Policies.Application.Dtos;

public record PolicyDto(Guid Id, string Scope, Guid? ProjectId, string Format, string Document, DateTime EffectiveAt, DateTime CreatedAt, DateTime? SupersededAt, Guid? SupersededByPolicyId);
