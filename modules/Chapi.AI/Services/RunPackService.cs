using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Chapi.AI.Utilities;
using ShipMvp.Integration.SemanticKernel.Infrastructure;

namespace Chapi.AI.Services;

public class RunPackService
{
    private readonly ISemanticKernelService _semanticKernelService;
    private readonly ILogger<RunPackService> _logger;
    private readonly RunPackBuilder _runPackBuilder;

    public RunPackService(
        ISemanticKernelService semanticKernelService,
        RunPackBuilder runPackBuilder,
        ILogger<RunPackService> logger)
    {
        _semanticKernelService = semanticKernelService;

        _runPackBuilder = runPackBuilder;
        _logger = logger;
    }

    public async Task<byte[]> GenerateZipAsync(
        IReadOnlyList<string> manifestPaths,
        string endpointsContext,
        string environment,
        Dictionary<string, string> fileRoles,
        Dictionary<string, string> roleContexts)
    {
        _logger.LogInformation("=== RunPackService.GenerateZipAsync Started ===");
        _logger.LogInformation("Manifest paths: {ManifestPaths}", string.Join(", ", manifestPaths));
        _logger.LogInformation("Environment: {Environment}", environment);
        _logger.LogInformation("Endpoints context length: {Length} chars", endpointsContext.Length);


        // Check if the RunPack plugin exists
        var runpackPlugin = _semanticKernelService.Kernel.Plugins.FirstOrDefault(p => p.Name == "RunPack");
        if (runpackPlugin == null)
        {
            _logger.LogWarning("⚠️  RunPack plugin not found in kernel");
        }
        else
        {
            _logger.LogInformation("Found runpack plugin: {PluginName}", runpackPlugin.Name);
        }

        // Prepare kernel arguments
        var kernelArguments = new KernelArguments
        {
            ["files_manifest"] = string.Join("\n", manifestPaths.Select(p => $"- {p}")),
            ["env"] = environment,
            ["endpoints_context"] = endpointsContext,
            ["file_roles_json"] = JsonSerializer.Serialize(fileRoles ?? new Dictionary<string, string>())
        };

        _logger.LogInformation("=== Kernel Arguments ===");
        _logger.LogInformation("files_manifest: {FilesManifest}", kernelArguments["files_manifest"]);
        _logger.LogInformation("env: {Environment}", kernelArguments["env"]);
        _logger.LogInformation("endpoints_context preview: {ContextPreview}",
            endpointsContext.Length > 100 ? endpointsContext.Substring(0, 100) + "..." : endpointsContext);
        _logger.LogInformation("file_roles_json: {FileRoles}", kernelArguments["file_roles_json"]);

        // Add role-specific contexts to kernel arguments
        _logger.LogInformation("=== Role Contexts Passed to Prompt ===");
        foreach (var roleContext in roleContexts)
        {
            var argKey = $"{roleContext.Key.ToLower()}_context";
            kernelArguments[argKey] = roleContext.Value;
            _logger.LogInformation("{Role} context:", roleContext.Key);
            var lines = roleContext.Value.Split('\n');
            foreach (var line in lines.Take(3))
            {
                _logger.LogInformation("  {Line}", line);
            }
            if (lines.Length > 3)
            {
                _logger.LogInformation("  ... ({Count} more lines)", lines.Length - 3);
            }
        }

        try
        {
            _logger.LogInformation("=== Invoking RunPack Plugin ===");

            // Use the semantic kernel to invoke the RunPack plugin
            var result = await _semanticKernelService.Kernel.InvokeAsync("RunPack", "GenerateFiles", kernelArguments);

            _logger.LogInformation("✓ RunPack plugin invocation completed");
            _logger.LogInformation("Result: {Result}", result.GetValue<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error invoking RunPack plugin");
        }

        // If runBuilder has no files, produce simple placeholders
        _logger.LogInformation("=== Checking RunPackBuilder Results ===");
        _logger.LogInformation("RunPackBuilder file count: {FileCount}", _runPackBuilder.Files.Count);

        if (_runPackBuilder.Files.Count == 0 && manifestPaths.Any())
        {
            _logger.LogWarning("⚠️  RunPackBuilder is empty! Generating fallback placeholders.");

            foreach (var manifestPath in manifestPaths)
            {
                var role = fileRoles?.GetValueOrDefault(manifestPath) ?? DetermineFileRole(manifestPath);
                var fallbackContent = GenerateFallbackContent(role, environment);
                _runPackBuilder.Add(manifestPath, fallbackContent);
                _logger.LogInformation("Added fallback placeholder for: {Path}", manifestPath);
            }
        }

        var zipBytes = _runPackBuilder.ToZip();
        _logger.LogInformation("✓ Final ZIP generated: {Size} bytes", zipBytes.Length);

        return zipBytes;
    }

    private string DetermineFileRole(string filePath)
    {
        // Determine role from file extension or path pattern
        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(filePath);

        if (fileName.Contains("test", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("spec", StringComparison.OrdinalIgnoreCase))
        {
            return "test";
        }

        if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return "api";
        }

        if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".ts", StringComparison.OrdinalIgnoreCase))
        {
            return "frontend";
        }

        return "misc";
    }

    private string GenerateFallbackContent(string role, string environment)
    {
        return role.ToLower() switch
        {
            "api" => $"// Generated API file for {environment} environment\n// TODO: Implement actual logic",
            "test" => $"// Generated test file for {environment} environment\n// TODO: Implement actual tests",
            "frontend" => $"// Generated frontend file for {environment} environment\n// TODO: Implement actual functionality",
            _ => $"// Generated {role} file for {environment} environment\n// TODO: Implement"
        };
    }
}
