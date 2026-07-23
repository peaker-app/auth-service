using System.Net;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class RefreshEndpointTests(AuthServiceApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Refresh_WithValidToken_RotatesTokens()
    {
        string email = await _client.RegisterUserAsync();
        TokenPair initial = await _client.LoginAsync(email);

        TokenPair rotated = await _client.RefreshTokensAsync(initial.RefreshToken);

        rotated.RefreshToken.Should().NotBe(initial.RefreshToken);
    }

    [Fact]
    public async Task Refresh_ReusingRevokedToken_RevokesWholeChain()
    {
        string email = await _client.RegisterUserAsync();
        TokenPair initial = await _client.LoginAsync(email);
        TokenPair rotated = await _client.RefreshTokensAsync(initial.RefreshToken);

        using HttpResponseMessage reuse = await _client.RefreshAsync(initial.RefreshToken);
        using HttpResponseMessage cascade = await _client.RefreshAsync(rotated.RefreshToken);

        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        cascade.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
