using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


// Services/RunPackService.cs
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
// avoid Microsoft.SemanticKernel.Plugins to keep compatibility with the installed SK package surface
using ShipMvp.Integration.SemanticKernel.Infrastructure;
using System.Text.Json;
using Chapi.AI.Utilities;
using System.IO;


namespace Chapi.AI.Services;

public sealed class RunPackService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chat;
    private readonly string _pluginDir;

    public RunPackService(ShipMvp.Integration.SemanticKernel.Infrastructure.ISemanticKernelService skService, string? pluginRoot = null)
    {
        // Use injected semantic kernel service - do not create a new builder here
        _kernel = skService.Kernel;
        _chat = _kernel.GetRequiredService<IChatCompletionService>();

        // Note: don't create a Kernel plugin here because the project may use prompt-based plugins
        // loaded at kernel build time. If a runpack plugin is present, we'll attempt to wire a
        // per-request RunPackBuilder into it via reflection at invocation time.
        // Keep an initial no-op builder for safety.
        var initialBuilder = new RunPackBuilder();

        _pluginDir = pluginRoot ?? Path.Combine(AppContext.BaseDirectory, "plugins", "RunPack", "GenerateFiles");
    }

    public async Task<byte[]> GenerateZipAsync(
        IReadOnlyList<string> manifestPaths,     // from card.files[].path
        string endpointsContext,                 // compact text
        string env = "local",
        Dictionary<string, string>? fileRoles = null,      // path -> "AUTH"|"SMOKE"|"CRUD"
        Dictionary<string, string>? roleContexts = null)   // role -> compact endpoint text
    {
        // Reset the builder for this run by replacing the plugin instance's field
        var runBuilder = new RunPackBuilder();
        var runPackPlugin = _kernel.Plugins.FirstOrDefault(p => string.Equals(p.Name, "runpack", StringComparison.OrdinalIgnoreCase));
        if (runPackPlugin != null)
        {
            // Try to get a PluginInstance property via reflection (not all plugin implementations expose it)
            var pluginInstanceProp = runPackPlugin.GetType().GetProperty("PluginInstance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var pluginInstance = pluginInstanceProp?.GetValue(runPackPlugin);
            if (pluginInstance != null)
            {
                var field = pluginInstance.GetType().GetField("_builder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(pluginInstance, runBuilder);
            }
        }

        // Try to invoke a 'GenerateFiles' function on any loaded plugin using the kernel's InvokeAsync by plugin name.
        var filesManifest = string.Join("\n", manifestPaths.Select(p => $"- {p}"));
        var args = new KernelArguments
        {
            ["files_manifest"] = filesManifest,
            ["env"] = env,
            ["endpoints_context"] = endpointsContext
        };

        // Add role-based parameters if provided
        if (fileRoles != null && fileRoles.Any())
        {
            // Convert to JSON format expected by prompt
            var roleEntries = fileRoles.Select(kvp => $"\"{kvp.Key}\": \"{kvp.Value}\"");
            var fileRolesJson = "{ " + string.Join(", ", roleEntries) + " }";
            args["file_roles_json"] = fileRolesJson;
        }

        if (roleContexts != null && roleContexts.Any())
        {
            // Convert to JSON format expected by prompt
            var contextEntries = roleContexts.Select(kvp => $"\"{kvp.Key}\": \"{kvp.Value.Replace("\"", "\\\"")}\"");
            var roleContextsJson = "{ " + string.Join(", ", contextEntries) + " }";
            args["role_contexts_json"] = roleContextsJson;
        }

        foreach (var plugin in _kernel.Plugins)
        {
            try
            {
                var pluginName = plugin.Name;
                var result = await _kernel.InvokeAsync(pluginName, "GenerateFiles", args);
                // If invocation succeeded, assume runpack.AddFile was called by the model.
                break;
            }
            catch
            {
                // Try next plugin
            }
        }

        // If runBuilder has no files, produce simple placeholders
        if (runBuilder.Files.Count == 0 && manifestPaths.Any())
        {
            foreach (var path in manifestPaths.Distinct())
            {
                var testObj = new Dictionary<string, object>
                {
                    ["version"] = "chapi-test-1",
                    ["suite"] = Path.GetFileName(Path.GetDirectoryName(path) ?? "run-pack") ?? "run-pack",
                    ["env"] = env,
                    ["tests"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["name"] = $"smoke - {Path.GetFileName(path)}",
                            ["request"] = new Dictionary<string, object>
                            {
                                ["method"] = "GET",
                                ["url"] = "{{BASE_URL}}/",
                                ["headers"] = new Dictionary<string,string>{{"Accept","application/json"}}
                            },
                            ["expect"] = new Dictionary<string, object>{{"status",200}}
                        }
                    }
                };

                var json = JsonSerializer.Serialize(testObj, new JsonSerializerOptions { WriteIndented = true });
                runBuilder.Add(path, json);
            }
        }

        return runBuilder.ToZip(includeHelpers: true);
    }
}
