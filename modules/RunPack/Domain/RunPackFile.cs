using System.ComponentModel.DataAnnotations.Schema;
using ShipMvp.Core.Entities;
using ShipMvp.Domain.Files;

namespace RunPack.Domain;

public class RunPackFile : Entity<Guid>
{
    public Guid RunPackId { get; private set; }
    public Guid FileId { get; private set; }

    [ForeignKey("FileId")]
    public virtual ShipMvp.Domain.Files.File File { get; private set; }
    public string Role { get; private set; } = "GENERATED";

    private RunPackFile() : base(Guid.Empty) { }

    private RunPackFile(Guid id, Guid runPackId, Guid fileId, string role) : base(id)
    {
        RunPackId = runPackId;
        FileId = fileId;

        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public static RunPackFile Create(Guid runPackId, Guid fileId, string role = "GENERATED")
        => new(Guid.NewGuid(), runPackId, fileId, role);
}
