using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Chapi.AI.Dto;
using ShipMvp.Integration.SemanticKernel.Infrastructure;

namespace Chapi.AI.Services
{
    public class SemanticKernelAuthDetectionService : IAuthAiDetectionService
    {
        private readonly ISemanticKernelService _sk;
        private readonly ILogger<SemanticKernelAuthDetectionService> _logger;

        public SemanticKernelAuthDetectionService(ISemanticKernelService sk, ILogger<SemanticKernelAuthDetectionService> logger)
        {
            _sk = sk ?? throw new ArgumentNullException(nameof(sk));
            _logger = logger;
        }

        public async Task<DetectionResponseDto> DetectByCodeAsync(string code, Guid? projectId, CancellationToken ct)
        {
            // Use Semantic Kernel plugin 'auth_detection' function 'detect_by_code'
            var args = new KernelArguments
            {
                ["code"] = code ?? string.Empty
            };
            if (projectId.HasValue) args["projectId"] = projectId.Value.ToString();

            //var result = await _sk.InvokeAsync("AuthDetect", "ByCode", args, ct);
            //var json = result.GetValue<string>() ?? string.Empty;
            var json = """
                {"detect_source":"ai-by-code","detect_confidence":0.95,"profile":{"type":"OAuth2","environmentKey":"ENVIRONMENT_KEY","parameters":{"tokenUrl":"/connect/token","authorizationUrl":null,"audience":null,"scopes":"openid email profile roles","clientId":"spa-client","clientSecretRef":null,"usernameRef":"CREDENTIALS_EMAIL","passwordRef":"CREDENTIALS_PASSWORD","customLoginUrl":null,"customBodyType":"form","customUserKey":null,"customPassKey":null,"tokenJsonPath":"access_token"},"injection":{"mode":"Header","name":"Authorization","format":"Bearer"},"secrets":[{"key":"CREDENTIALS_EMAIL","secretRef":"EMAIL_SECRET"},{"key":"CREDENTIALS_PASSWORD","secretRef":"PASSWORD_SECRET"}],"token_request":{"method":"POST","url":"/connect/token","headers":{"Content-Type":"application/x-www-form-urlencoded"},"body":{"kind":"form","value":{"grant_type":"password","client_id":"spa-client","username":"CREDENTIALS_EMAIL","password":"CREDENTIALS_PASSWORD","scope":"openid email profile roles"}},"expect":{"status":200,"tokenJsonPath":"access_token"}}}}
                """;
            try
            {
                var dto = JsonSerializer.Deserialize<DetectionResponseDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return dto ?? CreateEmptyResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize auth detection response from Semantic Kernel");
                return CreateEmptyResponse();
            }
        }

        public async Task<DetectionResponseDto> DetectByPromptAsync(string prompt, Guid? projectId, CancellationToken ct)
        {
            var args = new KernelArguments
            {
                ["prompt"] = prompt ?? string.Empty
            };
            if (projectId.HasValue) args["projectId"] = projectId.Value.ToString();

            var result = await _sk.InvokeAsync("AuthDetect", "ByPrompt", args, ct);
            var json = result.GetValue<string>() ?? string.Empty;

            try
            {
                var dto = JsonSerializer.Deserialize<DetectionResponseDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return dto ?? CreateEmptyResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize auth detection response from Semantic Kernel");
                return CreateEmptyResponse();
            }
        }

        private static DetectionResponseDto CreateEmptyResponse()
        {
            return new DetectionResponseDto(
                "unknown",
                0.0,
                new DetectedProfile(
                    "Unknown",
                    "dev",
                    new DetectedParameters(null, null, null, null, null, null, null, null, null, null, null, null, null),
                    new InjectionPreview("Header", "Authorization", "Bearer {{token}}"),
                    null,
                    null
                )
            );
        }
    }
}
