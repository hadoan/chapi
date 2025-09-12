using Chapi.AI.Dto;
using Chapi.AI.Services;
using Chapi.AI.Utilities;
using Microsoft.Extensions.Logging;
using RunPack.Application.Requests;
using RunPack.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Chapi.AI.Services
{
    public interface IRunPackGenerationService
    {
        Task<RunPackGenerationResult> GenerateRunPackAsync(GenerateRunPackRequest request);
    }

    public class GenerateRunPackRequest
    {
        public Guid ProjectId { get; set; }
        public ChapiCard Card { get; set; } = null!;
        public string UserQuery { get; set; } = "";
        public string Environment { get; set; } = "local";
        public Guid ConversationId { get; set; }
        public Guid MessageId { get; set; }


        public Guid? RunPackId { get; set; }
    }

    public class RunPackGenerationResult
    {
        public byte[] ZipData { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = "";
        public Guid? SavedFileId { get; set; }
        public string? StoragePath { get; set; }
        public Guid? RunPackId { get; set; }
        public bool FromDatabase { get; set; } = false;
    }

    public class RunPackGenerationService : IRunPackGenerationService
    {
        private readonly IEndpointAnalysisService _endpointAnalysis;
        private readonly EndpointSelectorService _endpointSelector;
        private readonly RunPackService _runPackService;
        private readonly IRunPackFileService _fileService;
        private readonly IRunPackAppService _runPackAppService;
        private readonly ILogger<RunPackGenerationService> _logger;

        public RunPackGenerationService(
            IEndpointAnalysisService endpointAnalysis,
            EndpointSelectorService endpointSelector,
            RunPackService runPackService,
            IRunPackFileService fileService,
            IRunPackAppService runPackAppService,
            ILogger<RunPackGenerationService> logger)
        {
            _endpointAnalysis = endpointAnalysis;
            _endpointSelector = endpointSelector;
            _runPackService = runPackService;
            _fileService = fileService;
            _runPackAppService = runPackAppService;
            _logger = logger;
        }

        public async Task<RunPackGenerationResult> GenerateRunPackAsync(GenerateRunPackRequest request)
        {
            _logger.LogInformation("=== RunPack Generation Started ===");
            _logger.LogInformation("ProjectId: {ProjectId}, UserQuery: {UserQuery}, Env: {Env}",
                request.ProjectId, request.UserQuery, request.Environment);
            
            var existingRunPack = await CheckForExistingRunPack(request);
            if (existingRunPack != null)
            {
                _logger.LogInformation("Found existing RunPack files for request, returning from database");
                
                // Double-check that files actually exist before attempting download
                var filesExist = await _fileService.CheckRunPackFilesExistAsync(existingRunPack.RunPackId);
                if (!filesExist)
                {
                    _logger.LogWarning("RunPack {RunPackId} was found but files don't exist, proceeding with generation", existingRunPack.RunPackId);
                }
                else
                {
                    // Return existing files from database
                    var existingZipData = await _fileService.DownloadRunPackFileAsync(existingRunPack.RunPackId);
                    if (existingZipData != null)
                    {
                        var fileName = $"runpack-{existingRunPack.RunPackId}-{request.Environment}.zip";
                        return new RunPackGenerationResult
                        {
                            ZipData = existingZipData,
                            FileName = fileName,
                            RunPackId = existingRunPack.RunPackId,
                            SavedFileId = existingRunPack.RunPackId, // Use RunPackId as SavedFileId for consistency
                            StoragePath = $"{request.ProjectId}/{existingRunPack.RunPackId}/generated-files",
                            FromDatabase = true
                        };
                    }
                    else
                    {
                        _logger.LogWarning("RunPack {RunPackId} files exist but download failed, proceeding with generation", existingRunPack.RunPackId);
                    }
                }
            }

            _logger.LogInformation("No existing files found, generating new RunPack");

            // No existing files found, proceed with generation
            return await GenerateNewRunPack(request);
        }

        private async Task<ExistingRunPackInfo?> CheckForExistingRunPack(GenerateRunPackRequest request)
        {
            try
            {
                // If a specific RunPackId is provided, check for that exact RunPack
                if (request.RunPackId.HasValue)
                {
                    _logger.LogInformation("Checking for existing RunPack by ID: {RunPackId}", request.RunPackId.Value);
                    
                    // Use the more efficient method to check if files exist
                    var hasFiles = await _fileService.CheckRunPackFilesExistAsync(request.RunPackId.Value);
                    
                    if (hasFiles)
                    {
                        // Get the RunPack details since files exist
                        var specificRunPack = await _fileService.GetRunPackFilesAsync(request.RunPackId.Value, 1, 1);
                        
                        if (specificRunPack.Files?.Any() == true)
                        {
                            var runPackInfo = specificRunPack.Files.First();
                            _logger.LogInformation("Found existing RunPack by ID: {RunPackId} with {FileCount} files", 
                                runPackInfo.RunId, runPackInfo.FileCount);
                                
                            return new ExistingRunPackInfo
                            {
                                RunPackId = runPackInfo.RunId,
                                FileCount = runPackInfo.FileCount,
                                CreatedAt = runPackInfo.CreatedAt
                            };
                        }
                    }
                    
                    _logger.LogInformation("RunPack with ID {RunPackId} not found or has no files", request.RunPackId.Value);
                    return null;
                }

                // Fallback: Get RunPacks for the project and match by environment (original logic)
                _logger.LogInformation("No specific RunPackId provided, checking by project and environment");
                var runPackFiles = await _fileService.GetRunPackFilesByProjectAsync(request.ProjectId, 1, 50);
                
                if (runPackFiles.Files?.Any() == true)
                {
                    // Look for a RunPack that matches the current request parameters
                    var matchingRunPack = runPackFiles.Files.FirstOrDefault(rp => 
                        rp.ProjectId == request.ProjectId &&
                        rp.Environment.Equals(request.Environment, StringComparison.OrdinalIgnoreCase) &&
                        rp.FileCount > 0);
                    
                    if (matchingRunPack != null)
                    {
                        _logger.LogInformation("Found existing RunPack by project/environment: {RunPackId} with {FileCount} files", 
                            matchingRunPack.RunId, matchingRunPack.FileCount);
                            
                        return new ExistingRunPackInfo
                        {
                            RunPackId = matchingRunPack.RunId,
                            FileCount = matchingRunPack.FileCount,
                            CreatedAt = matchingRunPack.CreatedAt
                        };
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking for existing RunPack, proceeding with generation");
                return null; // If we can't check, proceed with generation
            }
        }

        private async Task<RunPackGenerationResult> GenerateNewRunPack(GenerateRunPackRequest request)
        {
            // 1) Parse paths + slug from card and detect file roles
            var filePaths = ExtractFilePaths(request.Card);
            var fileRoles = DetectFileRoles(request.Card);

            _logger.LogInformation("Files to generate: {FilePaths}", string.Join(", ", filePaths));
            _logger.LogInformation("File roles detected: {FileRoles}",
                string.Join(", ", fileRoles.Select(kvp => $"{kvp.Key}:{kvp.Value}")));

            // 2) Analyze endpoints
            var analysisResult = await _endpointAnalysis.AnalyzeEndpointsAsync(request.ProjectId, fileRoles);

            // 3) Select relevant endpoints using LLM
            var selectorResult = await SelectEndpoints(request.UserQuery, analysisResult);

            // 4) Build role-specific contexts from selector picks
            var roleContextsFromPicks = BuildRoleContextsFromPicks(selectorResult, fileRoles);

            // 5) Generate ZIP
            var endpointsContext = Helpers.BuildEndpointsContextFromPicks(selectorResult?.Picks);
            if (string.IsNullOrWhiteSpace(endpointsContext))
                throw new InvalidOperationException("No matching endpoints. Import OpenAPI or broaden request.");

            var zipData = await _runPackService.GenerateZipAsync(
                filePaths, endpointsContext, request.Environment, fileRoles, roleContextsFromPicks);

            _logger.LogInformation("✓ RunPack ZIP generated successfully ({ZipSize} bytes)", zipData.Length);

            // 6) Create RunPack entity in database first
            Guid runPackId = Guid.Empty;
            try
            {
                var runPackDto = await _runPackAppService.BuildFromConversationAsync(
                    new BuildRunPackFromConversationRequest(
                        request.ProjectId,
                        request.ConversationId,
                        request.MessageId,
                        "hybrid"
                    ),
                    default);
                runPackId = runPackDto.Id;
                _logger.LogInformation("✓ RunPack entity created and linked to conversation: {RunPackId}", runPackId);
            }
            catch (Exception dbEx)
            {
                _logger.LogWarning(dbEx, "⚠️ Failed to create RunPack entity in database, proceeding without linking");
            }

            // 7) Save to storage and link files to RunPack
            var fileName = $"chapi-runpack-{request.ProjectId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";

            try
            {
                var fileResult = await _fileService.SaveRunPackAsync(zipData, request.ProjectId, request.Environment, runPackId);

                return new RunPackGenerationResult
                {
                    ZipData = zipData,
                    FileName = fileName,
                    SavedFileId = fileResult.RunId,
                    StoragePath = fileResult.ProjectPath,
                    RunPackId = runPackId,
                    FromDatabase = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to save RunPack to storage, returning ZIP data only");

                return new RunPackGenerationResult
                {
                    ZipData = zipData,
                    FileName = fileName,
                    RunPackId = runPackId,
                    FromDatabase = false
                };
            }
        }

        private class ExistingRunPackInfo
        {
            public Guid RunPackId { get; set; }
            public int FileCount { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private List<string> ExtractFilePaths(ChapiCard card)
        {
            var root = (JsonElement)JsonSerializer.SerializeToElement(card);
            var paths = new List<string>();

            foreach (var f in root.GetProperty("files").EnumerateArray())
            {
                var path = f.GetProperty("path").GetString();
                if (!string.IsNullOrWhiteSpace(path))
                    paths.Add(path);
            }

            return paths;
        }

        private Dictionary<string, string> DetectFileRoles(ChapiCard card)
        {
            return card.Files.ToDictionary(f => f.Path, f => _endpointAnalysis.DetectRole(f.Path));
        }

        private async Task<EndpointSelectorService.Result?> SelectEndpoints(
            string userQuery,
            EndpointAnalysisResult analysisResult)
        {
            _logger.LogInformation("=== Calling Endpoint Selector ===");
            _logger.LogInformation("UserQuery: {UserQuery}", userQuery);
            _logger.LogInformation("AllowedOps preview: {AllowedOpsPreview}",
                string.Join(", ", analysisResult.FilteredEndpoints.Take(3).Select(e => $"{e.Method} {e.Path}")));

            var selectorResult = await _endpointSelector.SelectAsync(userQuery, analysisResult.AllowedOps, analysisResult.Hints);

            _logger.LogInformation("=== Endpoint Selector Results ===");
            _logger.LogInformation("ServiceSlug: {ServiceSlug}, PicksCount: {PicksCount}",
                selectorResult?.ServiceSlug, selectorResult?.Picks?.Count ?? 0);

            if (selectorResult?.Picks != null)
            {
                _logger.LogInformation("Endpoint selector picked {PickCount} endpoints:", selectorResult.Picks.Count);
                foreach (var pick in selectorResult.Picks)
                {
                    _logger.LogInformation("  ✓ {Method} {Path} | auth:{Auth} | req:{Req} | success:{Success}",
                        pick.Method, pick.Path, pick.Auth, pick.Req, pick.Success);
                }
            }
            else
            {
                _logger.LogError("❌ Endpoint selector returned null or empty picks!");
                throw new InvalidOperationException("Endpoint selector failed to find matching endpoints.");
            }

            return selectorResult;
        }

        private Dictionary<string, string> BuildRoleContextsFromPicks(
            EndpointSelectorService.Result? selectorResult,
            Dictionary<string, string> fileRoles)
        {
            var picks = selectorResult?.Picks ?? new List<EndpointSelectorService.Pick>();

            _logger.LogInformation("=== Building Role-Specific Contexts ===");
            _logger.LogInformation("Building role contexts from {PicksCount} picks", picks.Count);

            foreach (var pick in picks)
            {
                _logger.LogInformation("Available pick: {Method} {Path} | auth:{Auth}", pick.Method, pick.Path, pick.Auth);
            }

            // AUTH: protected first; fallback to any write op
            var authPicks = picks.Where(p => p.Auth != "none").ToList();
            if (!authPicks.Any())
            {
                _logger.LogWarning("No picks with auth != 'none' found. Falling back to write operations.");
                authPicks = picks.Where(p => p.Method is "POST" or "PUT" or "PATCH" or "DELETE").ToList();
            }

            _logger.LogInformation("AUTH analysis: {AuthPicksCount} picks selected for AUTH context", authPicks.Count);
            foreach (var pick in authPicks)
            {
                _logger.LogInformation("  AUTH pick: {Method} {Path} | auth:{Auth}", pick.Method, pick.Path, pick.Auth);
            }

            // Validate AUTH picks before proceeding
            ValidateAuthPicks(fileRoles, authPicks, picks);

            // SMOKE: 3–5 representative
            var smokePicks = picks.Take(5).ToList();

            // CRUD: writes if present
            var crudPicks = picks.Where(p => p.Method is "POST" or "PUT" or "PATCH" or "DELETE").ToList();
            if (!crudPicks.Any()) crudPicks = picks.ToList();

            var roleContextsFromPicks = new Dictionary<string, string>
            {
                ["AUTH"] = CompactFromPicks(authPicks),
                ["SMOKE"] = CompactFromPicks(smokePicks),
                ["CRUD"] = CompactFromPicks(crudPicks)
            };

            LogFinalRoleContexts(roleContextsFromPicks, authPicks, smokePicks, crudPicks);

            return roleContextsFromPicks;
        }

        private void ValidateAuthPicks(
            Dictionary<string, string> fileRoles,
            List<EndpointSelectorService.Pick> authPicks,
            List<EndpointSelectorService.Pick> allPicks)
        {
            if (!fileRoles.Values.Contains("AUTH")) return;

            _logger.LogInformation("=== AUTH Validation ===");
            if (!authPicks.Any())
            {
                _logger.LogError("❌ AUTH file detected but no suitable AUTH picks found!");
                _logger.LogError("Available picks: {AvailablePicks}",
                    string.Join(", ", allPicks.Select(p => $"{p.Method} {p.Path} (auth:{p.Auth})")));

                throw new InvalidOperationException(
                    $"Cannot generate AUTH tests: No protected endpoints found. " +
                    $"AUTH tests require endpoints with authentication (auth != 'none'). " +
                    $"Available endpoints all appear to be public. " +
                    $"Available: {string.Join(", ", allPicks.Select(p => $"{p.Method} {p.Path} (auth:{p.Auth})"))}");
            }

            // Additional validation: AUTH picks should not be root endpoints
            var badAuthPicks = authPicks.Where(p => p.Path == "/" || p.Path == "/health" || p.Path == "/ping").ToList();
            if (badAuthPicks.Any())
            {
                _logger.LogError("❌ AUTH file has unsuitable endpoints: {BadPicks}",
                    string.Join(", ", badAuthPicks.Select(p => $"{p.Method} {p.Path}")));

                throw new InvalidOperationException(
                    $"Cannot generate AUTH tests: Selected endpoints are not suitable for authentication testing. " +
                    $"AUTH tests cannot use root (/), health, or ping endpoints. " +
                    $"Bad endpoints: {string.Join(", ", badAuthPicks.Select(p => $"{p.Method} {p.Path}"))}");
            }

            _logger.LogInformation("✓ AUTH validation passed. Using {AuthPicksCount} suitable endpoints.", authPicks.Count);
        }

        private void LogFinalRoleContexts(
            Dictionary<string, string> roleContextsFromPicks,
            List<EndpointSelectorService.Pick> authPicks,
            List<EndpointSelectorService.Pick> smokePicks,
            List<EndpointSelectorService.Pick> crudPicks)
        {
            _logger.LogInformation("=== Final Role Contexts ===");
            foreach (var kvp in roleContextsFromPicks)
            {
                _logger.LogInformation("{Role} context ({Length} chars):", kvp.Key, kvp.Value.Length);
                if (kvp.Value.Length > 0)
                {
                    var lines = kvp.Value.Split('\n').Take(2);
                    foreach (var line in lines)
                    {
                        _logger.LogInformation("  {Line}", line);
                    }
                    if (kvp.Value.Split('\n').Length > 2)
                    {
                        _logger.LogInformation("  ... ({MoreLines} more lines)", kvp.Value.Split('\n').Length - 2);
                    }
                }
                else
                {
                    _logger.LogWarning("  ⚠️  {Role} context is EMPTY!", kvp.Key);
                }
            }

            _logger.LogInformation("Role pick counts - AUTH: {AuthPicksCount}, SMOKE: {SmokePicksCount}, CRUD: {CrudPicksCount}",
                authPicks.Count, smokePicks.Count, crudPicks.Count);
        }

        private static string CompactFromPicks(IEnumerable<EndpointSelectorService.Pick> picks) =>
            string.Join("\n", picks.Select(p =>
                $"- {p.Method,-4} {p.Path,-28} | auth:{p.Auth,-6} | req:{p.Req,-16} | {p.Success}"));
    }
}
