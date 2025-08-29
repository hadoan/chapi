using Microsoft.AspNetCore.Mvc;
using Environments.Application;
using Environments.Application.Dtos;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;

namespace Environments.Controllers;

[ApiController]
[Route("api/environments")]
public class EnvironmentsController : ControllerBase
{
    private readonly IEnvironmentAppService _service;
    private readonly IHttpClientFactory _httpFactory;

    public EnvironmentsController(IEnvironmentAppService service, IHttpClientFactory httpFactory)
    {
        _service = service;
        _httpFactory = httpFactory;
    }

    [HttpPost]
    public async Task<ActionResult<EnvironmentDto>> Create(CreateEnvironmentRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnvironmentDto>>> List([FromQuery] GetEnvironmentsQuery query, CancellationToken ct)
        => Ok(await _service.GetListAsync(query, ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<EnvironmentDto?>> GetById(Guid id, CancellationToken ct)
    {
        var e = await _service.GetByIdAsync(id, ct);
        return e == null ? NotFound() : Ok(e);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EnvironmentDto>> Update(Guid id, UpdateEnvironmentRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id}/test")]
    public async Task<IActionResult> TestConnection(Guid id, CancellationToken ct)
    {
        var env = await _service.GetByIdAsync(id, ct);
        if (env == null) return NotFound();

    var client = _httpFactory.CreateClient();
    var timeoutMs = (env.TimeoutMs > 0) ? env.TimeoutMs : 30000;
    client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);

        var req = new HttpRequestMessage(HttpMethod.Get, env.BaseUrl ?? string.Empty);
        if (env.Headers != null)
        {
            foreach (var h in env.Headers)
            {
                if (!string.IsNullOrEmpty(h.Key) && h.Value != null)
                {
                    // add header without validation to allow non-standard headers
                    req.Headers.TryAddWithoutValidation(h.Key, h.Value);
                }
            }
        }

        try
        {
            var sw = Stopwatch.StartNew();
            var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            sw.Stop();

            return Ok(new {
                ok = resp.IsSuccessStatusCode,
                status = (int)resp.StatusCode,
                reason = resp.ReasonPhrase,
                elapsedMs = sw.ElapsedMilliseconds
            });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, new { ok = false, error = "timeout" });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { ok = false, error = ex.Message });
        }
    }
}
