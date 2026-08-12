using System.Net;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
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

    [Fact]
    public async Task JsonWebKeySet_ExposesTheKeysMember()
    {
        using HttpResponseMessage response = await _client.GetAsync("/.well-known/jwks.json");
        string body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("\"keys\"");
    }

    [Fact]
    public async Task JsonWebKeySet_IdentifiesEveryKeyByItsKeyId()
    {
        using HttpResponseMessage response = await _client.GetAsync("/.well-known/jwks.json");
        JsonWebKeySet keySet = new(await response.Content.ReadAsStringAsync());

        keySet.Keys.Should().NotBeEmpty()
            .And.OnlyContain(key => !string.IsNullOrWhiteSpace(key.Kid));
    }

    [Fact]
    public async Task OpenIdConfiguration_UsesTheSnakeCaseMembersTheDiscoveryContractRequires()
    {
        using HttpResponseMessage response = await _client.GetAsync("/.well-known/openid-configuration");
        string body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("\"issuer\"")
            .And.Contain("\"jwks_uri\"")
            .And.Contain("\"id_token_signing_alg_values_supported\"")
            .And.Contain("\"response_types_supported\"")
            .And.Contain("\"subject_types_supported\"");
    }
}
