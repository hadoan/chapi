using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chapi.AI.Dto
{
    public class FileEntry
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("addedLines")]
        public int AddedLines { get; set; }
    }

    public class ChapiCard
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "Chapi";

        [JsonPropertyName("heading")]
        public string Heading { get; set; } = string.Empty;

        [JsonPropertyName("plan")]
        public List<string> Plan { get; set; } = new();

        [JsonPropertyName("files")]
        public List<FileEntry> Files { get; set; } = new();

        [JsonPropertyName("actions")]
        public List<string> Actions { get; set; } = new();
    }
}
