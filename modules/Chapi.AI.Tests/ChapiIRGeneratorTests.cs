using System.Collections.Generic;
using System.Text.Json;
using Chapi.AI.Dto;
using Chapi.AI.Services;
using Chapi.AI.Services.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Chapi.AI.Tests
{
    public class ChapiIRGeneratorTests
    {
        [Fact]
        public void GenerateTokenStep_HasContentTypeHeader()
        {
            var logger = new NullLogger<ChapiIRGenerator>();
            var tokenGenMock = new Mock<ITokenGenerator>();
            tokenGenMock.Setup(t => t.GetAccessTokenAsync(It.IsAny<AuthProfile>(), default)).ReturnsAsync((string?)null);

            var gen = new ChapiIRGenerator(logger, tokenGenMock.Object);

            var endpoint = new SelectedEndpoint { Method = "GET", Path = "/api/test", RequiresAuth = true };
            var auth = new AuthProfile { Type = "OIDC_CLIENT_CREDENTIALS", Config = new Dictionary<string, string> { ["TOKEN_URL"] = "https://token" } };
            var options = new TestGenOptions { IncludeForbidden = false };

            var json = gen.GenerateTestsJsonContent(endpoint, auth, options);
            using var doc = JsonDocument.Parse(json);

            // Find token step
            var steps = doc.RootElement.GetProperty("suite").GetProperty("steps").EnumerateArray();
            JsonElement? tokenStep = null;
            foreach (var s in steps)
            {
                if (s.GetProperty("type").GetString() == "http" && s.GetProperty("id").GetString()?.StartsWith("get_token") == true)
                {
                    tokenStep = s;
                    break;
                }
            }

            Assert.True(tokenStep.HasValue, "token step not found");
            var headers = tokenStep.Value.GetProperty("request").GetProperty("headers");
            Assert.True(headers.TryGetProperty("Content-Type", out _));
        }

        [Fact]
        public void ForbiddenStep_EmitsTokenStepForForbiddenProfile()
        {
            var logger = new NullLogger<ChapiIRGenerator>();
            var tokenGenMock = new Mock<ITokenGenerator>();
            tokenGenMock.Setup(t => t.GetAccessTokenAsync(It.IsAny<AuthProfile>(), default)).ReturnsAsync((string?)null);

            var gen = new ChapiIRGenerator(logger, tokenGenMock.Object);

            var endpoint = new SelectedEndpoint { Method = "GET", Path = "/api/forbid", RequiresAuth = true };
            var auth = new AuthProfile { Type = "BEARER" };
            var forbidden = new AuthProfile { Type = "OIDC_CLIENT_CREDENTIALS", Config = new Dictionary<string, string> { ["TOKEN_URL"] = "https://token" } };
            var options = new TestGenOptions { IncludeForbidden = true, ForbiddenAuthProfile = forbidden };

            var json = gen.GenerateTestsJsonContent(endpoint, auth, options);
            using var doc = JsonDocument.Parse(json);

            var steps = doc.RootElement.GetProperty("suite").GetProperty("steps").EnumerateArray();
            bool foundForbidden = false;
            foreach (var s in steps)
            {
                if (s.GetProperty("id").GetString() == "endpoint_forbidden")
                {
                    if (s.TryGetProperty("auth", out var authProp))
                    {
                        var tokenStep = authProp.GetProperty("token_step").GetString();
                        Assert.Equal("get_token_forbidden", tokenStep);
                        foundForbidden = true;
                    }
                }
            }

            Assert.True(foundForbidden, "Forbidden step with auth override not found");
        }
    }
}
