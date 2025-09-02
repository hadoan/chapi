using Chapi.EndpointCatalog.Domain;
using Chapi.AI.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chapi.AI.Services
{
    public interface IEndpointAnalysisService
    {
        Task<EndpointAnalysisResult> AnalyzeEndpointsAsync(
            Guid projectId,
            Dictionary<string, string> fileRoles);

        string? ValidateAuthFiles(Dictionary<string, string> fileRoles, List<ApiEndpoint> filteredEndpoints);
        string DetectRole(string path);
        bool IsIgnorable(string path);
        string DerivedAuth(ApiEndpoint endpoint);
    }

    public class EndpointAnalysisResult
    {
        public List<ApiEndpoint> FilteredEndpoints { get; set; } = new();
        public Dictionary<string, string> Contexts { get; set; } = new();
        public string AllowedOps { get; set; } = "";
        public string Hints { get; set; } = "";
        public List<ApiEndpoint> AuthEndpoints { get; set; } = new();
        public List<ApiEndpoint> PublicCandidates { get; set; } = new();
    }

    public class EndpointAnalysisService : IEndpointAnalysisService
    {
        private readonly IApiEndpointRepository _repository;
        private readonly ILogger<EndpointAnalysisService> _logger;

        public EndpointAnalysisService(
            IApiEndpointRepository repository,
            ILogger<EndpointAnalysisService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<EndpointAnalysisResult> AnalyzeEndpointsAsync(
            Guid projectId,
            Dictionary<string, string> fileRoles)
        {
            _logger.LogInformation("=== Starting Endpoint Analysis ===");
            _logger.LogInformation("ProjectId: {ProjectId}, FileRoles: {FileRoles}",
                projectId, string.Join(", ", fileRoles.Select(kvp => $"{kvp.Key}:{kvp.Value}")));

            // 1) Pull all endpoints for project and filter out ignorable ones
            var allEndpoints = await _repository.ListByProjectAsync(projectId, tag: null, search: null);
            _logger.LogInformation("Retrieved {TotalEndpoints} total endpoints from repository", allEndpoints.Count());

            var filteredEndpoints = allEndpoints.Where(e => !IsIgnorable(e.Path)).ToList();
            _logger.LogInformation("After filtering: {FilteredCount} endpoints remain (removed {RemovedCount} health/root/metrics)",
                filteredEndpoints.Count, allEndpoints.Count() - filteredEndpoints.Count);

            LogEndpointSample(filteredEndpoints);

            // 2) Validate AUTH files have suitable endpoints
            var validationError = ValidateAuthFiles(fileRoles, filteredEndpoints);
            if (validationError != null)
            {
                _logger.LogWarning("AUTH file validation failed: {ValidationError}", validationError);
                throw new InvalidOperationException(validationError);
            }

            // 3) Build role-specific contexts using derived auth
            var authEndpoints = filteredEndpoints.Where(e => DerivedAuth(e) != "none").ToList();
            var publicCandidates = filteredEndpoints.Where(e => !IsRoot(e)).ToList();

            _logger.LogInformation("Auth analysis: {AuthEndpointsCount} protected endpoints, {PublicCount} public candidates",
                authEndpoints.Count, publicCandidates.Count);

            LogAuthAnalysis(authEndpoints);

            var contexts = BuildRoleContexts(authEndpoints, publicCandidates, filteredEndpoints);

            // 4) Build ALLOWED_OPS and HINTS with filtered endpoints
            var allowedOps = string.Join("\n", filteredEndpoints
                .OrderBy(e => e.Path).ThenBy(e => e.Method)
                .Select(e => $"{e.Method} {e.Path}"));
            var hints = Helpers.BuildHintsWithAuth(filteredEndpoints, DerivedAuth);

            return new EndpointAnalysisResult
            {
                FilteredEndpoints = filteredEndpoints,
                Contexts = contexts,
                AllowedOps = allowedOps,
                Hints = hints,
                AuthEndpoints = authEndpoints,
                PublicCandidates = publicCandidates
            };
        }

        public string DetectRole(string path)
        {
            var p = path.ToLowerInvariant();
            if (p.EndsWith("/auth.json")) return "AUTH";
            if (p.EndsWith("/crud.json")) return "CRUD";
            return "SMOKE";
        }

        public bool IsIgnorable(string path) =>
            path is "/" or "/health" or "/ping" ||
            path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/health", StringComparison.OrdinalIgnoreCase);

        public string DerivedAuth(ApiEndpoint endpoint)
        {
            var declared = EndpointIntrospection.ExtractAuth(endpoint);
            if (declared != "none") return declared;

            // Heuristic: assume write operations require auth if not explicitly declared
            bool DefaultWriteRequiresAuth = true;
            if (DefaultWriteRequiresAuth && new[] { "POST", "PUT", "PATCH", "DELETE" }.Contains(endpoint.Method))
                return "bearer";
            return "none";
        }

        public string? ValidateAuthFiles(Dictionary<string, string> fileRoles, List<ApiEndpoint> filteredEndpoints)
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

        private void LogEndpointSample(List<ApiEndpoint> filteredEndpoints)
        {
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
        }

        private void LogAuthAnalysis(List<ApiEndpoint> authEndpoints)
        {
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
        }

        private Dictionary<string, string> BuildRoleContexts(
            List<ApiEndpoint> authEndpoints,
            List<ApiEndpoint> publicCandidates,
            List<ApiEndpoint> filteredEndpoints)
        {
            return new Dictionary<string, string>
            {
                ["AUTH"] = authEndpoints.Any()
                    ? CompactEndpoints(authEndpoints)
                    : CompactEndpoints(publicCandidates.Where(e => new[] { "POST", "PUT", "PATCH", "DELETE" }.Contains(e.Method)).Take(5)),
                ["SMOKE"] = CompactEndpoints(filteredEndpoints.Take(10)),
                ["CRUD"] = CompactEndpoints(filteredEndpoints.Where(e => new[] { "POST", "PUT", "PATCH", "DELETE" }.Contains(e.Method)).Take(10))
            };
        }

        private static string CompactEndpoints(IEnumerable<ApiEndpoint> endpoints) =>
            string.Join("\n", endpoints
                .OrderBy(e => e.Path).ThenBy(e => e.Method)
                .Select(e => $"- {e.Method,-4} {e.Path,-28} | auth:{EndpointIntrospection.ExtractAuth(e),-6} | req:{EndpointIntrospection.ExtractReq(e),-16} | {EndpointIntrospection.ExtractSuccess(e)}"));

        private static bool IsRoot(ApiEndpoint endpoint) =>
            endpoint.Path == "/" || endpoint.Path == "" || endpoint.Path == "/health" || endpoint.Path == "/ping";
    }
}
