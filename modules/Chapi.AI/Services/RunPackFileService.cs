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
using RunPack.Domain;
using Microsoft.EntityFrameworkCore;
using ShipMvp.Core.Abstractions;

namespace Chapi.AI.Services
{

    public class RunPackFileResult
    {
        public Guid RunId { get; set; }
        //public List<string> FilePaths { get; set; } = new();
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
        private readonly IRunPackFileRepository _runPackFileRepository;
        private readonly IRunPackRepository _runPackRepository;
        private readonly ILogger<RunPackFileService> _logger;
        private readonly IGuidGenerator _guidGenerator;

        private const string CONTAINER_NAME = "chapi-runpacks";

        public RunPackFileService(
                IFileStorageService fileStorageService,
                IFileRepository fileRepository,
                IRunPackRepository runPackRepository,
                IRunPackFileRepository runPackFileRepository,
                IGuidGenerator guidGenerator,
                ILogger<RunPackFileService> logger)
        {
            _fileStorageService = fileStorageService;
            _fileRepository = fileRepository;
            _runPackRepository = runPackRepository;
            _guidGenerator = guidGenerator;
            _runPackFileRepository = runPackFileRepository;
            _logger = logger;
        }

        public async Task<RunPackFileResult> SaveRunPackAsync(
            byte[] zipData,
            Guid projectId,
            string environment,
            Guid runPackId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== Saving RunPack Files to Storage and Database ===");
            _logger.LogInformation("ProjectId: {ProjectId}, Environment: {Environment}, ZipSize: {Size} bytes, RunPackId: {RunPackId}",
                projectId, environment, zipData.Length, runPackId);

            try
            {
                var projectPath = $"{projectId}/{runPackId}/generated-files";
                var createdFiles = new List<ShipMvp.Domain.Files.File>();
                var createdPackFiles = new List<RunPackFile>();

                _logger.LogInformation("Extracting ZIP and uploading individual files to: {ProjectPath}", projectPath);

                // Get the RunPack entity if provided to add files to it
                var runPack = await _runPackRepository.GetByIdAsync(runPackId, cancellationToken);
                if (runPack == null)
                {
                    _logger.LogWarning("RunPack not found: {RunPackId}, proceeding without linking files", runPackId);
                }

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

                    // Create File entity and save to database
                    var fileEntity = new ShipMvp.Domain.Files.File(
                        _guidGenerator.Create(),
                        containerName: CONTAINER_NAME,
                        fileName: entry.Name,
                        originalFileName: entry.Name,
                        mimeType: contentType,
                        size: entry.Length,
                        storagePath: storagePath,
                        userId: null, // System generated file
                        isPublic: false
                    );

                    // Set additional properties
                    fileEntity.Tags = $"project:{projectId},env:{environment},runpack:{runPackId}";
                    createdFiles.Add(fileEntity);

                    // Link file to RunPack if RunPack entity exists
                    if (runPack != null)
                    {
                        var runPackFile = RunPackFile.Create(runPackId, _guidGenerator.Create(), fileEntity.Id);
                        createdPackFiles.Add(runPackFile);
                    }

                }

                // Update RunPack if it exists
                if (runPack != null)
                {
                    await _fileRepository.InsertManyAsync(createdFiles);
                    await _runPackFileRepository.InsertManyAsync(createdPackFiles);
                    _logger.LogInformation("✓ RunPack updated with {FileCount} files", createdFiles.Count);
                }


                return new RunPackFileResult
                {
                    RunId = runPackId,
                    FileCount = createdFiles.Count,
                    ProjectPath = projectPath
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to save RunPack files to storage and database");
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

        public async Task<RunPackFileListResult> GetRunPackFilesByProjectAsync(
            Guid? projectId = null,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== Getting RunPack Files from Database ===");
            _logger.LogInformation("ProjectId: {ProjectId}, Page: {Page}, PageSize: {PageSize}",
                projectId, page, pageSize);

            try
            {
                // Get run packs from database
                var runPackQuery = _runPackRepository.Query();

                // Filter by project if specified
                if (projectId.HasValue)
                {
                    runPackQuery = runPackQuery.Where(rp => rp.ProjectId == projectId.Value);
                }

                // Apply pagination
                var totalCount = await runPackQuery.CountAsync(cancellationToken);
                var skip = (page - 1) * pageSize;

                var runPacks = await runPackQuery
                    .Include(rp => rp.Files)
                        .ThenInclude(rpf => rpf.File)
                    .OrderByDescending(rp => rp.CreatedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                // Convert to RunPackFileInfo format
                var runPackFileInfos = runPacks.Select(rp => new RunPackFileInfo
                {
                    RunId = rp.Id, // Using RunPack ID as RunId
                    ProjectId = rp.ProjectId,
                    ProjectPath = $"{rp.ProjectId}/{rp.Id}/generated-files",
                    FileCount = rp.FilesCount,
                    CreatedAt = rp.CreatedAt,
                    Environment = ExtractEnvironmentFromFiles(rp.Files) ?? "unknown",
                    GeneratedFiles = rp.Files.Select(f => f.File?.FileName ?? "").Where(name => !string.IsNullOrEmpty(name)).ToList()
                }).ToList();

                var result = new RunPackFileListResult
                {
                    Files = runPackFileInfos,
                    TotalCount = totalCount,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                _logger.LogInformation("✓ Retrieved {Count} run packs from database", runPackFileInfos.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving RunPack files from database");
                throw;
            }
        }



        // Overload to get run pack files for a specific RunPackId
        public async Task<RunPackFileListResult> GetRunPackFilesAsync(
            Guid runPackId,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== Getting RunPack Files for RunPackId from Database ===");
            _logger.LogInformation("RunPackId: {RunPackId}, Page: {Page}, PageSize: {PageSize}", runPackId, page, pageSize);

            try
            {
                var runPack = await _runPackRepository.Query()
                    .Include(rp => rp.Files)
                        .ThenInclude(rpf => rpf.File)
                    .FirstOrDefaultAsync(rp => rp.Id == runPackId, cancellationToken);

                if (runPack == null)
                {
                    _logger.LogWarning("RunPack not found: {RunPackId}", runPackId);
                    return new RunPackFileListResult
                    {
                        Files = new List<RunPackFileInfo>(),
                        TotalCount = 0,
                        CurrentPage = page,
                        PageSize = pageSize
                    };
                }

                var runPackFileInfo = new RunPackFileInfo
                {
                    RunId = runPack.Id,
                    ProjectId = runPack.ProjectId,
                    ProjectPath = $"{runPack.ProjectId}/{runPack.Id}/generated-files",
                    FileCount = runPack.FilesCount,
                    CreatedAt = runPack.CreatedAt,
                    Environment = ExtractEnvironmentFromFiles(runPack.Files) ?? "unknown",
                    GeneratedFiles = runPack.Files.Select(f => f.File?.FileName ?? "").Where(name => !string.IsNullOrEmpty(name)).ToList()
                };

                var result = new RunPackFileListResult
                {
                    Files = new[] { runPackFileInfo },
                    TotalCount = 1,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                _logger.LogInformation("✓ Retrieved run pack info for RunPackId: {RunPackId}", runPackId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving RunPack files for RunPackId: {RunPackId}", runPackId);
                throw;
            }
        }

        public async Task<byte[]?> DownloadRunPackFileAsync(
            Guid runPackId, // Changed parameter name to be clearer
            string? specificFile = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== Downloading RunPack File from Database ===");
            _logger.LogInformation("RunPackId: {RunPackId}, SpecificFile: {SpecificFile}", runPackId, specificFile);

            try
            {
                // Get run pack with files from database
                var runPack = await _runPackRepository.Query()
                    .Include(rp => rp.Files)
                        .ThenInclude(rpf => rpf.File)
                    .FirstOrDefaultAsync(rp => rp.Id == runPackId, cancellationToken);

                if (runPack == null)
                {
                    _logger.LogWarning("RunPack not found: {RunPackId}", runPackId);
                    return null;
                }

                if (!string.IsNullOrEmpty(specificFile))
                {
                    // Download specific file
                    var file = runPack.Files.FirstOrDefault(f => f.File?.FileName == specificFile)?.File;
                    if (file == null)
                    {
                        _logger.LogWarning("Specific file not found: {SpecificFile} in RunPack: {RunPackId}", specificFile, runPackId);
                        return null;
                    }

                    _logger.LogInformation("Downloading specific file: {FileName} from storage path: {StoragePath}",
                        file.FileName, file.StoragePath);

                    var normalizedPath = NormalizeStoragePath(file.StoragePath, file.ContainerName);
                    using var fileStream = await _fileStorageService.DownloadAsync(file.ContainerName, normalizedPath, cancellationToken);
                    using var memoryStream = new MemoryStream();
                    await fileStream.CopyToAsync(memoryStream, cancellationToken);
                    return memoryStream.ToArray();
                }
                else
                {
                    // Download all files as ZIP
                    _logger.LogInformation("Creating ZIP from {FileCount} files", runPack.Files.Count);
                    using var zipStream = new MemoryStream();
                    using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var runPackFile in runPack.Files)
                        {
                            var file = runPackFile.File;
                            if (file == null) continue;

                            try
                            {
                                var normalizedPath = NormalizeStoragePath(file.StoragePath, file.ContainerName);
                                using var fileStream = await _fileStorageService.DownloadAsync(file.ContainerName, normalizedPath, cancellationToken);
                                var entry = zipArchive.CreateEntry(file.FileName);
                                using var entryStream = entry.Open();
                                await fileStream.CopyToAsync(entryStream, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "File not found in storage: {StoragePath}", file.StoragePath);
                            }
                        }
                    }

                    _logger.LogInformation("✓ ZIP created successfully with {Size} bytes", zipStream.Length);
                    return zipStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error downloading run pack file. RunPackId: {RunPackId}, SpecificFile: {SpecificFile}", runPackId, specificFile);
                return null;
            }
        }

        public async Task UpdateRunPackFileAsync(
            Guid runPackId, // Changed parameter name to be clearer
            string filePath,
            string content,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== Updating RunPack File ===");
            _logger.LogInformation("RunPackId: {RunPackId}, FilePath: {FilePath}", runPackId, filePath);

            try
            {
                // Get run pack with files from database
                var runPack = await _runPackRepository.Query()
                    .Include(rp => rp.Files)
                        .ThenInclude(rpf => rpf.File)
                    .FirstOrDefaultAsync(rp => rp.Id == runPackId, cancellationToken);

                if (runPack == null)
                {
                    throw new FileNotFoundException($"RunPack not found: {runPackId}");
                }

                // Find the file in the run pack
                var runPackFile = runPack.Files.FirstOrDefault(f => f.File?.FileName == Path.GetFileName(filePath));
                if (runPackFile?.File == null)
                {
                    throw new FileNotFoundException($"File not found in RunPack: {filePath}");
                }

                var file = runPackFile.File;
                var contentType = GetContentType(filePath);

                // Upload the updated content to storage
                using var contentStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
                var normalizedPath = NormalizeStoragePath(file.StoragePath, file.ContainerName);
                await _fileStorageService.UploadAsync(file.ContainerName, normalizedPath, contentStream, contentType, false, cancellationToken);

                _logger.LogInformation("✓ File updated successfully: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating RunPack file: {RunPackId}, FilePath: {FilePath}", runPackId, filePath);
                throw;
            }
        }

        public async Task DeleteRunPackFileAsync(
            Guid runPackId, // Changed parameter name to be clearer
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== Deleting RunPack ===");
            _logger.LogInformation("RunPackId: {RunPackId}", runPackId);

            try
            {
                // Get run pack with files from database
                var runPack = await _runPackRepository.Query()
                    .Include(rp => rp.Files)
                        .ThenInclude(rpf => rpf.File)
                    .FirstOrDefaultAsync(rp => rp.Id == runPackId, cancellationToken);

                if (runPack == null)
                {
                    _logger.LogWarning("RunPack not found: {RunPackId}", runPackId);
                    return;
                }

                // Delete all individual files from storage
                foreach (var runPackFile in runPack.Files)
                {
                    var file = runPackFile.File;
                    if (file == null) continue;

                    try
                    {
                        var normalizedPath = NormalizeStoragePath(file.StoragePath, file.ContainerName);
                        await _fileStorageService.DeleteAsync(file.ContainerName, normalizedPath, cancellationToken);
                        _logger.LogDebug("Deleted file from storage: {StoragePath}", file.StoragePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete file from storage: {StoragePath}", file.StoragePath);
                        // Continue deleting other files even if one fails
                    }
                }

                // Delete the run pack from database (this should cascade delete the files)
                await _runPackRepository.DeleteAsync(runPackId, cancellationToken);

                _logger.LogInformation("✓ RunPack deleted successfully: {RunPackId}", runPackId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting RunPack: {RunPackId}", runPackId);
                throw;
            }
        }

        public async Task<bool> CheckRunPackFilesExistAsync(
            Guid runPackId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== Checking if RunPack Files Exist ===");
            _logger.LogInformation("RunPackId: {RunPackId}", runPackId);

            try
            {
                var runPack = await _runPackRepository.Query()
                    .Include(rp => rp.Files)
                        .ThenInclude(rpf => rpf.File)
                    .FirstOrDefaultAsync(rp => rp.Id == runPackId, cancellationToken);

                if (runPack == null)
                {
                    _logger.LogInformation("RunPack not found: {RunPackId}", runPackId);
                    return false;
                }

                var hasFiles = runPack.Files.Any(rpf => rpf.File != null);
                _logger.LogInformation("RunPack {RunPackId} has files: {HasFiles} (FileCount: {FileCount})", 
                    runPackId, hasFiles, runPack.Files.Count);

                return hasFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking RunPack files existence for RunPackId: {RunPackId}", runPackId);
                return false; // Return false on error to be safe
            }
        }

        private static string? ExtractEnvironmentFromFiles(IReadOnlyList<RunPackFile> files)
        {
            // Try to extract environment from the first file's tags
            var firstFile = files.FirstOrDefault()?.File;
            if (firstFile?.Tags == null) return null;

            var match = System.Text.RegularExpressions.Regex.Match(firstFile.Tags, @"env:([^,]+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        // Normalize storage path to remove scheme or container prefix if present (e.g., "gs://container/..." or "container/...")
        private static string NormalizeStoragePath(string storagePath, string containerName)
        {
            if (string.IsNullOrEmpty(storagePath)) return storagePath ?? string.Empty;

            // Trim whitespace and leading slashes
            var path = storagePath.Trim();
            while (path.StartsWith("/")) path = path[1..];

            // If path starts with gs://, remove scheme
            if (path.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
            {
                // gs://containerName/whatever
                var withoutScheme = path[5..];
                // If it starts with containerName/, strip it
                if (withoutScheme.StartsWith(containerName + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return withoutScheme[(containerName.Length + 1)..];
                }

                // If it starts with containerName (no slash), strip it
                if (withoutScheme.Equals(containerName, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }

                return withoutScheme;
            }

            // If path starts with containerName/, strip it
            if (path.StartsWith(containerName + "/", StringComparison.OrdinalIgnoreCase))
            {
                return path[(containerName.Length + 1)..];
            }

            // If path equals containerName, return empty
            if (path.Equals(containerName, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            return path;
        }

    }
}
