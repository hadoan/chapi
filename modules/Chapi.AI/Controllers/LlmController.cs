using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Chapi.AI.Services;
using Chapi.AI.Dto;
using AuthProfiles.Application.Services;
using AuthProfiles.Application.Dtos;
using Chapi.EndpointCatalog.Application;

namespace Chapi.AI.Controllers
{
    [ApiController]
    [Route("api/llm")]
    public class LlmController : ControllerBase
    {
        private readonly IApiTestGenerationService _apiTestService;
        private readonly ILogger<LlmController> _logger;
        private readonly IEndpointContextService _endpointContextService;
        private readonly IAuthAiDetectionService _authDetectionService;
        private readonly IAuthProfileReadService _authProfileReadService;
        private readonly IEndpointAppService _endpointAppService;

        public LlmController(
            IApiTestGenerationService apiTestService,
            IEndpointContextService endpointContextService,
            ILogger<LlmController> logger,
            IAuthAiDetectionService authDetectionService,
            IAuthProfileReadService authProfileReadService,
            IEndpointAppService endpointAppService)
        {
            _apiTestService = apiTestService;
            _endpointContextService = endpointContextService;
            _logger = logger;
            _authDetectionService = authDetectionService;
            _authProfileReadService = authProfileReadService;
            _endpointAppService = endpointAppService;
        }

        [HttpPost("generate")]
        public async Task<ChapiCard> Generate([FromBody] ApiTestGenerateRequest req)
        {
            _logger.LogInformation("LLM generate requested (Chapi.AI controller)");
            var endpointsContext = await _endpointContextService.BuildContextAsync(req.ProjectId);
            var card = await _apiTestService.GenerateTestAsync(req.UserQuery, endpointsContext, req.MaxFiles, req.OpenApiJson);
            return card;
        }

        public class GenerateEndpointRequest
        {
            public Guid AuthProfileId { get; set; }
            public Guid EndpointId { get; set; }
        }

        [HttpPost("generate/endpoint")]
        public async Task<ActionResult<Chapi.IR.ChapiIr>> GenerateEndpoint([FromBody] GenerateEndpointRequest req)
        {
            _logger.LogInformation("LLM generate endpoint requested (Chapi.AI controller)");

            if (req == null) return BadRequest(new { error = "Request body is required" });
            if (req.AuthProfileId == Guid.Empty) return BadRequest(new { error = "AuthProfileId is required" });
            if (req.EndpointId == Guid.Empty) return BadRequest(new { error = "EndpointId is required" });

            var authId = req.AuthProfileId;
            var endpointId = req.EndpointId;

            // Fetch raw DTOs from application services
            var authDto = await _authProfileReadService.GetByIdAsync(authId, HttpContext.RequestAborted).ConfigureAwait(false);
            if (authDto == null) return BadRequest(new { error = $"AuthProfile with id {authId} not found" });

            var epDto = await _endpointAppService.GetAsync(endpointId).ConfigureAwait(false);
            if (epDto == null) return BadRequest(new { error = $"Endpoint with id {endpointId} not found" });

            // Serialize raw DB DTOs and pass them directly to the generator
            var authJson = JsonSerializer.Serialize(authDto, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var endpointJson = JsonSerializer.Serialize(epDto, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            try
            {
                var ir = await _apiTestService.GenerateEndpointtAsync(authJson, endpointJson);
                return Ok(ir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate endpoint IR");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("detect/code")]
        public Task<DetectionResponseDto> DetectByCode([FromBody] DetectByCodeRequest req, System.Threading.CancellationToken ct)
        {
            _logger.LogInformation("LLM detect by code requested");
            return _authDetectionService.DetectByCodeAsync(req.Code ?? string.Empty, req.ProjectId, ct);
        }

        [HttpPost("detect/prompt")]
        public async Task<DetectionResponseDto> DetectByPrompt([FromBody] DetectByPromptRequest req, System.Threading.CancellationToken ct)
        {
            _logger.LogInformation("LLM detect by prompt requested");
            var res = await _authDetectionService.DetectByPromptAsync(req.Prompt ?? string.Empty, req.ProjectId, ct);
            return res;
        }

    }

}