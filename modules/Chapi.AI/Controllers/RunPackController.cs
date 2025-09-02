using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Chapi.AI.Services;
using System.Collections.Generic;
using Chapi.AI.Utilities;
using Chapi.EndpointCatalog.Domain;
using Chapi.AI.Dto;
using System.Linq;

namespace Chapi.AI.Controllers
{
    [ApiController]
    [Route("api/run-pack")]
    public sealed class RunPackController : ControllerBase
    {
        private readonly RunPackService _runpack;
        private readonly IApiEndpointRepository _repo;
        private readonly EndpointSelectorService _selector;
        private readonly ILogger<RunPackController> _logger;

        public RunPackController(RunPackService runpack, IApiEndpointRepository repo, EndpointSelectorService selector, ILogger<RunPackController> logger)
        {
            _runpack = runpack;
            _repo = repo;
            _selector = selector;
            _logger = logger;
        }

        private static string DetectRole(string path)
        {
            var p = path.ToLowerInvariant();
            if (p.EndsWith("/auth.json")) return "AUTH";
            if (p.EndsWith("/crud.json")) return "CRUD";
            return "SMOKE";
        }

        private static bool IsIgnorable(string path) =>
            path is "/" or "/health" or "/ping" ||
            path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/health", StringComparison.OrdinalIgnoreCase);

        private static string AuthOf(ApiEndpoint e) => EndpointIntrospection.ExtractAuth(e);
        private static bool IsProtected(ApiEndpoint e) => AuthOf(e) != "none";
        private static bool IsRoot(ApiEndpoint e) => e.Path == "/" || e.Path == "" || e.Path == "/health" || e.Path == "/ping";

        private static string DerivedAuth(ApiEndpoint e)
        {
            var declared = EndpointIntrospection.ExtractAuth(e);
            if (declared != "none") return declared;
            
            // Heuristic: assume write operations require auth if not explicitly declared
            bool DefaultWriteRequiresAuth = true;
            if (DefaultWriteRequiresAuth && new[] {"POST", "PUT", "PATCH", "DELETE"}.Contains(e.Method))
                return "bearer";
            return "none";
        }

        private static string Compact(IEnumerable<ApiEndpoint> eps) => string.Join("\n", eps
            .OrderBy(e => e.Path).ThenBy(e => e.Method)
            .Select(e => $"- {e.Method,-4} {e.Path,-28} | auth:{DerivedAuth(e),-6} | req:{EndpointIntrospection.ExtractReq(e),-16} | {EndpointIntrospection.ExtractSuccess(e)}"));

        private static string CompactFromPicks(IEnumerable<EndpointSelectorService.Pick> picks) => 
            string.Join("\n", picks.Select(p =>
                $"- {p.Method,-4} {p.Path,-28} | auth:{p.Auth,-6} | req:{p.Req,-16} | {p.Success}"));

        private static string? ValidateAuthFiles(Dictionary<string, string> fileRoles, List<ApiEndpoint> filteredEndpoints)
        {
            var authFiles = fileRoles.Where(kvp => kvp.Value == "AUTH").ToList();
            if (!authFiles.Any()) return null; // No AUTH files to validate

            var protectedEndpoints = filteredEndpoints.Where(e => DerivedAuth(e) != "none" && !IsRoot(e)).ToList();
            
            if (!protectedEndpoints.Any())
            {
                return $"Cannot generate AUTH tests: No protected endpoints found. " +
                       $"AUTH files require endpoints with authentication (not 'none') and not root/health paths. " +
                       $"Available endpoints: {string.Join(", ", filteredEndpoints.Take(5).Select(e => $"{e.Method} {e.Path}"))}";
            }

            return null; // Validation passed
        }

        public record GenerateRequest(Guid ProjectId, ChapiCard Card, string UserQuery, string Env = "local");

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] GenerateRequest body)
        {
            _logger.LogInformation("=== RunPack Generation Started ===");
            _logger.LogInformation("ProjectId: {ProjectId}, UserQuery: {UserQuery}, Env: {Env}", 
                body.ProjectId, body.UserQuery, body.Env);

            // 1) parse paths + slug from card AND detect file roles
            var root = (JsonElement)JsonSerializer.SerializeToElement(body.Card);
            var paths = new List<string>();
            foreach (var f in root.GetProperty("files").EnumerateArray())
            {
                var p = f.GetProperty("path").GetString();
                if (!string.IsNullOrWhiteSpace(p)) paths.Add(p);
            }

            _logger.LogInformation("Files to generate: {FilePaths}", string.Join(", ", paths));

            // Detect roles from file paths
            var fileRoles = body.Card.Files.ToDictionary(f => f.Path, f => DetectRole(f.Path));
            _logger.LogInformation("File roles detected: {FileRoles}", string.Join(", ", fileRoles.Select(kvp => $"{kvp.Key}:{kvp.Value}")));

            // 2) pull all endpoints for project and filter out ignorable ones
            var all = await _repo.ListByProjectAsync(body.ProjectId, tag: null, search: null);
            _logger.LogInformation("Retrieved {TotalEndpoints} total endpoints from repository", all.Count());

            var filteredEndpoints = all.Where(e => !IsIgnorable(e.Path)).ToList();
            _logger.LogInformation("After filtering: {FilteredCount} endpoints remain (removed {RemovedCount} health/root/metrics)", 
                filteredEndpoints.Count, all.Count() - filteredEndpoints.Count);

            if (filteredEndpoints.Count <= 5)
            {
                _logger.LogInformation("Available endpoints after filtering:");
                foreach (var ep in filteredEndpoints)
                {
                    _logger.LogInformation("  {Method} {Path} | auth:{Auth} | declared:{DeclaredAuth}", 
                        ep.Method, ep.Path, DerivedAuth(ep), EndpointIntrospection.ExtractAuth(ep));
                }
            }
            else
            {
                _logger.LogInformation("Sample of available endpoints (first 5):");
                foreach (var ep in filteredEndpoints.Take(5))
                {
                    _logger.LogInformation("  {Method} {Path} | auth:{Auth} | declared:{DeclaredAuth}", 
                        ep.Method, ep.Path, DerivedAuth(ep), EndpointIntrospection.ExtractAuth(ep));
                }
            }

            // Validate AUTH files have suitable endpoints
            var validationError = ValidateAuthFiles(fileRoles, filteredEndpoints);
            if (validationError != null)
            {
                _logger.LogWarning("AUTH file validation failed: {ValidationError}", validationError);
                return BadRequest(new { error = validationError });
            }

            // 3) Build role-specific contexts using derived auth
            var authEndpoints = filteredEndpoints.Where(e => DerivedAuth(e) != "none").ToList();
            var publicCandidates = filteredEndpoints.Where(e => !IsRoot(e)).ToList();
            
            _logger.LogInformation("Auth analysis: {AuthEndpointsCount} protected endpoints, {PublicCount} public candidates", 
                authEndpoints.Count, publicCandidates.Count);

            if (authEndpoints.Any())
            {
                _logger.LogInformation("Protected endpoints found:");
                foreach (var ep in authEndpoints.Take(3))
                {
                    _logger.LogInformation("  {Method} {Path} | auth:{Auth}", ep.Method, ep.Path, DerivedAuth(ep));
                }
            }
            else
            {
                _logger.LogWarning("No protected endpoints found! All endpoints appear to be public.");
            }
            
            var contexts = new Dictionary<string, string>
            {
                ["AUTH"] = authEndpoints.Any() ? Compact(authEndpoints) : Compact(publicCandidates.Where(e => new[] {"POST", "PUT", "PATCH", "DELETE"}.Contains(e.Method)).Take(5)),
                ["SMOKE"] = Compact(filteredEndpoints.Take(10)),
                ["CRUD"] = Compact(filteredEndpoints.Where(e => new[] {"POST", "PUT", "PATCH", "DELETE"}.Contains(e.Method)).Take(10))
            };

            _logger.LogDebug("Role contexts built - AUTH: {AuthCount} endpoints, SMOKE: {SmokeCount} endpoints, CRUD: {CrudCount} endpoints", 
                authEndpoints.Any() ? authEndpoints.Count : publicCandidates.Count(e => new[] {"POST", "PUT", "PATCH", "DELETE"}.Contains(e.Method)), 
                Math.Min(10, filteredEndpoints.Count), 
                Math.Min(10, filteredEndpoints.Count(e => new[] {"POST", "PUT", "PATCH", "DELETE"}.Contains(e.Method))));

            // 4) Build ALLOWED_OPS and HINTS with filtered endpoints
            var allowedOps = string.Join("\n", filteredEndpoints
                .OrderBy(e => e.Path).ThenBy(e => e.Method)
                .Select(e => $"{e.Method} {e.Path}"));
            var hints = Helpers.BuildHintsWithAuth(filteredEndpoints, DerivedAuth);

            // 5) LLM selects relevant endpoints
            _logger.LogInformation("=== Calling Endpoint Selector ===");
            _logger.LogInformation("UserQuery: {UserQuery}", body.UserQuery);
            _logger.LogInformation("AllowedOps preview: {AllowedOpsPreview}", 
                string.Join(", ", filteredEndpoints.Take(3).Select(e => $"{e.Method} {e.Path}")));

            var sel = await _selector.SelectAsync(body.UserQuery, allowedOps, hints);
            
            _logger.LogInformation("=== Endpoint Selector Results ===");
            _logger.LogInformation("ServiceSlug: {ServiceSlug}, PicksCount: {PicksCount}", 
                sel?.ServiceSlug, sel?.Picks?.Count ?? 0);
            
            if (sel?.Picks != null)
            {
                _logger.LogInformation("Endpoint selector picked {PickCount} endpoints:", sel.Picks.Count);
                foreach (var pick in sel.Picks)
                {
                    _logger.LogInformation("  ✓ {Method} {Path} | auth:{Auth} | req:{Req} | success:{Success}", 
                        pick.Method, pick.Path, pick.Auth, pick.Req, pick.Success);
                }
            }
            else
            {
                _logger.LogError("❌ Endpoint selector returned null or empty picks!");
                return BadRequest("Endpoint selector failed to find matching endpoints.");
            }

            // 6) build endpoints_context from picks (backward compatibility)
            var endpointsContext = Helpers.BuildEndpointsContextFromPicks(sel?.Picks);
            if (string.IsNullOrWhiteSpace(endpointsContext))
                return BadRequest("No matching endpoints. Import OpenAPI or broaden request.");

            // 7) Build role-specific contexts from selector picks
            var picks = sel?.Picks ?? new List<EndpointSelectorService.Pick>();
            
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
            if (fileRoles.Values.Contains("AUTH"))
            {
                _logger.LogInformation("=== AUTH Validation ===");
                if (!authPicks.Any())
                {
                    _logger.LogError("❌ AUTH file detected but no suitable AUTH picks found!");
                    _logger.LogError("Available picks: {AvailablePicks}", 
                        string.Join(", ", picks.Select(p => $"{p.Method} {p.Path} (auth:{p.Auth})")));
                    return BadRequest(new { 
                        error = "Cannot generate AUTH tests: No protected endpoints found.", 
                        details = "AUTH tests require endpoints with authentication (auth != 'none'). Available endpoints all appear to be public.",
                        suggestion = "Import OpenAPI specs with security definitions, or ensure write operations (POST/PUT/PATCH/DELETE) are available.",
                        available_endpoints = picks.Select(p => $"{p.Method} {p.Path} (auth:{p.Auth})").ToList()
                    });
                }

                // Additional validation: AUTH picks should not be root endpoints
                var badAuthPicks = authPicks.Where(p => p.Path == "/" || p.Path == "/health" || p.Path == "/ping").ToList();
                if (badAuthPicks.Any())
                {
                    _logger.LogError("❌ AUTH file has unsuitable endpoints: {BadPicks}", 
                        string.Join(", ", badAuthPicks.Select(p => $"{p.Method} {p.Path}")));
                    return BadRequest(new { 
                        error = "Cannot generate AUTH tests: Selected endpoints are not suitable for authentication testing.",
                        details = "AUTH tests cannot use root (/), health, or ping endpoints.",
                        bad_endpoints = badAuthPicks.Select(p => $"{p.Method} {p.Path}").ToList()
                    });
                }
                
                _logger.LogInformation("✓ AUTH validation passed. Using {AuthPicksCount} suitable endpoints.", authPicks.Count);
            }

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

            // 8) generate ZIP with role contexts and file roles
            _logger.LogInformation("=== Generating RunPack ZIP ===");
            var zip = await _runpack.GenerateZipAsync(paths, endpointsContext, body.Env, fileRoles, roleContextsFromPicks);
            
            _logger.LogInformation("✓ RunPack ZIP generated successfully ({ZipSize} bytes)", zip.Length);
            return File(zip, "application/zip", "chapi-run-pack.zip");
        }
    }
}
