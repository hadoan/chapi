using ShipMvp.Core.Entities;

namespace RunPack.Domain;

public class RunPack : Entity<Guid>
{
    public Guid ProjectId { get; private set; }
    public Guid SuiteId { get; private set; }
    public int Version { get; private set; }
    public string ZipBlob { get; private set; } = string.Empty; // blob reference
    public DateTime CreatedAt { get; private set; }
    private RunPack() : base(Guid.Empty) { }
    private RunPack(Guid id, Guid projectId, Guid suiteId, int version, string zipBlob) : base(id)
    { ProjectId = projectId; SuiteId = suiteId; Version = version; ZipBlob = zipBlob; CreatedAt = DateTime.UtcNow; }
    public static RunPack Build(Guid projectId, Guid suiteId, int version, string blob) => new(Guid.NewGuid(), projectId, suiteId, version, blob);
}

public interface IRunPackRepository : ShipMvp.Core.Abstractions.IRepository<RunPack, Guid>
{
    IQueryable<RunPack> Query();
    Task<int> GetLatestVersionAsync(Guid projectId, Guid suiteId, CancellationToken ct);
}
