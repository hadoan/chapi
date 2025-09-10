using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Chapi.AI.Dto;

namespace Chapi.AI.Services
{
    public interface ITestGenValidationService
    {
        List<RunPackValidationRow> CreateValidations(string runPackId, List<TestGenFile> files, SelectedEndpoint endpoint, TestGenOptions options, string timestamp);
        string ClassifyFileRole(string path);
    }

    public class TestGenValidationService : ITestGenValidationService
    {
        private readonly ILogger<TestGenValidationService> _logger;

        public TestGenValidationService(ILogger<TestGenValidationService> logger)
        {
            _logger = logger;
        }

        public List<RunPackValidationRow> CreateValidations(string runPackId, List<TestGenFile> files, SelectedEndpoint endpoint, TestGenOptions options, string timestamp)
        {
            var validations = new List<RunPackValidationRow>
            {
                CreateValidation(runPackId, "*", "NoRootUrl", ValidateNoRootUrl(files), timestamp),
                CreateValidation(runPackId, "tests.json", "HasTestsJson", files.Any(f => f.Path == "tests.json"), timestamp),
                CreateValidation(runPackId, "*", "HasRunner", files.Any(f => f.Path == "run.sh" || f.Path == "run.ps1"), timestamp)
            };

            if (endpoint.RequiresAuth)
            {
                validations.Add(CreateValidation(runPackId, "tests.json", "AuthVariantsPresent",
                    files.Any(f => f.Path == "tests.json" && f.Content.Contains("endpoint_unauthorized")), timestamp));
            }

            if (options.IncludeForbidden && endpoint.RequiresAuth)
            {
                validations.Add(CreateValidation(runPackId, "tests.json", "AuthThreeCases",
                    ValidateAuthThreeCases(files), timestamp));
            }

            return validations;
        }

        public string ClassifyFileRole(string path)
        {
            if (path == "tests.json") return "ENDPOINT"; // Primary test definition in v2
            if (path.Contains(".env")) return "ENV";
            if (path.Contains("run.sh") || path.Contains("run.ps1")) return "RUNNER";
            if (path.Contains("README.md")) return "DOCS";
            return "SMOKE";
        }

        private RunPackValidationRow CreateValidation(string runPackId, string filePath, string rule, bool passed, string timestamp)
        {
            return new RunPackValidationRow
            {
                Id = System.Guid.NewGuid().ToString(),
                RunpackId = runPackId,
                FilePath = filePath,
                Rule = rule,
                Passed = passed,
                CreatedAt = timestamp
            };
        }

        private bool ValidateNoRootUrl(List<TestGenFile> files)
        {
            var testsJsonFile = files.FirstOrDefault(f => f.Path == "tests.json");
            if (testsJsonFile == null) return false;

            // Check if URLs use {{BASE_URL}} interpolation
            return testsJsonFile.Content.Contains("{{BASE_URL}}") &&
                   !testsJsonFile.Content.Contains("http://") &&
                   !testsJsonFile.Content.Contains("https://");
        }

        private bool ValidateAuthThreeCases(List<TestGenFile> files)
        {
            var testsJsonFile = files.FirstOrDefault(f => f.Path == "tests.json");
            if (testsJsonFile == null) return false;

            var content = testsJsonFile.Content;
            // Check for required test steps
            var hasHappyPath = content.Contains("endpoint_happy");
            var hasUnauthorized = content.Contains("endpoint_unauthorized");
            var hasForbidden = content.Contains("endpoint_forbidden");

            return hasHappyPath && hasUnauthorized && hasForbidden;
        }
    }
}
