using System.Threading.Tasks;
using AuthProfiles.Infrastructure.Services;
using AuthProfiles.Application.Dtos;
using FluentAssertions;
using Xunit;

namespace AuthProfiles.Tests;

public class AuthDetectionServiceTests
{
    [Fact]
    public async Task Detects_OAuth_From_OpenApi_SecuritySchemes()
    {
        var svc = new AuthDetectionService();
        var openApi = @"{ ""openapi"":""3.0.1"",""servers"": [{""url"":""https://api.example.com""}],
          ""components"": { ""securitySchemes"": {
            ""oauth"": { ""type"":""oauth2"",""flows"": { ""clientCredentials"": { ""tokenUrl"": ""https://api.example.com/connect/token"" } } }
          } },
          ""paths"": {}
        }";
        var res = await svc.DetectAsync(new DetectTokenRequest(null, null, null, openApi, null), default);
        res.Should().Contain(c => c.Type == "oauth2_client_credentials" && c.Endpoint == "/connect/token");
    }
}
