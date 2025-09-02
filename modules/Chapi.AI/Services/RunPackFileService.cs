using ShipMvp.Domain.Files;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO.Compression;

namespace Chapi.AI.Services
{
    public interface IRunPackFileService
    {
        Task<RunPackFileResult> SaveRunPackAsync(
            byte[] zipData,
            Guid projectId,
            string environment,
            CancellationToken cancellationToken = default);

        Task<RunPackFileListResult> GetRunPackFilesAsync(
            Guid? projectId = null,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default);

        Task<byte[]?> DownloadRunPackFileAsync(
            Guid runId,
            string? specificFile = null,
            CancellationToken cancellationToken = default);

        Task DeleteRunPackFileAsync(
            Guid runId,
            CancellationToken cancellationToken = default);
    }

    public class RunPackFileResult
    {
        public Guid RunId { get; set; }
        public List<string> FilePaths { get; set; } = new();
        public int FileCount { get; set; }
        public string ProjectPath { get; set; } = "";
    }

    public class RunPackFileDownloadResult
    {
        public Stream FileStream { get; set; } = null!;
        public string FileName { get; set; } = "";
        public string MimeType { get; set; } = "";
    }

    public class RunPackFileListResult
    {
        public IEnumerable<RunPackFileInfo> Files { get; set; } = new List<RunPackFileInfo>();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }

    public class RunPackFileInfo
    {
        public Guid RunId { get; set; }
        public Guid ProjectId { get; set; }
        public string ProjectPath { get; set; } = "";
        public int FileCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Environment { get; set; } = "";
        public List<string> GeneratedFiles { get; set; } = new();
    }

    public class RunPackFileService : IRunPackFileService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IFileRepository _fileRepository;
        private readonly ILogger<RunPackFileService> _logger;

        private const string CONTAINER_NAME = "chapi-runpacks";

        // Temporary in-memory storage for run pack metadata
        // TODO: Replace with proper database persistence
        private static readonly ConcurrentDictionary<Guid, RunPackFileInfo> _runPackMetadata = new(); public RunPackFileService(
                IFileStorageService fileStorageService,
                IFileRepository fileRepository,
                ILogger<RunPackFileService> logger)
        {
            _fileStorageService = fileStorageService;
            _fileRepository = fileRepository;
            _logger = logger;
        }

        public async Task<RunPackFileResult> SaveRunPackAsync(
            byte[] zipData,
            Guid projectId,
            string environment,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== Saving RunPack Files to Storage ===");
            _logger.LogInformation("ProjectId: {ProjectId}, Environment: {Environment}, ZipSize: {Size} bytes",
                projectId, environment, zipData.Length);

            try
            {
                var runId = Guid.NewGuid();
                var projectPath = $"{projectId}/{runId}/generated-files";
                var uploadedFiles = new List<string>();

                _logger.LogInformation("Extracting ZIP and uploading individual files to: {ProjectPath}", projectPath);

                // Extract ZIP and upload individual files
                using var zipStream = new MemoryStream(zipData);
                using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

                foreach (var entry in archive.Entries)
                {
                    if (entry.Length == 0) continue; // Skip directories

                    var filePath = $"{projectPath}/{entry.FullName}";

                    _logger.LogDebug("Uploading file: {FilePath} ({Size} bytes)", filePath, entry.Length);

                    // Read file content from ZIP entry
                    using var entryStream = entry.Open();
                    using var fileContent = new MemoryStream();
                    await entryStream.CopyToAsync(fileContent, cancellationToken);
                    fileContent.Position = 0;

                    // Determine content type based on file extension
                    var contentType = GetContentType(entry.Name);

                    // Upload individual file to storage
                    var storagePath = await _fileStorageService.UploadAsync(
                        CONTAINER_NAME,
                        filePath,
                        fileContent,
                        contentType,
                        isPublic: false,
                        cancellationToken);

                    uploadedFiles.Add(filePath);
                }

                // Store metadata for this run pack
                var runPackInfo = new RunPackFileInfo
                {
                    RunId = runId,
                    ProjectId = projectId,
                    ProjectPath = projectPath,
                    FileCount = uploadedFiles.Count,
                    CreatedAt = DateTime.UtcNow,
                    Environment = environment,
                    GeneratedFiles = uploadedFiles
                };

                _runPackMetadata[runId] = runPackInfo;

                _logger.LogInformation("✓ RunPack saved successfully: RunId={RunId}, Files={FileCount}, Path={ProjectPath}",
                    runId, uploadedFiles.Count, projectPath);

                return new RunPackFileResult
                {
                    RunId = runId,
                    FilePaths = uploadedFiles,
                    FileCount = uploadedFiles.Count,
                    ProjectPath = projectPath
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to save RunPack files to storage");
                throw;
            }
        }

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".json" => "application/json",
                ".js" => "application/javascript",
                ".ts" => "text/typescript",
                ".py" => "text/x-python",
                ".md" => "text/markdown",
                ".txt" => "text/plain",
                ".yml" or ".yaml" => "application/x-yaml",
                ".xml" => "application/xml",
                ".html" => "text/html",
                ".css" => "text/css",
                _ => "text/plain"
            };
        }

        public async Task<RunPackFileListResult> GetRunPackFilesAsync(
            Guid? projectId = null,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== Getting RunPack Files ===");
            _logger.LogInformation("ProjectId: {ProjectId}, Page: {Page}, PageSize: {PageSize}",
                projectId, page, pageSize);

            await Task.CompletedTask; // For async signature consistency

            try
            {
                var allRunPacks = _runPackMetadata.Values.AsEnumerable();

                // Filter by project if specified
                if (projectId.HasValue)
                {
                    allRunPacks = allRunPacks.Where(rp => rp.ProjectId == projectId.Value);
                }

                // Apply pagination
                var totalCount = allRunPacks.Count();
                var skip = (page - 1) * pageSize;
                var pagedRunPacks = allRunPacks
                    .OrderByDescending(rp => rp.CreatedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToList();

                var result = new RunPackFileListResult
                {
                    Files = pagedRunPacks,
                    TotalCount = totalCount,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving RunPack files");
                throw;
            }
        }

        public async Task<byte[]?> DownloadRunPackFileAsync(
            Guid runId,
            string? specificFile = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== Downloading RunPack File ===");
            _logger.LogInformation("RunId: {RunId}, SpecificFile: {SpecificFile}", runId, specificFile);

            try
            {
                if (!_runPackMetadata.TryGetValue(runId, out var runInfo))
                {
                    _logger.LogWarning("RunPack not found: {RunId}", runId);
                    return null;
                }

                if (!string.IsNullOrEmpty(specificFile))
                {
                    // Download specific file
                    var containerName = CONTAINER_NAME;
                    var fileName = $"{runInfo.ProjectPath}/{specificFile}";
                    _logger.LogInformation("Downloading specific file: {FileName}", fileName);

                    using var fileStream = await _fileStorageService.DownloadAsync(containerName, fileName, cancellationToken);
                    using var memoryStream = new MemoryStream();
                    await fileStream.CopyToAsync(memoryStream, cancellationToken);
                    return memoryStream.ToArray();
                }
                else
                {
                    // Download all files as ZIP
                    _logger.LogInformation("Creating ZIP from {FileCount} files", runInfo.GeneratedFiles.Count);
                    using var zipStream = new MemoryStream();
                    using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var fileName in runInfo.GeneratedFiles)
                        {
                            var containerName = CONTAINER_NAME;
                            var fullFileName = $"{runInfo.ProjectPath}/{fileName}";

                            try
                            {
                                using var fileStream = await _fileStorageService.DownloadAsync(containerName, fullFileName, cancellationToken);
                                var entry = zipArchive.CreateEntry(fileName);
                                using var entryStream = entry.Open();
                                await fileStream.CopyToAsync(entryStream, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "File not found in storage: {FileName}", fullFileName);
                            }
                        }
                    }

                    _logger.LogInformation("✓ ZIP created successfully with {Size} bytes", zipStream.Length);
                    return zipStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error downloading run pack file. RunId: {RunId}, SpecificFile: {SpecificFile}", runId, specificFile);
                return null;
            }
        }

        public async Task DeleteRunPackFileAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== Deleting RunPack ===");
            _logger.LogInformation("RunId: {RunId}", runId);

            try
            {
                if (!_runPackMetadata.TryGetValue(runId, out var runInfo))
                {
                    _logger.LogWarning("RunPack not found: {RunId}", runId);
                    return;
                }

                // Delete all individual files from storage
                foreach (var fileName in runInfo.GeneratedFiles)
                {
                    var containerName = CONTAINER_NAME;
                    var fullFileName = $"{runInfo.ProjectPath}/{fileName}";
                    try
                    {
                        await _fileStorageService.DeleteAsync(containerName, fullFileName, cancellationToken);
                        _logger.LogDebug("Deleted file: {FileName}", fullFileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete file: {FileName}", fullFileName);
                        // Continue deleting other files even if one fails
                    }
                }

                // Remove from metadata
                _runPackMetadata.TryRemove(runId, out _);

                _logger.LogInformation("✓ RunPack deleted successfully: {RunId}", runId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting RunPack: {RunId}", runId);
                throw;
            }
        }

        private static string? ExtractProjectIdFromTags(string? tags)
        {
            if (string.IsNullOrEmpty(tags)) return null;

            var match = System.Text.RegularExpressions.Regex.Match(tags, @"project:([^,]+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string? ExtractEnvironmentFromTags(string? tags)
        {
            if (string.IsNullOrEmpty(tags)) return null;

            var match = System.Text.RegularExpressions.Regex.Match(tags, @"env:([^,]+)");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
