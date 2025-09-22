using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Chapi.AI.Dto;
using Chapi.AI.Services.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Chapi.AI.Tests
{
    public class TokenGeneratorTests
    {
        [Fact]
        public async Task ClientCredentials_ThrowsWhenMissingSecrets()
        {
            var factory = new Mock<IHttpClientFactory>();
            var logger = new NullLogger<TokenGenerator>();
            var tg = new TokenGenerator(factory.Object, logger);

            var profile = new AuthProfile { Type = "OIDC_CLIENT_CREDENTIALS", Config = new Dictionary<string, string>() };

            await Assert.ThrowsAsync<System.InvalidOperationException>(async () => await tg.GetAccessTokenAsync(profile));
        }
    }
}
