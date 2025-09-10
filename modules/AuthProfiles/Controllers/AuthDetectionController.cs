using AuthProfiles.Application.Dtos;
using AuthProfiles.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthProfiles.Controllers;

[ApiController]
[Route("api/authprofiles")]
public sealed class AuthDetectionController : ControllerBase
{
    private readonly IAuthDetectionService _detector;

    public AuthDetectionController(IAuthDetectionService detector) => _detector = detector;

    [HttpPost("detect")]
    public async Task<ActionResult<DetectionResponse>> Detect(
        [FromBody] DetectTokenRequest request,
        CancellationToken ct)
    {
        var results = await _detector.DetectAsync(request, ct).ConfigureAwait(false);
        var best = results.OrderByDescending(c => c.Confidence).FirstOrDefault();

        var resp = new DetectionResponse(
            results,
            best is null ? null : new SimpleDetection(best.Endpoint, best.Source, best.Confidence)
        );
        return Ok(resp);
    }
}
