using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Chapi.AI.Dto;

namespace Chapi.AI.Services
{
    public class TestGenService : ITestGenService
    {
        private readonly ILogger<TestGenService> _logger;
        private readonly ITestGenCardGenerator _cardGenerator;
        private readonly ITestGenFileGenerator _fileGenerator;
        private readonly ITestGenDatabaseService _databaseService;

        public TestGenService(
            ILogger<TestGenService> logger,
            ITestGenCardGenerator cardGenerator,
            ITestGenFileGenerator fileGenerator,
            ITestGenDatabaseService databaseService)
        {
            _logger = logger;
            _cardGenerator = cardGenerator;
            _fileGenerator = fileGenerator;
            _databaseService = databaseService;
        }

        public async Task<TestGenResponse> GenerateTestsAsync(TestGenInput input)
        {
            _logger.LogInformation("Generating tests for endpoint {Method} {Path}",
                input.SelectedEndpoint.Method, input.SelectedEndpoint.Path);

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var conversationId = input.Chat.ConversationId ?? Guid.NewGuid().ToString();
            var messageId = Guid.NewGuid().ToString();

            var card = _cardGenerator.GenerateCard(input);

            List<TestGenFile>? files = null;
            if (input.Mode == "FILES")
            {
                files = _fileGenerator.GenerateFiles(input.SelectedEndpoint, input.AuthProfile, input.Options);
            }

            // Create database operations using the database service (now saves to database)
            var dbOps = await _databaseService.CreateDatabaseOperations(input, card, conversationId, messageId, timestamp, files);

            return new TestGenResponse
            {
                Role = "Chapi",
                Card = card,
                Files = files,
                DbOps = dbOps
            };
        }
    }
}
