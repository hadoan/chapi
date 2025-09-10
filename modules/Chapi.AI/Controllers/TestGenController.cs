using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Text.Json;
using Chapi.AI.Services;
using Chapi.AI.Dto;

namespace Chapi.AI.Controllers
{
    [ApiController]
    [Route("api/testgen")]
    public class TestGenController : ControllerBase
    {
        private readonly ITestGenService _testGenService;
        private readonly ILogger<TestGenController> _logger;

        public TestGenController(ITestGenService testGenService, ILogger<TestGenController> logger)
        {
            _testGenService = testGenService;
            _logger = logger;
        }

        [HttpPost("generate")]
        public async Task<ActionResult<TestGenResponse>> Generate([FromBody] TestGenRequest request)
        {
            try
            {
                _logger.LogInformation("TestGen generate requested for project {ProjectId}", request.ProjectId);

                // Parse the testGenInput from the openApiJson field
                var testGenInput = JsonSerializer.Deserialize<TestGenInput>(request.OpenApiJson ?? "{}", new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (testGenInput == null)
                {
                    return BadRequest("Invalid testGenInput in openApiJson field");
                }

                var result = await _testGenService.GenerateTestsAsync(testGenInput);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate tests");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
