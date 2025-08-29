namespace Policies.Application.Requests;

public record SavePolicyRequest(string Scope, Guid? ProjectId, string Format, string Document, DateTime? EffectiveAt);
