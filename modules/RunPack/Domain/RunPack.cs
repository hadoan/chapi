using ShipMvp.Core.Entities;

namespace RunPack.Domain;

public class RunPack : Entity<Guid>
{
    public Guid ProjectId { get; private set; }
    public Guid? RunId { get; private set; } // Link to Runs table if executed
    public string Mode { get; private set; } = "hybrid";
    public int FilesCount { get; private set; }
    public string? ZipUrl { get; private set; }
    public string Status { get; private set; } = "draft";
    public string? GeneratorVersion { get; private set; }
    public string? CardHash { get; private set; }
    public string? InputsHash { get; private set; }
    public new DateTime CreatedAt { get; private set; }
    public DateTime? FinalizedAt { get; private set; }

    // Navigation properties
    private readonly List<RunPackFile> _files = new();
    public IReadOnlyList<RunPackFile> Files => _files.AsReadOnly();
    
    public RunPackInput? Input { get; private set; }

    private RunPack() : base(Guid.Empty) { }

    private RunPack(Guid id, Guid projectId, string mode = "hybrid") : base(id)
    {
        ProjectId = projectId;
        Mode = mode;
        Status = "draft";
        CreatedAt = DateTime.UtcNow;
        FilesCount = 0;
    }

    public static RunPack Create(Guid projectId, string mode = "hybrid")
        => new(Guid.NewGuid(), projectId, mode);

    public void AddFile(string path, string content, string role = "GENERATED")
    {
        var file = RunPackFile.Create(Id, path, content, role);
        _files.Add(file);
        FilesCount = _files.Count;
    }

    public void SetInput(string fileRolesJson, string roleContextsJson, string endpointsContext, 
        string allowedOps, string environment)
    {
        Input = RunPackInput.Create(Id, fileRolesJson, roleContextsJson, endpointsContext, allowedOps, environment);
    }

    public void SetZipUrl(string zipUrl)
    {
        ZipUrl = zipUrl;
    }

    public void Complete()
    {
        Status = "finalized";
        FinalizedAt = DateTime.UtcNow;
    }

    public void LinkToRun(Guid runId)
    {
        RunId = runId;
    }

    public void SetHashes(string cardHash, string inputsHash)
    {
        CardHash = cardHash;
        InputsHash = inputsHash;
    }

    public void SetGeneratorVersion(string version)
    {
        GeneratorVersion = version;
    }
}

public interface IRunPackRepository : ShipMvp.Core.Abstractions.IRepository<RunPack, Guid>
{
    IQueryable<RunPack> Query();
}
