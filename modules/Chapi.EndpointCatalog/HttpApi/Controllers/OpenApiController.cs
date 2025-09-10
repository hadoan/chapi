using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Chapi.EndpointCatalog.Domain;
using Chapi.ApiSpecs.Domain;

namespace Chapi.EndpointCatalog.HttpApi.Controllers;

[Route("api/openapi")]
[Authorize]
public class OpenApiController : ControllerBase
{
    private readonly IApiEndpointRepository _endpoints;
    private readonly IApiSpecRepository _specs;

    public OpenApiController(IApiEndpointRepository endpoints, IApiSpecRepository specs)
    {
        _endpoints = endpoints;
        _specs = specs;
    }

    [HttpDelete("{specId:guid}")]
    public async Task<IActionResult> Delete(Guid specId)
    {
        var spec = await _specs.GetByIdAsync(specId);
        if (spec == null) return NotFound();

        // delete endpoints created from this spec
        await _endpoints.DeleteBySpecIdAsync(specId);

        // delete spec
        await _specs.DeleteAsync(specId);

        return NoContent();
    }
}
