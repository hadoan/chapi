using Integrations.GitHub.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Integrations.GitHub.Controllers;

[ApiController]
[Route("api/integrations/github/webhook")]
public class GitHubWebhookController : ControllerBase
{
    private readonly IGitHubWebhookVerifier _verifier;
    public GitHubWebhookController(IGitHubWebhookVerifier verifier) => _verifier = verifier;

    [HttpPost]
    public Task<IActionResult> Post(CancellationToken ct)
    {
        // TODO: read payload & signature headers, verify, dispatch domain events
        return Task.FromResult<IActionResult>(Ok());
    }
}
