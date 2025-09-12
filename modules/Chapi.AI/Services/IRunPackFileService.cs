using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chapi.AI.Services
{
    public interface IRunPackFileService
    {
        Task<RunPackFileResult> SaveRunPackAsync(
            byte[] zipData,
            Guid projectId,
            string environment,
            Guid runPackId, 
            CancellationToken cancellationToken = default);

        Task<RunPackFileListResult> GetRunPackFilesByProjectAsync(
            Guid? projectId = null,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default);

        // Overload to get run pack files by a specific RunPackId
        Task<RunPackFileListResult> GetRunPackFilesAsync(
            Guid runPackId,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default);

        Task<byte[]?> DownloadRunPackFileAsync(
            Guid runPackId,
            string? specificFile = null,
            CancellationToken cancellationToken = default);

        Task UpdateRunPackFileAsync(
            Guid runPackId,
            string filePath,
            string content,
            CancellationToken cancellationToken = default);

        Task DeleteRunPackFileAsync(
            Guid runPackId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a RunPack has associated files
        /// </summary>
        Task<bool> CheckRunPackFilesExistAsync(
            Guid runPackId,
            CancellationToken cancellationToken = default);
    }
}
