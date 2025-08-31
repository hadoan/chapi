using System;
using System.Collections.Generic;
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
        var args = new KernelArguments { ["user_query"] = userQuery, ["allowed_ops"] = allowedOps, ["hints"] = hints };
        var result = await _semanticKernelService.InvokeAsync("EndpointContext", "Select", args);
        var raw = result.GetValue<string>() ?? string.Empty;
        
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
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var parsedResult = JsonSerializer.Deserialize<Result>(raw, options)!;
            _logger.LogDebug("Successfully parsed JSON directly. ServiceSlug: {ServiceSlug}, PicksCount: {PicksCount}", 
                parsedResult.ServiceSlug, parsedResult.Picks?.Count ?? 0);
            return parsedResult;
        }
        catch (JsonException ex) 
        {
            _logger.LogDebug("Direct JSON parse failed: {Error}", ex.Message);
        }

        // If the model returned additional surrounding text, try to extract the first {...} block
        var first = raw.IndexOf('{');
        var last = raw.LastIndexOf('}');
        if (first >= 0 && last > first)
        {
            var candidate = raw.Substring(first, last - first + 1);
            _logger.LogDebug("Trying to parse extracted JSON block: {ExtractedJson}", candidate);
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var extractedResult = JsonSerializer.Deserialize<Result>(candidate, options)!;
                _logger.LogDebug("Successfully parsed extracted JSON. ServiceSlug: {ServiceSlug}, PicksCount: {PicksCount}", 
                    extractedResult.ServiceSlug, extractedResult.Picks?.Count ?? 0);
                return extractedResult;
            }
            catch (JsonException ex)
            {
                _logger.LogDebug("Extracted JSON parse failed: {Error}", ex.Message);
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
