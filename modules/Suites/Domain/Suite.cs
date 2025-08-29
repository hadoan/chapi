namespace Suites.Domain;

public enum SuiteStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2
}

public class Suite : ShipMvp.Core.Entities.Entity<Guid>
{
    private readonly List<SuiteFile> _files = new();
    private readonly List<TestCase> _testCases = new();
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public SuiteStatus Status { get; private set; } = SuiteStatus.Draft;
    public IReadOnlyCollection<SuiteFile> Files => _files;
    public IReadOnlyCollection<TestCase> TestCases => _testCases;

    private Suite() : base(Guid.Empty) { }
    private Suite(Guid id, string name, string? description) : base(id)
    {
        Name = name;
        Description = description;
    }

    public static Suite Create(string name, string? description = null)
        => new Suite(Guid.NewGuid(), name, description);

    public void Update(string name, string? description, IEnumerable<(string path, string kind)> files, IEnumerable<(string name, string command)> testCases)
    {
        Name = name;
        Description = description;
        _files.Clear();
        _files.AddRange(files.Select(f => SuiteFile.Create(f.path, f.kind)));
        _testCases.Clear();
        _testCases.AddRange(testCases.Select(tc => TestCase.Create(tc.name, tc.command)));
    }

    public void Activate() { if (Status == SuiteStatus.Draft) Status = SuiteStatus.Active; }
    public void Archive() { if (Status != SuiteStatus.Archived) Status = SuiteStatus.Archived; }
}

public class SuiteFile : ShipMvp.Core.Entities.Entity<Guid>
{
    public Guid SuiteId { get; private set; }
    public string Path { get; private set; } = string.Empty;
    public string Kind { get; private set; } = string.Empty;
    private SuiteFile() : base(Guid.Empty) { }
    private SuiteFile(Guid id, string path, string kind) : base(id) { Path = path; Kind = kind; }
    public static SuiteFile Create(string path, string kind) => new(Guid.NewGuid(), path, kind);
}

public class TestCase : ShipMvp.Core.Entities.Entity<Guid>
{
    public Guid SuiteId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Command { get; private set; } = string.Empty;
    private TestCase() : base(Guid.Empty) { }
    private TestCase(Guid id, string name, string command) : base(id) { Name = name; Command = command; }
    public static TestCase Create(string name, string command) => new(Guid.NewGuid(), name, command);
}

public interface ISuiteRepository : ShipMvp.Core.Abstractions.IRepository<Suite, Guid>
{
    Task<(IEnumerable<Suite> Items, int Total)> GetPagedAsync(int page, int pageSize, SuiteStatus? status = null, string? search = null, CancellationToken cancellationToken = default);
}
