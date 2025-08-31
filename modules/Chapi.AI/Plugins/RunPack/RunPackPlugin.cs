using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Chapi.AI.Utilities;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;

namespace Chapi.AI.Plugins.RunPack
{
    public sealed class RunPackPlugin
    {
        private readonly RunPackBuilder _builder;

        public RunPackPlugin(RunPackBuilder builder) 
        {
            _builder = builder;
        }

        [KernelFunction]
        [Description("Add a file to the downloadable run pack")]
        public string AddFile(
            [Description("File path to create, e.g., tests/email-service/smoke.json")]
            [Required] string path,
            [Description("UTF-8 content of the file. For test files, this MUST be JSON in the 'chapi-test-1' schema.")]
            [Required] string content)
        {
            _builder.Add(path, content ?? "");
            return $"added:{path}";
        }
    }
}
