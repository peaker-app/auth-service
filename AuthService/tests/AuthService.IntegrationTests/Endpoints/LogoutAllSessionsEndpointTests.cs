using System.Net;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class LogoutAllSessionsEndpointTests(AuthServiceApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task LogoutAll_RevokesTheRefreshTokenOfEveryDevice()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair phone = await _client.LoginWithTokensAsync(user.Email);
        TokenPair laptop = await _client.LoginWithTokensAsync(user.Email);

        using HttpResponseMessage logout = await _client.LogoutAllSessionsAsync(laptop.AccessToken);
        using HttpResponseMessage fromPhone = await _client.RefreshAsync(phone.RefreshToken);
        using HttpResponseMessage fromLaptop = await _client.RefreshAsync(laptop.RefreshToken);

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fromPhone.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        fromLaptop.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutAll_WithoutToken_ReturnsUnauthorized()
    {
        using HttpResponseMessage response = await _client.LogoutAllSessionsAsync(null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutAll_LeavesTheSessionsOfOtherAccountsAlone()
    {
        RegisteredUser mine = await _client.RegisterUserAsync();
        RegisteredUser theirs = await _client.RegisterUserAsync();
        TokenPair myTokens = await _client.LoginWithTokensAsync(mine.Email);
        TokenPair theirTokens = await _client.LoginWithTokensAsync(theirs.Email);

        (await _client.LogoutAllSessionsAsync(myTokens.AccessToken)).EnsureSuccessStatusCode();

        using HttpResponseMessage refresh = await _client.RefreshAsync(theirTokens.RefreshToken);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
