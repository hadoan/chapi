using ShipMvp.Domain.Files;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        Task<RunPackFileDownloadResult> DownloadRunPackFileAsync(
            Guid fileId,
            CancellationToken cancellationToken = default);

        Task DeleteRunPackFileAsync(
            Guid fileId,
            CancellationToken cancellationToken = default);
    }

    public class RunPackFileResult
    {
        public Guid FileId { get; set; }
        public string StoragePath { get; set; } = "";
        public string FileName { get; set; } = "";
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
        public Guid Id { get; set; }
        public string OriginalFileName { get; set; } = "";
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Tags { get; set; }
        public string? ProjectId { get; set; }
        public string? Environment { get; set; }
    }

    public class RunPackFileService : IRunPackFileService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IFileRepository _fileRepository;
        private readonly ILogger<RunPackFileService> _logger;

        private const string CONTAINER_NAME = "chapi-runpacks";

        public RunPackFileService(
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
            _logger.LogInformation("=== Saving RunPack to Storage ===");
            _logger.LogInformation("ProjectId: {ProjectId}, Environment: {Environment}, Size: {Size} bytes",
                projectId, environment, zipData.Length);

            try
            {
                var fileId = Guid.NewGuid();
                var fileName = $"runpack-{fileId}.zip";

                // Upload to storage
                using var zipStream = new MemoryStream(zipData);
                var storagePath = await _fileStorageService.UploadAsync(
                    CONTAINER_NAME,
                    fileName,
                    zipStream,
                    "application/zip",
                    isPublic: false,
                    cancellationToken);

                // Create file entity
                var fileEntity = new ShipMvp.Domain.Files.File(
                    fileId,
                    CONTAINER_NAME,
                    fileName,
                    $"chapi-runpack-{projectId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip",
                    "application/zip",
                    zipData.Length,
                    storagePath,
                    null, // TODO: Get current user ID from authentication context
                    isPublic: false);

                // Set tags for easier searching
                fileEntity.UpdateTags($"runpack,project:{projectId},env:{environment},generated:{DateTime.UtcNow:yyyy-MM-dd}");

                // Save to database
                var savedFile = await _fileRepository.InsertAsync(fileEntity, cancellationToken);

                _logger.LogInformation("✓ RunPack saved to storage: FileId={FileId}, StoragePath={StoragePath}",
                    fileId, storagePath);

                return new RunPackFileResult
                {
                    FileId = fileId,
                    StoragePath = storagePath,
                    FileName = fileEntity.OriginalFileName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to save RunPack to storage");
                throw;
            }
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

            try
            {
                var skip = (page - 1) * pageSize;

                // Get files from the runpack container
                var (files, totalCount) = await _fileRepository.GetPaginatedAsync(
                    skip,
                    pageSize,
                    userId: null, // TODO: Filter by current user when auth is implemented
                    containerName: CONTAINER_NAME,
                    cancellationToken);

                // Filter by project if specified
                if (projectId.HasValue)
                {
                    files = files.Where(f => f.Tags != null && f.Tags.Contains($"project:{projectId.Value}"));
                    totalCount = files.Count();
                }

                var result = new RunPackFileListResult
                {
                    Files = files.Select(f => new RunPackFileInfo
                    {
                        Id = f.Id,
                        OriginalFileName = f.OriginalFileName,
                        Size = f.Size,
                        CreatedAt = f.CreatedAt,
                        Tags = f.Tags,
                        ProjectId = ExtractProjectIdFromTags(f.Tags),
                        Environment = ExtractEnvironmentFromTags(f.Tags)
                    }),
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

        public async Task<RunPackFileDownloadResult> DownloadRunPackFileAsync(
            Guid fileId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== Downloading RunPack File ===");
            _logger.LogInformation("FileId: {FileId}", fileId);

            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
                if (file == null || file.IsDeleted)
                {
                    throw new FileNotFoundException("RunPack file not found");
                }

                // Ensure it's a runpack file
                if (file.ContainerName != CONTAINER_NAME)
                {
                    throw new InvalidOperationException("Not a RunPack file");
                }

                // Download from storage
                var fileStream = await _fileStorageService.DownloadAsync(
                    file.ContainerName,
                    file.FileName,
                    cancellationToken);

                return new RunPackFileDownloadResult
                {
                    FileStream = fileStream,
                    FileName = file.OriginalFileName,
                    MimeType = file.MimeType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error downloading RunPack file: {FileId}", fileId);
                throw;
            }
        }

        public async Task DeleteRunPackFileAsync(
            Guid fileId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== Deleting RunPack File ===");
            _logger.LogInformation("FileId: {FileId}", fileId);

            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
                if (file == null || file.IsDeleted)
                {
                    throw new FileNotFoundException("RunPack file not found");
                }

                // Ensure it's a runpack file
                if (file.ContainerName != CONTAINER_NAME)
                {
                    throw new InvalidOperationException("Not a RunPack file");
                }

                // Delete from storage
                await _fileStorageService.DeleteAsync(file.ContainerName, file.FileName, cancellationToken);

                // Soft delete from database
                await _fileRepository.DeleteAsync(fileId, cancellationToken);

                _logger.LogInformation("✓ RunPack file deleted successfully: {FileId}", fileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting RunPack file: {FileId}", fileId);
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
