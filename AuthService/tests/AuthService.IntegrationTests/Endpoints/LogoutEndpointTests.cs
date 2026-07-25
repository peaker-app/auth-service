using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class LogoutEndpointTests(AuthServiceApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Logout_WithValidToken_RevokesRefreshToken()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);

        using HttpResponseMessage logout = await _client.LogoutAsync(tokens.AccessToken, tokens.RefreshToken);
        using HttpResponseMessage refresh = await _client.RefreshAsync(tokens.RefreshToken);

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithoutToken_ReturnsUnauthorized()
    {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/logout", new { refreshToken = "any-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_LeavesTheAccessTokenValidUntilItsNaturalExpiry()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);
        using HttpResponseMessage first = await _client.LogoutAsync(tokens.AccessToken, tokens.RefreshToken);
        first.EnsureSuccessStatusCode();

        using HttpResponseMessage second = await _client.LogoutAsync(tokens.AccessToken, tokens.RefreshToken);

        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
