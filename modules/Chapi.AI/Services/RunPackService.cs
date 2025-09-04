using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Chapi.AI.Utilities;
using Chapi.AI.Models;
using ShipMvp.Integration.SemanticKernel.Infrastructure;

namespace Chapi.AI.Services;

public class RunPackService
{
    private readonly ISemanticKernelService _semanticKernelService;
    private readonly ILogger<RunPackService> _logger;
    private readonly RunPackBuilder _runPackBuilder;

    public RunPackService(
        ISemanticKernelService semanticKernelService,
        ILogger<RunPackService> logger,
        RunPackBuilder runPackBuilder)
    {
        _semanticKernelService = semanticKernelService;
        _logger = logger;
        _runPackBuilder = runPackBuilder;
    }

    //public async Task<byte[]> GenerateZipAsync(
    //    IReadOnlyList<string> manifestPaths,
    //    string endpointsContext,
    //    string environment,
    //    Dictionary<string, string> fileRoles,
    //    Dictionary<string, string> roleContexts)
    //{
    //    _logger.LogInformation("=== RunPackService.GenerateZipAsync Started ===");
    //    _logger.LogInformation("Manifest paths: {ManifestPaths}", string.Join(", ", manifestPaths));
    //    _logger.LogInformation("Environment: {Environment}", environment);
    //    _logger.LogInformation("Endpoints context length: {Length} chars", endpointsContext.Length);

    //    // Prepare kernel arguments
    //    var kernelArguments = new KernelArguments
    //    {
    //        ["files_manifest"] = string.Join("\n", manifestPaths.Select(p => $"- {p}")),
    //        ["env"] = environment,
    //        ["endpoints_context"] = endpointsContext,
    //        ["file_roles_json"] = JsonSerializer.Serialize(fileRoles ?? new Dictionary<string, string>())
    //    };

    //    _logger.LogInformation("=== Kernel Arguments ===");
    //    _logger.LogInformation("files_manifest: {FilesManifest}", kernelArguments["files_manifest"]);
    //    _logger.LogInformation("env: {Environment}", kernelArguments["env"]);
    //    _logger.LogInformation("endpoints_context preview: {ContextPreview}",
    //        endpointsContext.Length > 100 ? endpointsContext.Substring(0, 100) + "..." : endpointsContext);
    //    _logger.LogInformation("file_roles_json: {FileRoles}", kernelArguments["file_roles_json"]);

    //    // Add role-specific contexts to kernel arguments
    //    _logger.LogInformation("=== Role Contexts Passed to Prompt ===");
    //    foreach (var roleContext in roleContexts)
    //    {
    //        var argKey = $"{roleContext.Key.ToLower()}_context";
    //        kernelArguments[argKey] = roleContext.Value;
    //        _logger.LogInformation("{Role} context:", roleContext.Key);
    //        var lines = roleContext.Value.Split('\n');
    //        foreach (var line in lines.Take(3))
    //        {
    //            _logger.LogInformation("  {Line}", line);
    //        }
    //        if (lines.Length > 3)
    //        {
    //            _logger.LogInformation("  ... ({Count} more lines)", lines.Length - 3);
    //        }
    //    }

    //    try
    //    {
    //        _logger.LogInformation("=== Invoking RunPack Plugin ===");

    //        // Use the semantic kernel to invoke the RunPack prompt-based plugin
    //        var result = await _semanticKernelService.Kernel.InvokeAsync("RunPack", "GenerateFiles", kernelArguments);

    //        _logger.LogInformation("✓ RunPack plugin invocation completed");

    //        // Log the full result to see the content
    //        var resultValue = result.GetValue<string>() ?? "";
    //        _logger.LogInformation("=== RunPack GenerateFiles Result ===");
    //        _logger.LogInformation("Result length: {Length} characters", resultValue.Length);
    //        _logger.LogInformation("Full result: {Result}", resultValue);

    //        // Parse the result to extract file list
    //        var files = ParseGeneratedFiles(resultValue);
    //        _logger.LogInformation("✓ Parsed {Count} files from result", files.Count);

    //        // Add files to RunPackBuilder
    //        foreach (var file in files)
    //        {
    //            _logger.LogInformation("Adding file: {Path} ({ContentLength} chars)", file.Path, file.Content?.Length ?? 0);
    //            _runPackBuilder.Add(file.Path, file.Content ?? "");
    //        }

    //        // Generate ZIP using RunPackBuilder
    //        var zipBytes = _runPackBuilder.ToZip();
    //        _logger.LogInformation("✓ ZIP generated using RunPackBuilder: {Size} bytes", zipBytes.Length);

    //        return zipBytes;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "❌ Error in RunPack generation workflow");

    //        // Fallback: create a minimal zip with error message
    //        _runPackBuilder.Add("error.txt", $"Error generating files: {ex.Message}");
    //        return _runPackBuilder.ToZip();
    //    }
    //}


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

        // Add role-specific contexts to kernel arguments
        _logger.LogInformation("=== Role Contexts Passed to Prompt ===");
        foreach (var roleContext in roleContexts)
        {
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
            _logger.LogInformation("=== Generating Fake RunPack Data ===");

            // Generate fake data instead of calling LLM via Semantic Kernel
            var fakeResponse = GenerateFakeFilesResponse(manifestPaths, environment, fileRoles, roleContexts);
            var resultValue = JsonSerializer.Serialize(fakeResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true
            });

            _logger.LogInformation("✓ Fake data generation completed");
            _logger.LogInformation("=== Fake RunPack GenerateFiles Result ===");
            _logger.LogInformation("Result length: {Length} characters", resultValue.Length);
            _logger.LogInformation("Full result: {Result}", resultValue);

            // Parse the result to extract file list
            var files = ParseGeneratedFiles(resultValue);
            _logger.LogInformation("✓ Parsed {Count} files from result", files.Count);

            // Add files to RunPackBuilder
            foreach (var file in files)
            {
                _logger.LogInformation("Adding file: {Path} ({ContentLength} chars)", file.Path, file.Content?.Length ?? 0);
                _runPackBuilder.Add(file.Path, file.Content ?? "");
            }

            // Generate ZIP using RunPackBuilder
            var zipBytes = _runPackBuilder.ToZip();
            _logger.LogInformation("✓ ZIP generated using RunPackBuilder: {Size} bytes", zipBytes.Length);

            return zipBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in RunPack generation workflow");

            // Fallback: create a minimal zip with error message
            _runPackBuilder.Add("error.txt", $"Error generating files: {ex.Message}");
            return _runPackBuilder.ToZip();
        }
    }

    private GeneratedFilesResponse GenerateFakeFilesResponse(
        IReadOnlyList<string> manifestPaths,
        string environment,
        Dictionary<string, string> fileRoles,
        Dictionary<string, string> roleContexts)
    {
        var fakeFiles = new List<GeneratedFile>();

        // Generate files based on the manifest paths
        foreach (var path in manifestPaths)
        {
            var role = fileRoles.GetValueOrDefault(path, DetermineFileRole(path));
            var content = GenerateFakeContent(path, role, environment, roleContexts);
            
            fakeFiles.Add(new GeneratedFile
            {
                Path = path,
                Content = content,
                Role = role,
                Size = content.Length
            });
        }

        // If no manifest paths, generate some default test files
        if (!fakeFiles.Any())
        {
            fakeFiles.AddRange(GenerateDefaultTestFiles(environment, roleContexts));
        }

        return new GeneratedFilesResponse
        {
            Files = fakeFiles,
            Summary = $"Generated {fakeFiles.Count} fake test files for {environment} environment",
            TotalFiles = fakeFiles.Count
        };
    }

    private List<GeneratedFile> GenerateDefaultTestFiles(string environment, Dictionary<string, string> roleContexts)
    {
        var defaultFiles = new List<GeneratedFile>();

        // Generate AUTH test files if AUTH context exists
        if (roleContexts.ContainsKey("AUTH"))
        {
            defaultFiles.Add(new GeneratedFile
            {
                Path = "tests/auth/auth-test.json",
                Content = GenerateAuthTestContent(environment),
                Role = "AUTH",
                Size = 0
            });
        }

        // Generate SMOKE test files
        defaultFiles.Add(new GeneratedFile
        {
            Path = "tests/smoke/smoke-test.json",
            Content = GenerateSmokeTestContent(environment),
            Role = "SMOKE",
            Size = 0
        });

        // Generate CRUD test files if CRUD context exists
        if (roleContexts.ContainsKey("CRUD"))
        {
            defaultFiles.Add(new GeneratedFile
            {
                Path = "tests/crud/crud-test.json",
                Content = GenerateCrudTestContent(environment),
                Role = "CRUD",
                Size = 0
            });
        }

        // Update sizes
        foreach (var file in defaultFiles)
        {
            file.Size = file.Content.Length;
        }

        return defaultFiles;
    }

    private string GenerateFakeContent(string path, string role, string environment, Dictionary<string, string> roleContexts)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        
        return extension switch
        {
            ".json" => GenerateJsonTestContent(path, role, environment),
            ".sh" => GenerateShellScriptContent(path, role, environment),
            ".js" => GenerateJavaScriptContent(path, role, environment),
            ".cs" => GenerateCSharpContent(path, role, environment),
            _ => GenerateFallbackContent(role, environment)
        };
    }

    private string GenerateJsonTestContent(string path, string role, string environment)
    {
        var testName = Path.GetFileNameWithoutExtension(path);
        return $@"{{
  ""$schema"": ""https://chapi.dev/schemas/chapi-test-1.json"",
  ""name"": ""{testName}"",
  ""description"": ""Generated {role} test for {environment} environment"",
  ""environment"": ""{environment}"",
  ""baseUrl"": ""{{{{ BASE_URL }}}}"",
  ""headers"": {{
    ""Content-Type"": ""application/json"",
    ""Authorization"": ""Bearer {{{{ TOKEN }}}}"",
  }},
  ""tests"": [
    {{
      ""name"": ""Test {role} functionality"",
      ""request"": {{
        ""method"": ""GET"",
        ""url"": ""/api/test"",
        ""headers"": {{}}
      }},
      ""response"": {{
        ""status"": 200,
        ""headers"": {{
          ""content-type"": ""application/json""
        }},
        ""body"": {{
          ""message"": ""Test passed""
        }}
      }}
    }}
  ]
}}";
    }

    private string GenerateAuthTestContent(string environment)
    {
        return $@"{{
  ""$schema"": ""https://chapi.dev/schemas/chapi-test-1.json"",
  ""name"": ""auth-test"",
  ""description"": ""Authentication tests for {environment} environment"",
  ""environment"": ""{environment}"",
  ""baseUrl"": ""{{{{ BASE_URL }}}}"",
  ""tests"": [
    {{
      ""name"": ""Test protected endpoint requires authentication"",
      ""request"": {{
        ""method"": ""GET"",
        ""url"": ""/api/protected"",
        ""headers"": {{}}
      }},
      ""response"": {{
        ""status"": 401,
        ""body"": {{
          ""error"": ""Unauthorized""
        }}
      }}
    }},
    {{
      ""name"": ""Test protected endpoint with valid token"",
      ""request"": {{
        ""method"": ""GET"",
        ""url"": ""/api/protected"",
        ""headers"": {{
          ""Authorization"": ""Bearer {{{{ TOKEN }}}}"",
        }}
      }},
      ""response"": {{
        ""status"": 200,
        ""body"": {{
          ""message"": ""Access granted""
        }}
      }}
    }}
  ]
}}";
    }

    private string GenerateSmokeTestContent(string environment)
    {
        return $@"{{
  ""$schema"": ""https://chapi.dev/schemas/chapi-test-1.json"",
  ""name"": ""smoke-test"",
  ""description"": ""Smoke tests for {environment} environment"",
  ""environment"": ""{environment}"",
  ""baseUrl"": ""{{{{ BASE_URL }}}}"",
  ""tests"": [
    {{
      ""name"": ""Health check"",
      ""request"": {{
        ""method"": ""GET"",
        ""url"": ""/health"",
        ""headers"": {{}}
      }},
      ""response"": {{
        ""status"": 200,
        ""body"": {{
          ""status"": ""healthy""
        }}
      }}
    }},
    {{
      ""name"": ""API version check"",
      ""request"": {{
        ""method"": ""GET"",
        ""url"": ""/api/version"",
        ""headers"": {{}}
      }},
      ""response"": {{
        ""status"": 200
      }}
    }}
  ]
}}";
    }

    private string GenerateCrudTestContent(string environment)
    {
        return $@"{{
  ""$schema"": ""https://chapi.dev/schemas/chapi-test-1.json"",
  ""name"": ""crud-test"",
  ""description"": ""CRUD operation tests for {environment} environment"",
  ""environment"": ""{environment}"",
  ""baseUrl"": ""{{{{ BASE_URL }}}}"",
  ""headers"": {{
    ""Content-Type"": ""application/json"",
    ""Authorization"": ""Bearer {{{{ TOKEN }}}}"",
  ""tests"": [
    {{
      ""name"": ""Create resource"",
      ""request"": {{
        ""method"": ""POST"",
        ""url"": ""/api/resources"",
        ""body"": {{
          ""name"": ""Test Resource"",
          ""description"": ""A test resource""
        }}
      }},
      ""response"": {{
        ""status"": 201,
        ""body"": {{
          ""id"": ""@isString"",
          ""name"": ""Test Resource""
        }}
      }}
    }},
    {{
      ""name"": ""Read resource"",
      ""request"": {{
        ""method"": ""GET"",
        ""url"": ""/api/resources/{{{{ tests[0].response.body.id }}}}"",
        ""headers"": {{}}
      }},
      ""response"": {{
        ""status"": 200,
        ""body"": {{
          ""id"": ""{{{{ tests[0].response.body.id }}}}"",
          ""name"": ""Test Resource""
        }}
      }}
    }}
  ]
}}";
    }

    private string GenerateShellScriptContent(string path, string role, string environment)
    {
        return $@"#!/bin/bash
# Generated {role} script for {environment} environment
# File: {path}

set -euo pipefail

echo ""Running {role} tests for {environment} environment...""

# Set base URL if not provided
BASE_URL=${{BASE_URL:-""http://localhost:8080""}}
echo ""Using base URL: $BASE_URL""

# Run the test
echo ""Executing test logic for {role}...""

echo ""✓ {role} test completed successfully""
";
    }

    private string GenerateJavaScriptContent(string path, string role, string environment)
    {
        return $@"// Generated {role} JavaScript file for {environment} environment
// File: {path}

const config = {{
    environment: '{environment}',
    baseUrl: process.env.BASE_URL || 'http://localhost:8080',
    role: '{role}'
}};

console.log(`Running ${{config.role}} tests for ${{config.environment}} environment`);
console.log(`Base URL: ${{config.baseUrl}}`);

// TODO: Implement {role} functionality
export default config;
";
    }

    private string GenerateCSharpContent(string path, string role, string environment)
    {
        var className = Path.GetFileNameWithoutExtension(path);
        if (string.IsNullOrWhiteSpace(className)) className = "GeneratedClass";
        return $@"// Generated {role} C# file for {environment} environment
// File: {path}

using System;

namespace Chapi.Generated
{{
    public class {className}
    {{
        public string Environment {{ get; }} = ""{environment}"";
        public string Role {{ get; }} = ""{role}"";

       }}
    }}
}}
";
    }

    private List<(string Path, string Content)> ParseGeneratedFiles(string result)
    {
        var files = new List<(string Path, string Content)>();

        // First, extract JSON from markdown code blocks if present
        var cleanedResult = ExtractJsonFromMarkdown(result);
        _logger.LogInformation("=== Cleaned result length: {Length} characters ===", cleanedResult.Length);

        try
        {
            _logger.LogInformation("=== Attempting to parse as GeneratedFilesResponse ===");

            // Try to parse as strong typed GeneratedFilesResponse first
            var response = JsonSerializer.Deserialize<GeneratedFilesResponse>(cleanedResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (response?.Files != null)
            {
                foreach (var file in response.Files)
                {
                    if (!string.IsNullOrEmpty(file.Path) && !string.IsNullOrEmpty(file.Content))
                    {
                        files.Add((file.Path, file.Content));
                        _logger.LogInformation("✓ Parsed file from GeneratedFilesResponse: {Path} (role: {Role}, size: {Size})",
                            file.Path, file.Role ?? "unknown", file.Size ?? file.Content.Length);
                    }
                }

                if (!string.IsNullOrEmpty(response.Summary))
                {
                    _logger.LogInformation("Response summary: {Summary}", response.Summary);
                }

                _logger.LogInformation("✓ Successfully parsed {Count} files using strong type", files.Count);
                return files;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Failed to parse as GeneratedFilesResponse: {Error}", ex.Message);
        }

        try
        {
            _logger.LogInformation("=== Attempting to parse as simple JSON array ===");

            // Try to parse as JSON array (fallback)
            var jsonArray = JsonSerializer.Deserialize<JsonElement[]>(cleanedResult);
            if (jsonArray != null)
            {
                foreach (var item in jsonArray)
                {
                    if (item.TryGetProperty("path", out var pathProp) &&
                        item.TryGetProperty("content", out var contentProp))
                    {
                        var path = pathProp.GetString() ?? "";
                        var content = contentProp.GetString() ?? "";
                        files.Add((path, content));
                        _logger.LogInformation("✓ Parsed file from JSON array: {Path}", path);
                    }
                }
            }

            if (files.Any())
            {
                _logger.LogInformation("✓ Successfully parsed {Count} files using JSON array fallback", files.Count);
                return files;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Failed to parse as JSON array: {Error}", ex.Message);
        }

        // If JSON parsing fails, try to parse as text format
        _logger.LogInformation("=== Attempting to parse as text format ===");
        files = ParseTextFormat(result);

        if (files.Any())
        {
            _logger.LogInformation("✓ Successfully parsed {Count} files using text format fallback", files.Count);
        }
        else
        {
            _logger.LogWarning("❌ Failed to parse any files from result");
        }

        return files;
    }

    private string ExtractJsonFromMarkdown(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Look for JSON code blocks: ```json ... ``` or ``` ... ```
        var jsonBlockPattern = @"```(?:json)?\s*\n(.*?)\n```";
        var match = Regex.Match(input, jsonBlockPattern,
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var extractedJson = match.Groups[1].Value.Trim();
            _logger.LogInformation("✓ Extracted JSON from markdown code block ({Length} chars)", extractedJson.Length);
            return extractedJson;
        }

        // If no code block found, return original
        _logger.LogInformation("No markdown code block found, using original result");
        return input;
    }

    private List<(string Path, string Content)> ParseTextFormat(string result)
    {
        var files = new List<(string Path, string Content)>();

        // Look for patterns like:
        // File: path/to/file.sh
        // Content: 
        // ... file content ...

        var lines = result.Split('\n');
        string? currentPath = null;
        var currentContent = new List<string>();
        bool inContent = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("File:", StringComparison.OrdinalIgnoreCase))
            {
                // Save previous file if exists
                if (!string.IsNullOrEmpty(currentPath) && currentContent.Any())
                {
                    files.Add((currentPath, string.Join("\n", currentContent)));
                    _logger.LogInformation("Parsed file from text format: {Path}", currentPath);
                }

                // Start new file
                currentPath = line.Substring(5).Trim();
                currentContent.Clear();
                inContent = false;
            }
            else if (line.StartsWith("Content:", StringComparison.OrdinalIgnoreCase))
            {
                inContent = true;
            }
            else if (inContent && !string.IsNullOrEmpty(currentPath))
            {
                currentContent.Add(line);
            }
        }

        // Save last file
        if (!string.IsNullOrEmpty(currentPath) && currentContent.Any())
        {
            files.Add((currentPath, string.Join("\n", currentContent)));
            _logger.LogInformation("Parsed file from text format: {Path}", currentPath);
        }

        return files;
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
