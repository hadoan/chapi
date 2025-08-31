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
using Microsoft.Extensions.Logging;


namespace Chapi.AI.Services;

public sealed class RunPackService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chat;
    private readonly string _pluginDir;
    private readonly ILogger<RunPackService> _logger;
    private readonly Chapi.AI.Utilities.RunPackBuilder _runPackBuilder;

    public RunPackService(
        ShipMvp.Integration.SemanticKernel.Infrastructure.ISemanticKernelService skService, 
        ILogger<RunPackService> logger,
        Chapi.AI.Utilities.RunPackBuilder runPackBuilder,
        string? pluginRoot = null)
    {
        // Use injected semantic kernel service - do not create a new builder here
        _kernel = skService.Kernel;
        _chat = _kernel.GetRequiredService<IChatCompletionService>();
        _logger = logger;
        _runPackBuilder = runPackBuilder;

        // Note: Using the injected RunPackBuilder that's shared with the plugin
        _pluginDir = pluginRoot ?? Path.Combine(AppContext.BaseDirectory, "plugins", "RunPack", "GenerateFiles");
    }

    public async Task<byte[]> GenerateZipAsync(
        IReadOnlyList<string> manifestPaths,     // from card.files[].path
        string endpointsContext,                 // compact text
        string env = "local",
        Dictionary<string, string>? fileRoles = null,      // path -> "AUTH"|"SMOKE"|"CRUD"
        Dictionary<string, string>? roleContexts = null)   // role -> compact endpoint text
    {
        _logger.LogInformation("=== RunPackService.GenerateZipAsync Started ===");
        _logger.LogInformation("Manifest paths: {ManifestPaths}", string.Join(", ", manifestPaths));
        _logger.LogInformation("Environment: {Env}", env);
        _logger.LogInformation("Endpoints context length: {ContextLength} chars", endpointsContext.Length);

        // Reset the builder for this run by replacing the plugin instance's field
        // Use the shared RunPackBuilder instance instead of creating a new one
        var runPackPlugin = _kernel.Plugins.FirstOrDefault(p => string.Equals(p.Name, "runpack", StringComparison.OrdinalIgnoreCase));
        if (runPackPlugin != null)
        {
            _logger.LogInformation("Found runpack plugin: {PluginName}", runPackPlugin.Name);
            // Try to get a PluginInstance property via reflection (not all plugin implementations expose it)
            var pluginInstanceProp = runPackPlugin.GetType().GetProperty("PluginInstance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var pluginInstance = pluginInstanceProp?.GetValue(runPackPlugin);
            if (pluginInstance != null)
            {
                var field = pluginInstance.GetType().GetField("_builder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(pluginInstance, _runPackBuilder);
                    _logger.LogInformation("✓ RunPackBuilder injected into plugin instance");
                }
                else
                {
                    _logger.LogWarning("⚠️  Could not find _builder field in plugin instance");
                }
            }
            else
            {
                _logger.LogWarning("⚠️  Could not get plugin instance from runpack plugin");
            }
        }
        else
        {
            _logger.LogWarning("⚠️  No runpack plugin found in kernel!");
        }

        // Try to invoke a 'GenerateFiles' function on any loaded plugin using the kernel's InvokeAsync by plugin name.
        var filesManifest = string.Join("\n", manifestPaths.Select(p => $"- {p}"));
        var args = new KernelArguments
        {
            ["files_manifest"] = filesManifest,
            ["env"] = env,
            ["endpoints_context"] = endpointsContext
        };

        _logger.LogInformation("=== Kernel Arguments ===");
        _logger.LogInformation("files_manifest: {FilesManifest}", filesManifest);
        _logger.LogInformation("env: {Env}", env);
        _logger.LogInformation("endpoints_context preview: {ContextPreview}...", 
            endpointsContext.Length > 100 ? endpointsContext.Substring(0, 100) : endpointsContext);

        // Add role-based parameters if provided
        if (fileRoles != null && fileRoles.Any())
        {
            // Convert to JSON format expected by prompt
            var roleEntries = fileRoles.Select(kvp => $"\"{kvp.Key}\": \"{kvp.Value}\"");
            var fileRolesJson = "{ " + string.Join(", ", roleEntries) + " }";
            args["file_roles_json"] = fileRolesJson;
            
            _logger.LogInformation("file_roles_json: {FileRoles}", fileRolesJson);
        }
        else
        {
            _logger.LogWarning("⚠️  No file roles provided to RunPackService.GenerateZipAsync");
        }

        if (roleContexts != null && roleContexts.Any())
        {
            // Convert to JSON format expected by prompt
            var contextEntries = roleContexts.Select(kvp => $"\"{kvp.Key}\": \"{kvp.Value.Replace("\"", "\\\"")}\"");
            var roleContextsJson = "{ " + string.Join(", ", contextEntries) + " }";
            args["role_contexts_json"] = roleContextsJson;
            
            _logger.LogInformation("=== Role Contexts Passed to Prompt ===");
            foreach (var kvp in roleContexts)
            {
                if (kvp.Value.Length > 0)
                {
                    var lines = kvp.Value.Split('\n').Take(2);
                    _logger.LogInformation("{Role} context:", kvp.Key);
                    foreach (var line in lines)
                    {
                        _logger.LogInformation("  {Line}", line);
                    }
                    if (kvp.Value.Split('\n').Length > 2)
                    {
                        _logger.LogInformation("  ... ({MoreLines} more lines)", kvp.Value.Split('\n').Length - 2);
                    }
                }
                else
                {
                    _logger.LogWarning("❌ {Role} context is EMPTY!", kvp.Key);
                }
            }
        }
        else
        {
            _logger.LogWarning("⚠️  No role contexts provided to RunPackService.GenerateZipAsync");
        }

        // Instead of using the prompt plugin, use OpenAI ChatCompletion directly with function calling
        _logger.LogInformation("=== Invoking OpenAI ChatCompletion with Function Calling ===");
        try
        {
            // Get OpenAI chat completion service specifically
            var openAiChatCompletion = _kernel.GetRequiredService<IChatCompletionService>("openai");
            
            // Create the system prompt
            var systemPrompt = $@"You are Chapi-TestWriter.

IMPORTANT: You have access to a tool called ""AddFile"" that you MUST use to create files.
For each file in the manifest, you will use the AddFile tool to create it.

The AddFile tool takes two parameters:
- path: the file path (string)  
- content: the JSON content as a string

You must call this tool for each file, then respond ""DONE"".

INPUTS
FILES:
{args["files_manifest"]}

ROLES (JSON map path->role):
{args["file_roles_json"]}

ROLE_CONTEXTS:
{args["role_contexts_json"]}

CONTENT RULES
- Content MUST be STRICT JSON in ""chapi-test-1"" schema:
  {{ ""version"":""chapi-test-1"",""suite"":""<service>-<role>"",""env"":""{args["env"]}"",""tests"":[...] }}
- CRITICAL: Use DOLLAR-BRACE template placeholders in URLs: ${{BASE_URL}}, ${{TOKEN}}, ${{email}}, ${{id}}
- NEVER use double-curly-brace format in generated content
- These are string templates that will be replaced at runtime - use them literally in JSON strings
- Prefer application/json; 3–6 concise tests per file.

ROLE-SPECIFIC RULES
AUTH:
- CRITICAL: If generating an auth.json file, you MUST follow these rules exactly:
- Check ROLE_CONTEXTS.AUTH - it MUST contain pre-filtered protected endpoints.
- If ROLE_CONTEXTS.AUTH is empty or missing, respond with: ""ERROR: No protected endpoints available for AUTH tests""
- NEVER use ""/"" or ""/health"" or ""/ping"" endpoints for AUTH tests under any circumstances.
- Pick ONE endpoint from ROLE_CONTEXTS.AUTH and create exactly THREE tests:
  1) NO Authorization header -> expect 401 or 403
  2) Invalid Bearer token (e.g. ""Bearer invalid_token_12345"") -> expect 401 or 403  
  3) Valid Bearer token (use ${{TOKEN}}) -> expect 2xx success
- All three tests use the SAME endpoint URL but different auth headers.
- Suite name MUST be ""<service>-auth"" (not just ""<service>"").
- Test names MUST describe the auth scenario (e.g. ""GET /api/emails without token returns 401"").

SMOKE:
- Use endpoints from ROLE_CONTEXTS.SMOKE (filtered representative endpoints).
- Create 3–5 tests across different endpoints for basic connectivity/health checking.
- Focus on GET operations with 200 status assertions.

CRUD:
- Use endpoints from ROLE_CONTEXTS.CRUD (filtered write operations like POST/PUT/PATCH/DELETE).
- Create 3–6 tests covering create, read, update, delete operations if present.
- Include both successful operations and validation error scenarios.

CONSTRAINTS
- DO NOT print file content inline.
- DO NOT use markdown code blocks or backticks in your response.
- Use the AddFile tool for each manifest entry with the file path and JSON content.
- Template variables like ${{BASE_URL}} are literal strings, NOT function calls.
- CORRECT example URL: ""${{BASE_URL}}/api/emails"" 
- WRONG: use double curly braces (this will cause errors!)
- After you have used the AddFile tool for ALL files, respond with: DONE";

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage("Create the test files as specified in the manifest.");

            // Enable function calling with OpenAI-specific settings
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.15,
                MaxTokens = 1600,
                ServiceId = "openai"
            };

            var result = await openAiChatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel);
                
            _logger.LogInformation("✓ OpenAI ChatCompletion with function calling completed");
            _logger.LogInformation("Result: {Result}", result.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Failed to invoke OpenAI ChatCompletion with function calling: {Error}", ex.Message);
            _logger.LogError("Exception details: {Exception}", ex.ToString());
        }

        // If runBuilder has no files, produce simple placeholders
        _logger.LogInformation("=== Checking RunPackBuilder Results ===");
        _logger.LogInformation("RunPackBuilder file count: {FileCount}", _runPackBuilder.Files.Count);
        _logger.LogInformation("RunPackBuilder hash: {Hash}", _runPackBuilder.GetHashCode());
        
        if (_runPackBuilder.Files.Count == 0 && manifestPaths.Any())
        {
            _logger.LogWarning("⚠️  RunPackBuilder is empty! Generating fallback placeholders.");
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
                _runPackBuilder.Add(path, json);
                _logger.LogInformation("Added fallback placeholder for: {Path}", path);
            }
        }
        else if (_runPackBuilder.Files.Count > 0)
        {
            _logger.LogInformation("✓ RunPackBuilder contains {FileCount} files:", _runPackBuilder.Files.Count);
            foreach (var file in _runPackBuilder.Files.Take(5))
            {
                _logger.LogInformation("  - {FileName}", file.Path);
            }
            if (_runPackBuilder.Files.Count > 5)
            {
                _logger.LogInformation("  ... and {MoreCount} more files", _runPackBuilder.Files.Count - 5);
            }
        }

        var finalZip = _runPackBuilder.ToZip(includeHelpers: true);
        _logger.LogInformation("✓ Final ZIP generated: {ZipSize} bytes", finalZip.Length);
        return finalZip;
    }
}
