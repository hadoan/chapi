using Microsoft.AspNetCore.Mvc;
using Chapi.EndpointCatalog.Application;

namespace Chapi.EndpointCatalog.HttpApi.Controllers;

[Route("api/projects/{projectId:guid}/endpoints")]
public class EndpointsController : Microsoft.AspNetCore.Mvc.ControllerBase
{
    private readonly IEndpointAppService _svc;
    public EndpointsController(IEndpointAppService svc) { _svc = svc; }

    [HttpGet]
    public Task<List<EndpointBriefDto>> List(Guid projectId, [FromQuery] string? tag, [FromQuery] string? q)
        => _svc.ListAsync(projectId, tag, q);

    [HttpGet("{endpointId:guid}")]
    public Task<EndpointDto> Get(Guid projectId, Guid endpointId) => _svc.GetAsync(endpointId);
}
