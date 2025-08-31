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

        private static string ValidateAuthFiles(Dictionary<string, string> fileRoles, List<ApiEndpoint> filteredEndpoints)
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
            // 1) parse paths + slug from card AND detect file roles
            var root = (JsonElement)JsonSerializer.SerializeToElement(body.Card);
            var paths = new List<string>();
            foreach (var f in root.GetProperty("files").EnumerateArray())
            {
                var p = f.GetProperty("path").GetString();
                if (!string.IsNullOrWhiteSpace(p)) paths.Add(p);
            }

            // Detect roles from file paths
            var fileRoles = body.Card.Files.ToDictionary(f => f.Path, f => DetectRole(f.Path));
            _logger.LogDebug("File roles detected: {FileRoles}", string.Join(", ", fileRoles.Select(kvp => $"{kvp.Key}:{kvp.Value}")));

            // 2) pull all endpoints for project and filter out ignorable ones
            var all = await _repo.ListByProjectAsync(body.ProjectId, tag: null, search: null);
            var filteredEndpoints = all.Where(e => !IsIgnorable(e.Path)).ToList();

            _logger.LogDebug("Filtered {FilteredCount} endpoints from {TotalCount} (removed health/root/metrics)", 
                filteredEndpoints.Count, all.Count());

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
            var sel = await _selector.SelectAsync(body.UserQuery, allowedOps, hints);
            
            _logger.LogDebug("SelectAsync result: ServiceSlug={ServiceSlug}, PicksCount={PicksCount}", 
                sel?.ServiceSlug, sel?.Picks?.Count ?? 0);
            
            if (sel?.Picks != null)
            {
                foreach (var pick in sel.Picks)
                {
                    _logger.LogDebug("Pick: {Method} {Path} | auth:{Auth} | req:{Req} | success:{Success}", 
                        pick.Method, pick.Path, pick.Auth, pick.Req, pick.Success);
                }
            }
            else
            {
                _logger.LogWarning("SelectAsync returned null or picks is null");
            }

            // 6) build endpoints_context from picks (backward compatibility)
            var endpointsContext = Helpers.BuildEndpointsContextFromPicks(sel?.Picks);
            if (string.IsNullOrWhiteSpace(endpointsContext))
                return BadRequest("No matching endpoints. Import OpenAPI or broaden request.");

            // 7) Build role-specific contexts from selector picks
            var picks = sel.Picks ?? new List<EndpointSelectorService.Pick>();
            
            // AUTH: protected first; fallback to any write op
            var authPicks = picks.Where(p => p.Auth != "none").ToList();
            if (!authPicks.Any())
                authPicks = picks.Where(p => p.Method is "POST" or "PUT" or "PATCH" or "DELETE").ToList();

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

            _logger.LogDebug("Role contexts from picks - AUTH: {AuthPicksCount}, SMOKE: {SmokePicksCount}, CRUD: {CrudPicksCount}", 
                authPicks.Count, smokePicks.Count, crudPicks.Count);

            // 8) generate ZIP with role contexts and file roles
            var zip = await _runpack.GenerateZipAsync(paths, endpointsContext, body.Env, fileRoles, roleContextsFromPicks);
            return File(zip, "application/zip", "chapi-run-pack.zip");
        }
    }
}
