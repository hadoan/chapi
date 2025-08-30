using Microsoft.AspNetCore.Mvc;
using Chapi.ApiSpecs.Application;

namespace Chapi.ApiSpecs.HttpApi.Controllers;

[Route("api/projects/{projectId:guid}/openapi")]
public class ApiSpecsController : Microsoft.AspNetCore.Mvc.ControllerBase
{
    private readonly IApiSpecAppService _svc;
    public ApiSpecsController(IApiSpecAppService svc) { _svc = svc; }

    [HttpPost("import")]
    public Task<ApiSpecDto> Import(Guid projectId, [FromBody] ImportOpenApiInputDto body)
        => _svc.ImportAsync(body with { ProjectId = projectId });
}
