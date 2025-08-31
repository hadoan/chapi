using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ShipMvp.Integration.SemanticKernel.Infrastructure;
namespace Chapi.AI.Services;

public sealed class EndpointSelectorService
{
    private readonly ISemanticKernelService _semanticKernelService;
    private readonly ILogger<EndpointSelectorService> _logger;

    public EndpointSelectorService(ISemanticKernelService semanticKernelService, ILogger<EndpointSelectorService> logger)
    {
        _semanticKernelService = semanticKernelService;
        _logger = logger;
    }

    public sealed record Pick(string Method, string Path, string Auth, string Req, string Success);
    public sealed record Result(string ServiceSlug, List<Pick> Picks);

    public async Task<Result> SelectAsync(string userQuery, string allowedOps, string hints = "")
    {
        _logger.LogInformation("=== EndpointSelectorService.SelectAsync ===");
        _logger.LogInformation("User query: {UserQuery}", userQuery);
        _logger.LogInformation("Allowed ops count: {AllowedOpsCount}", allowedOps.Split('\n').Length);
        _logger.LogInformation("Hints count: {HintsCount}", hints.Split('\n').Where(h => !string.IsNullOrWhiteSpace(h)).Count());

        var args = new KernelArguments { ["user_query"] = userQuery, ["allowed_ops"] = allowedOps, ["hints"] = hints };
        
        _logger.LogInformation("Invoking EndpointContext.Select plugin...");
        var result = await _semanticKernelService.InvokeAsync("EndpointContext", "Select", args);
        var raw = result.GetValue<string>() ?? string.Empty;
        
        _logger.LogInformation("Raw LLM response ({Length} chars): {RawResponse}", raw.Length, raw.Length > 500 ? raw.Substring(0, 500) + "..." : raw);
        
        _logger.LogDebug("=== JSON Parsing Attempts ===");
        _logger.LogDebug("Raw LLM response: {RawResponse}", raw);
        
        raw = raw.Trim();

        // Remove triple-backtick fenced blocks (optionally with language after opening ticks)
        if (raw.StartsWith("```"))
            {
                // drop opening fence and optional language tag
                var firstNewline = raw.IndexOf('\n');
                if (firstNewline >= 0)
                {
                    raw = raw.Substring(firstNewline + 1);
                }
                // drop trailing fence if present
                if (raw.EndsWith("```")) raw = raw.Substring(0, raw.Length - 3);
                raw = raw.Trim();
            }

        // If wrapped in single backticks, remove them
        if (raw.StartsWith("`") && raw.EndsWith("`")) raw = raw.Trim('`').Trim();

        _logger.LogDebug("Cleaned LLM response for parsing: {CleanedResponse}", raw);

        // Try direct parse first
        _logger.LogDebug("Attempt 1: Direct JSON parsing...");
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var parsedResult = JsonSerializer.Deserialize<Result>(raw, options);
            if (parsedResult == null)
            {
                _logger.LogWarning("Deserialized result is null");
                return new Result("unknown", new List<Pick>());
            }
            
            _logger.LogInformation("✓ Direct JSON parse successful! ServiceSlug: {ServiceSlug}, PicksCount: {PicksCount}", 
                parsedResult.ServiceSlug, parsedResult.Picks?.Count ?? 0);
            
            if (parsedResult.Picks != null && parsedResult.Picks.Any())
            {
                _logger.LogInformation("Selected endpoints:");
                foreach (var pick in parsedResult.Picks)
                {
                    _logger.LogInformation("  - {Method} {Path} | auth:{Auth}", pick.Method, pick.Path, pick.Auth);
                }
            }
            
            return parsedResult;
        }
        catch (JsonException ex) 
        {
            _logger.LogWarning("❌ Direct JSON parse failed: {Error}", ex.Message);
        }

        // If the model returned additional surrounding text, try to extract the first {...} block
        var first = raw.IndexOf('{');
        var last = raw.LastIndexOf('}');
        if (first >= 0 && last > first)
        {
            var candidate = raw.Substring(first, last - first + 1);
            _logger.LogInformation("Trying to parse extracted JSON block ({Length} chars)...", candidate.Length);
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var extractedResult = JsonSerializer.Deserialize<Result>(candidate, options);
                if (extractedResult == null)
                {
                    _logger.LogWarning("Extracted JSON deserialized to null");
                    return new Result("unknown", new List<Pick>());
                }
                
                _logger.LogInformation("✓ Extracted JSON parse successful! ServiceSlug: {ServiceSlug}, PicksCount: {PicksCount}", 
                    extractedResult.ServiceSlug, extractedResult.Picks?.Count ?? 0);
                return extractedResult;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("❌ Extracted JSON parse failed: {Error}", ex.Message);
            }
        }

        // As a last resort, try replacing single quotes with double quotes (some LLMs emit single-quoted JSON)
        var alt = raw.Replace("'", "\"");
        _logger.LogDebug("Trying to parse with quote replacement: {AltJson}", alt);
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var altResult = JsonSerializer.Deserialize<Result>(alt, options)!;
            _logger.LogDebug("Successfully parsed JSON with quote replacement. ServiceSlug: {ServiceSlug}, PicksCount: {PicksCount}", 
                altResult.ServiceSlug, altResult.Picks?.Count ?? 0);
            return altResult;
        }
        catch (JsonException ex)
        {
            _logger.LogError("All JSON parsing attempts failed. Final error: {Error}", ex.Message);
            throw new JsonException($"Failed to parse JSON from LLM output. Raw output:\n{raw}", ex);
            }
    }
}
