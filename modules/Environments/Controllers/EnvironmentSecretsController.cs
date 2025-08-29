using Microsoft.AspNetCore.Mvc;
using Environments.Application;
using Environments.Application.Dtos;

namespace Environments.Controllers;

[ApiController]
[Route("api/environments/{environmentId:guid}/secrets")]
public class EnvironmentSecretsController : ControllerBase
{
    private readonly ISecretAppService _service;

    public EnvironmentSecretsController(ISecretAppService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult<EnvironmentDto>> Set(Guid environmentId, SetSecretRequest request, CancellationToken ct)
        => Ok(await _service.SetSecretAsync(environmentId, request, ct));

    [HttpPost("rotate")]
    public async Task<ActionResult<EnvironmentDto>> Rotate(Guid environmentId, RotateSecretRequest request, CancellationToken ct)
        => Ok(await _service.RotateSecretAsync(environmentId, request, ct));
}
