using System.Net;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class JwksEndpointTests(AuthServiceApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Jwks_ReturnsRs256SigningKey()
    {
        using HttpResponseMessage response = await _client.GetAsync("/.well-known/jwks.json");
        string body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("RS256");
    }
}
