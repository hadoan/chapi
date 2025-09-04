using System;
using System.IO;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using ShipMvp.Core.Entities;
using ShipMvp.Domain.Files;

namespace RunPack.Domain;

public class RunPackFile : Entity<Guid>
{
    public Guid RunPackId { get; private set; }
    public Guid FileId { get; private set; }

    [ForeignKey("FileId")]
    public virtual ShipMvp.Domain.Files.File? File { get; private set; }
    public string Role { get; private set; } = "GENERATED";

    private RunPackFile() : base(Guid.Empty) { }

    private RunPackFile(Guid id, Guid runPackId, Guid fileId, string role) : base(id)
    {
        RunPackId = runPackId;
        FileId = fileId;

        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

   
    // New overload that creates a File entity from given path and content
    public static RunPackFile Create(Guid runPackId, Guid runPackFileId, Guid fileId,  string role = "GENERATED")
    {
        var runPackFile = new RunPackFile(runPackFileId, runPackId,fileId, role);
        return runPackFile;
    }

    private static string GetMimeType(string path)
    {
        var ext = Path.GetExtension(path)?.ToLowerInvariant() ?? "";
        return ext switch
        {
            ".json" => "application/json",
            ".js" => "application/javascript",
            ".sh" => "text/x-shellscript",
            ".ps1" => "text/plain",
            ".txt" => "text/plain",
            ".cs" => "text/plain",
            _ => "application/octet-stream",
        };
    }
}
