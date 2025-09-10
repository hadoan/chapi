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
    // Stub IApiSpecRepository that returns no stored specs for this unit test
    var repo = new TestSpecRepo();
    var svc = new AuthDetectionService(repo);
    var openApi = @"{ ""openapi"":""3.0.1"",""servers"": [{""url"":""https://api.example.com""}],
          ""components"": { ""securitySchemes"": {
            ""oauth"": { ""type"":""oauth2"",""flows"": { ""clientCredentials"": { ""tokenUrl"": ""https://api.example.com/connect/token"" } } }
          } },
          ""paths"": {}
        }";
    var res = await svc.DetectAsync(new DetectTokenRequest(null, null, null), default);
    res.Should().Contain(c => c.Type == "oauth2_client_credentials" && c.Endpoint == "/connect/token");
  }

  private class TestSpecRepo : Chapi.ApiSpecs.Domain.IApiSpecRepository
  {
    public Task<ApiSpec?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ApiSpec?>(null);
    public Task<IEnumerable<ApiSpec>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ApiSpec>>(Array.Empty<ApiSpec>());
    public Task<ApiSpec> AddAsync(ApiSpec entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<ApiSpec> UpdateAsync(ApiSpec entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<ApiSpec?> FindByShaAsync(string sha) => Task.FromResult<ApiSpec?>(null);
    public Task<Guid> UpsertAsync(ApiSpec spec) => throw new NotImplementedException();
    public Task<IEnumerable<string>> GetRawJsonByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
  }
}
