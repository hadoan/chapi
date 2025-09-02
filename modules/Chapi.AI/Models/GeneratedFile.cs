using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chapi.AI.Models;

public class GeneratedFile
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("size")]
    public int? Size { get; set; }
}

public class GeneratedFilesResponse
{
    [JsonPropertyName("files")]
    public List<GeneratedFile> Files { get; set; } = new();

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("total_files")]
    public int TotalFiles { get; set; }
}
