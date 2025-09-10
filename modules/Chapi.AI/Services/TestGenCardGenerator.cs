using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Chapi.AI.Dto;

namespace Chapi.AI.Services
{
    public interface ITestGenCardGenerator
    {
        ChapiCard GenerateCard(TestGenInput input);
    }

    public class TestGenCardGenerator : ITestGenCardGenerator
    {
        private readonly ILogger<TestGenCardGenerator> _logger;

        public TestGenCardGenerator(ILogger<TestGenCardGenerator> logger)
        {
            _logger = logger;
        }

        public ChapiCard GenerateCard(TestGenInput input)
        {
            var endpoint = input.SelectedEndpoint;
            var authProfile = input.AuthProfile;

            var heading = $"{endpoint.Method} {endpoint.Path} — tests-as-data";

            var plan = GeneratePlan();
            var files = GenerateFileEntries(endpoint, authProfile, input.Options);
            var actions = new List<string> { "DOWNLOAD_RUN_PACK", "RUN_CLOUD", "ADD_NEGATIVES" };

            return new ChapiCard
            {
                Role = "Chapi",
                Heading = heading,
                Plan = plan,
                Files = files,
                Actions = actions
            };
        }

        private List<string> GeneratePlan()
        {
            return new List<string>
            {
                "Create tests.json (IR v0.1) with auth + unauthorized",
                "Emit .env.example for selected auth profile",
                "Provide tiny bash/PowerShell runners",
                "Preflight validations: NoRootUrl, HasTestsJson, HasRunner"
            };
        }

        private List<FileEntry> GenerateFileEntries(SelectedEndpoint endpoint, AuthProfile authProfile, TestGenOptions options)
        {
            return new List<FileEntry>
            {
                new FileEntry
                {
                    Path = "tests.json",
                    AddedLines = EstimateTestsJsonLines(endpoint, authProfile, options)
                },
                new FileEntry
                {
                    Path = ".env.example",
                    AddedLines = EstimateEnvFileLines(authProfile)
                },
                new FileEntry
                {
                    Path = "run.sh",
                    AddedLines = EstimateRunnerLines()
                },
                new FileEntry
                {
                    Path = "run.ps1",
                    AddedLines = EstimateRunnerLines()
                },
                new FileEntry
                {
                    Path = "README.md",
                    AddedLines = 8
                }
            };
        }

        private int EstimateTestsJsonLines(SelectedEndpoint endpoint, AuthProfile authProfile, TestGenOptions options)
        {
            var baseLines = 40; // Basic structure

            // Token step for auth types that need it
            if (authProfile.Type == "OIDC_CLIENT_CREDENTIALS" || authProfile.Type == "OIDC_PASSWORD" || authProfile.Type == "CUSTOM_SCRIPT")
            {
                baseLines += 20;
            }

            // Happy path step
            baseLines += 15;

            // Unauthorized step
            if (endpoint.RequiresAuth)
            {
                baseLines += 10;
            }

            // Forbidden step
            if (options.IncludeForbidden && endpoint.RequiresAuth)
            {
                baseLines += 10;
            }

            return Math.Min(baseLines, 140);
        }

        private int EstimateEnvFileLines(AuthProfile authProfile)
        {
            return authProfile.Type switch
            {
                "OIDC_CLIENT_CREDENTIALS" => 25,
                "OIDC_PASSWORD" => 20,
                "BASIC" => 15,
                "BEARER" => 12,
                "API_KEY" => 12,
                "CUSTOM_SCRIPT" => 10,
                _ => 8
            };
        }

        private int EstimateRunnerLines()
        {
            return 50; // Bash and PowerShell runners are similar in complexity
        }
    }
}
