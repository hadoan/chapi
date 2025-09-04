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

    public static RunPackFile Create(Guid runPackId, Guid fileId, string role = "GENERATED")
        => new(Guid.NewGuid(), runPackId, fileId, role);

    // New overload that creates a File entity from given path and content
    public static RunPackFile Create(Guid runPackId, string path, string content, string role = "GENERATED")
    {
        var fileId = Guid.NewGuid();
        var fileName = Path.GetFileName(path) ?? path;
        var bytes = content is null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(content);
        var mime = GetMimeType(path);

        // Create File entity (using minimal sensible defaults)
        var file = new ShipMvp.Domain.Files.File(
            fileId,
            containerName: "runpacks",
            fileName: fileName,
            originalFileName: fileName,
            mimeType: mime,
            size: bytes.LongLength,
            storagePath: path,
            userId: null,
            isPublic: false
        );

        var runPackFile = new RunPackFile(Guid.NewGuid(), runPackId, fileId, role);
        runPackFile.File = file;
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
