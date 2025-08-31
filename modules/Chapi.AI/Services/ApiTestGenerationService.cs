using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ShipMvp.Integration.SemanticKernel.Infrastructure;

namespace Chapi.AI.Services
{
    public class ApiTestGenerationService : IApiTestGenerationService
    {
        private readonly ISemanticKernelService _semanticKernelService;
        private readonly ILogger<ApiTestGenerationService> _logger;

        public ApiTestGenerationService(ISemanticKernelService semanticKernelService, ILogger<ApiTestGenerationService> logger)
        {
            _semanticKernelService = semanticKernelService;
            _logger = logger;
        }

        // Call the Semantic Kernel plugin ApiTest.Generate and return the string result
        // Convenience: existing signature preserved
        public async Task<Chapi.AI.Dto.ChapiCard> GenerateTestAsync(string openApiJson)
        {
            return await GenerateTestAsync(userQuery: null, endpointsContext: null, maxFiles: null, openApiJson: openApiJson);
        }

        // Preferred overload: accept inputs that match the plugin config.json variables
        public async Task<Chapi.AI.Dto.ChapiCard> GenerateTestAsync(string? userQuery, string? endpointsContext, int? maxFiles, string? openApiJson)
        {
            try
            {
                var args = new KernelArguments();

                if (!string.IsNullOrEmpty(userQuery))
                    args["user_query"] = userQuery;
                if (!string.IsNullOrEmpty(endpointsContext))
                    args["endpoints_context"] = endpointsContext;
                if (maxFiles.HasValue)
                    args["max_files"] = maxFiles.Value.ToString();
                if (!string.IsNullOrEmpty(openApiJson))
                    args["openApiJson"] = openApiJson;

                _logger.LogInformation("Calling ApiTest.Generate with args: UserQuery='{UserQuery}', EndpointsContext length={ContextLength}, MaxFiles={MaxFiles}",
                    userQuery, endpointsContext?.Length ?? 0, maxFiles);

                var result = await _semanticKernelService.InvokeAsync("ApiTest", "Generate", args);

                // Prefer function value, then rendered prompt, then empty
                var content = result.GetValue<string>() ?? result.RenderedPrompt ?? string.Empty;

                _logger.LogInformation("ApiTest plugin returned: {Content}", content.Length > 500 ? content.Substring(0, 500) + "..." : content);

                // Parse JSON into DTO
                var card = System.Text.Json.JsonSerializer.Deserialize<Chapi.AI.Dto.ChapiCard>(content, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (card == null)
                {
                    _logger.LogWarning("ApiTest plugin returned invalid JSON: {Content}", content);
                    throw new System.InvalidOperationException("Invalid response from ApiTest plugin");
                }

                return card;
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "ApiTestGenerationService.GenerateTestAsync failed");
                throw;
            }
        }
    }
}
