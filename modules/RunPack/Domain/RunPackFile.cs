using ShipMvp.Core.Entities;

namespace RunPack.Domain;

public class RunPackFile : Entity<Guid>
{
    public Guid RunPackId { get; private set; }
    public string Path { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public int SizeBytes { get; private set; }
    public string Role { get; private set; } = "GENERATED";
    public new DateTime CreatedAt { get; private set; }

    private RunPackFile() : base(Guid.Empty) { }

    private RunPackFile(Guid id, Guid runPackId, string path, string content, string role) : base(id)
    {
        RunPackId = runPackId;
        Path = path;
        Content = content;
        SizeBytes = System.Text.Encoding.UTF8.GetByteCount(content);
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public static RunPackFile Create(Guid runPackId, string path, string content, string role = "GENERATED")
        => new(Guid.NewGuid(), runPackId, path, content, role);
}
