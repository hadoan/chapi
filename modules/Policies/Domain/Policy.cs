using ShipMvp.Core.Entities;

namespace Policies.Domain;

public enum PolicyScope
{
    Org,
    Project
}

/// <summary>
/// Governance policy aggregate containing configuration document.
/// </summary>
public class Policy : Entity<Guid>
{
    public PolicyScope Scope { get; private set; }
    public Guid? ProjectId { get; private set; } // Null when scope is Org
    public string Format { get; private set; } = "json"; // json | yaml
    public string Document { get; private set; } = string.Empty; // Raw JSON/YAML text
    public DateTime EffectiveAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SupersededAt { get; private set; }
    public Guid? SupersededByPolicyId { get; private set; }

    private Policy() : base(Guid.Empty) { }
    private Policy(Guid id, PolicyScope scope, Guid? projectId, string format, string document, DateTime effectiveAt) : base(id)
    {
        if (scope == PolicyScope.Project && projectId == null)
            throw new ArgumentException("ProjectId required for project scope", nameof(projectId));
        if (string.IsNullOrWhiteSpace(document)) throw new ArgumentException("Document required", nameof(document));
        Scope = scope;
        ProjectId = projectId;
        Format = (format?.ToLowerInvariant()) switch { "yaml" => "yaml", _ => "json" };
        Document = document;
        EffectiveAt = effectiveAt;
        CreatedAt = DateTime.UtcNow;
    }

    public static Policy Create(PolicyScope scope, Guid? projectId, string format, string document, DateTime? effectiveAt = null)
        => new(Guid.NewGuid(), scope, projectId, format, document, effectiveAt ?? DateTime.UtcNow);

    public Policy SupersedeWith(Policy replacement)
    {
        SupersededAt = DateTime.UtcNow;
        SupersededByPolicyId = replacement.Id;
        return this;
    }
}

public interface IPolicyRepository : ShipMvp.Core.Abstractions.IRepository<Policy, Guid>
{
    IQueryable<Policy> Query();
    Task<Policy?> GetEffectiveAsync(PolicyScope scope, Guid? projectId, DateTime asOf, CancellationToken ct = default);
}

/// <summary>
/// Domain service for evaluating policy flags quickly without parsing full document each time.
/// </summary>
public interface IPolicyEvaluator
{
    // TODO: expand with actual evaluation logic (drift, retries, redaction, prod guard, etc.)
    bool IsProductionGuardEnabled(Policy policy);
}

public class PolicyEvaluator : IPolicyEvaluator
{
    public bool IsProductionGuardEnabled(Policy policy)
    {
        // Very naive check placeholder
        return policy.Document.Contains("prodGuard", StringComparison.OrdinalIgnoreCase);
    }
}
