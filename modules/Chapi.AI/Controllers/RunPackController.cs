using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Chapi.AI.Services;
using Chapi.AI.Dto;
using System.IO;

namespace Chapi.AI.Controllers
{
    [ApiController]
    [Route("api/run-pack")]
    public sealed class RunPackController : ControllerBase
    {
        private readonly IRunPackGenerationService _generationService;
        private readonly IRunPackFileService _fileService;
        private readonly ILogger<RunPackController> _logger;

        public RunPackController(
            IRunPackGenerationService generationService,
            IRunPackFileService fileService,
            ILogger<RunPackController> logger)
        {
            _generationService = generationService;
            _fileService = fileService;
            _logger = logger;
        }

        public record UpdateFileRequest(string FilePath, string Content);

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] GenerateRunPackRequest request)
        {
            try
            {
             
                var result = await _generationService.GenerateRunPackAsync(request);

                // Add metadata headers if file was saved and RunPack was created
                if (result.SavedFileId.HasValue)
                {
                    Response.Headers["X-File-Id"] = result.SavedFileId.Value.ToString();
                    if (!string.IsNullOrEmpty(result.StoragePath))
                        Response.Headers["X-Storage-Path"] = result.StoragePath;
                }

                // Add RunPack ID header for frontend to use
                if (result.RunPackId.HasValue)
                {
                    Response.Headers["X-RunPack-Id"] = result.RunPackId.Value.ToString();
                }

                return File(result.ZipData, "application/zip", result.FileName);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid operation during RunPack generation: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during RunPack generation");
                return StatusCode(500, new { error = "Internal server error during RunPack generation" });
            }
        }

        [HttpGet("files")]
        public async Task<IActionResult> GetRunPackFiles(
            [FromQuery] Guid? projectId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _fileService.GetRunPackFilesAsync(projectId, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving RunPack files");
                return StatusCode(500, new { error = "Failed to retrieve RunPack files" });
            }
        }

        [HttpGet("runs/{runPackId}")]
        public async Task<IActionResult> DownloadRunPack(Guid runPackId, [FromQuery] string? file = null)
        {
            try
            {
                var fileContent = await _fileService.DownloadRunPackFileAsync(runPackId, file);
                if (fileContent == null)
                {
                    return NotFound(new { error = "RunPack not found" });
                }

                var fileName = string.IsNullOrEmpty(file) ? $"runpack-{runPackId}.zip" : file;
                var contentType = string.IsNullOrEmpty(file) ? "application/zip" : "application/octet-stream";

                return File(fileContent, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading RunPack: {RunPackId}, File: {File}", runPackId, file);
                return StatusCode(500, new { error = "Failed to download RunPack" });
            }
        }

        [HttpPut("runs/{runPackId}/files")]
        public async Task<IActionResult> UpdateRunPackFile(Guid runPackId, [FromBody] UpdateFileRequest request)
        {
            try
            {
                await _fileService.UpdateRunPackFileAsync(runPackId, request.FilePath, request.Content);
                return Ok(new { message = "File updated successfully" });
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { error = "RunPack or file not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating RunPack file: {RunPackId}, File: {FilePath}", runPackId, request.FilePath);
                return StatusCode(500, new { error = "Failed to update file" });
            }
        }

        [HttpDelete("runs/{runPackId}")]
        public async Task<IActionResult> DeleteRunPack(Guid runPackId)
        {
            try
            {
                await _fileService.DeleteRunPackFileAsync(runPackId);
                return Ok(new { message = "RunPack deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting RunPack: {RunPackId}", runPackId);
                return StatusCode(500, new { error = "Failed to delete RunPack" });
            }
        }
    }
}
