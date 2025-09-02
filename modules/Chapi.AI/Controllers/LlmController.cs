using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chapi.AI.Services;
using Chapi.EndpointCatalog.Application;
using Chapi.AI.Dto;

namespace Chapi.AI.Controllers
{
    [ApiController]
    [Route("api/llm")]
    public class LlmController : ControllerBase
    {
    private readonly IApiTestGenerationService _apiTestService;
    private readonly ILogger<LlmController> _logger;
    private readonly IEndpointContextService _endpointContextService;

        public LlmController(IApiTestGenerationService apiTestService, IEndpointContextService endpointContextService, ILogger<LlmController> logger)
        {
            _apiTestService = apiTestService;
            _endpointContextService = endpointContextService;
            _logger = logger;
        }


        [HttpPost("generate")]
        public async Task<ChapiCard> Generate([FromBody] Chapi.AI.Dto.ApiTestGenerateRequest req)
        {
            _logger.LogInformation("LLM generate requested (Chapi.AI controller)");

            var endpointsContext = await _endpointContextService.BuildContextAsync(req.ProjectId, "User Service");
            var card = await _apiTestService.GenerateTestAsync(req.UserQuery, endpointsContext, req.MaxFiles, req.OpenApiJson);

            return card;
        }
        
    }


    // ...existing code...
}
