using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Chapi.AI.Services;
using System.Collections.Generic;

namespace Chapi.AI.Controllers
{
    [ApiController]
    [Route("api/run-pack")]
    public sealed class RunPackController : ControllerBase
    {
        private readonly RunPackService _service;

        public RunPackController(RunPackService service)
        {
            _service = service;
        }

        public record GenerateRequest(object Card, string EndpointsContext, string Env = "local");

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] GenerateRequest body)
        {
            // Extract file manifest paths from the posted Card object if possible
            var manifestPaths = new List<string>();
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(body.Card));
                if (doc.RootElement.TryGetProperty("files", out var filesEl) && filesEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var f in filesEl.EnumerateArray())
                    {
                        if (f.TryGetProperty("path", out var p) && p.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            manifestPaths.Add(p.GetString() ?? string.Empty);
                        }
                    }
                }
            }
            catch
            {
                // ignore and pass empty manifest
            }

            var zip = await _service.GenerateZipAsync(manifestPaths, body.EndpointsContext, body.Env);
            return File(zip, "application/zip", "chapi-run-pack.zip");
        }
    }
}
