namespace Policies.Application.Requests;

/// <summary>
/// Minimal boolean flags to build a simple policy; will transform to full document.
/// </summary>
public record SaveMinimalFlagsRequest(string Scope, Guid? ProjectId, bool ProdGuard, bool FailOnDrift, int MaxRetries, bool RedactionEnabled);
