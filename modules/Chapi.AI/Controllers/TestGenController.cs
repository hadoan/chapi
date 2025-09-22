using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Text.Json;
using System.Threading;
using System.Collections.Generic;
using Chapi.AI.Services;
using Chapi.AI.Dto;
using AuthProfiles.Application.Services;
using AuthProfiles.Application.Dtos;

namespace Chapi.AI.Controllers
{
    [ApiController]
    [Route("api/testgen")]
    public class TestGenController : ControllerBase
    {
        private readonly ITestGenService _testGenService;
        private readonly ILogger<TestGenController> _logger;
        private readonly IAuthProfileReadService _authProfileReadService;

        public TestGenController(ITestGenService testGenService, ILogger<TestGenController> logger, IAuthProfileReadService authProfileReadService)
        {
            _testGenService = testGenService;
            _logger = logger;
            _authProfileReadService = authProfileReadService;
        }

        [HttpPost("generate")]
        public async Task<ActionResult<TestGenResponse>> Generate([FromBody] TestGenRequest request, CancellationToken ct)
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

                // If the input included a reference to an existing AuthProfile, reload it from the DB
                if (!string.IsNullOrEmpty(testGenInput.AuthProfile?.Id))
                {
                    if (Guid.TryParse(testGenInput.AuthProfile.Id, out var aid))
                    {
                        var dto = await _authProfileReadService.GetByIdAsync(aid, ct).ConfigureAwait(false);
                        if (dto == null)
                        {
                            return BadRequest($"AuthProfile with id {aid} not found");
                        }

                        // Map AuthProfileDto to Chapi.AI.Dto.AuthProfile
                        testGenInput.AuthProfile = MapAuthProfileDto(dto);
                        _logger.LogInformation("Reloaded AuthProfile {AuthProfileId} from DB for test generation", aid);
                    }
                    else
                    {
                        return BadRequest("AuthProfile.Id is not a valid GUID");
                    }
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

        private static AuthProfile MapAuthProfileDto(AuthProfileDto dto)
        {
            // Build a case-insensitive config dictionary. SecretRefs override Params.
            var cfg = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            static string? ParamToString(object? val)
            {
                if (val == null) return null;
                if (val is JsonElement je)
                {
                    return je.ValueKind switch
                    {
                        JsonValueKind.String => je.GetString(),
                        JsonValueKind.Number => je.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Null => null,
                        _ => je.GetRawText()
                    };
                }

                return val.ToString();
            }

            if (dto.Params != null)
            {
                foreach (var kv in dto.Params)
                {
                    try
                    {
                        var s = ParamToString(kv.Value);
                        if (!string.IsNullOrEmpty(s)) cfg[kv.Key] = s;
                    }
                    catch
                    {
                        // ignore individual param conversion errors but continue mapping others
                    }
                }
            }

            if (dto.SecretRefs != null)
            {
                foreach (var kv in dto.SecretRefs)
                {
                    // Secret refs should override params when present
                    if (kv.Value != null)
                        cfg[kv.Key] = kv.Value;
                }
            }

            // Explicit mapping of high-level fields
            var profile = new AuthProfile
            {
                Id = dto.Id?.ToString() ?? string.Empty,
                // Use auth type name as the default Name
                Name = dto.Type.ToString(),
                Type = dto.Type switch
                {
                    AuthProfiles.Domain.AuthType.OAuth2ClientCredentials => "OIDC_CLIENT_CREDENTIALS",
                    AuthProfiles.Domain.AuthType.OAuth2Password => "OIDC_PASSWORD",
                    AuthProfiles.Domain.AuthType.Basic => "BASIC",
                    AuthProfiles.Domain.AuthType.BearerStatic => "BEARER",
                    AuthProfiles.Domain.AuthType.ApiKeyHeader => "API_KEY",
                    AuthProfiles.Domain.AuthType.CustomLogin => "CUSTOM_SCRIPT",
                    _ => "NONE"
                },
                Config = cfg
            };

            // If there are common keys we want to normalize, do that here (non-destructive)
            // e.g. ensure token url keys exist in expected casing
            if (cfg.TryGetValue("token_url", out var turl) && !cfg.ContainsKey("TOKEN_URL")) profile.Config["TOKEN_URL"] = turl;
            if (cfg.TryGetValue("client_id", out var cid) && !profile.Config.ContainsKey("CLIENT_ID")) profile.Config["CLIENT_ID"] = cid;
            if (cfg.TryGetValue("client_secret", out var csecret) && !profile.Config.ContainsKey("CLIENT_SECRET")) profile.Config["CLIENT_SECRET"] = csecret;

            return profile;
        }
    }
}
