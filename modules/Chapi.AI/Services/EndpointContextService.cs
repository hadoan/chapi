using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Chapi.EndpointCatalog.Application;

namespace Chapi.AI.Services
{
    public class EndpointContextService : IEndpointContextService
    {
        private readonly IEndpointAppService _endpointAppService;
        private readonly ILogger<EndpointContextService> _logger;

        public EndpointContextService(IEndpointAppService endpointAppService, ILogger<EndpointContextService> logger)
        {
            _endpointAppService = endpointAppService;
            _logger = logger;
        }

        public async Task<string> BuildContextAsync(Guid projectId)
        {
            var eps = new List<EndpointBriefDto>();
            try
            {
                if (projectId != Guid.Empty)
                {
                    eps = await _endpointAppService.ListAsync(projectId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load endpoints for project {ProjectId}", projectId);
            }

            return BuildEndpointContext(eps);
        }

        private static string BuildEndpointContext(List<EndpointBriefDto> eps)
        {
            var sb = new StringBuilder();
            
            foreach (var e in eps)
            {
                var auth = (e.Tags is not null && e.Tags.Length > 0) ? string.Join("/", e.Tags) : "none";
                sb.AppendLine($"- {e.Method} {e.Path}");
                if (!string.IsNullOrWhiteSpace(e.Summary)) sb.AppendLine($"  summary: {e.Summary}");
                sb.AppendLine($"  auth: {auth}; request: none");
            }
            sb.AppendLine();
            sb.AppendLine("## Requirements");
            sb.AppendLine("- Generate smoke tests that cover auth + CRUD paths + simple edge validations.");
            sb.AppendLine("- Prefer application/json; keep 2-3 assertions per endpoint (status + key fields).");
            return sb.ToString();
        }
    }
}
