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


        public record GenerateRequest(Guid ProjectId, ChapiCard Card, string UserQuery, string Env = "local");

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] GenerateRequest body)
        {
            // 1) parse paths + slug from card
            var root = (JsonElement)JsonSerializer.SerializeToElement(body.Card);
            var paths = new List<string>();
            foreach (var f in root.GetProperty("files").EnumerateArray())
            {
                var p = f.GetProperty("path").GetString();
                if (!string.IsNullOrWhiteSpace(p)) paths.Add(p);
            }

            // 2) pull endpoints for project (no tags needed)
            var eps = await _repo.ListByProjectAsync(body.ProjectId, tag: null, search: null);

            // 3) ALLOWED_OPS + HINTS
            var allowedOps = Helpers.BuildAllowedOps(eps);
            var hints = Helpers.BuildHints(eps);

            // 4) LLM selects relevant endpoints
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

            // 5) build endpoints_context from picks
            var endpointsContext = Helpers.BuildEndpointsContextFromPicks(sel?.Picks);
            if (string.IsNullOrWhiteSpace(endpointsContext))
                return BadRequest("No matching endpoints. Import OpenAPI or broaden request.");

            // 6) generate ZIP (tool-calling adds files)
            var zip = await _runpack.GenerateZipAsync(paths, endpointsContext, body.Env);
            return File(zip, "application/zip", "chapi-run-pack.zip");
        }
    }
}
