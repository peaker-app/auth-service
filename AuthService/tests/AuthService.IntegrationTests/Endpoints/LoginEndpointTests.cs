using System.Net;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class LoginEndpointTests(AuthServiceApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_WithEmailIdentifier_ReturnsTokens()
    {
        RegisteredUser user = await _client.RegisterUserAsync();

        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);

        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithUsernameIdentifier_ReturnsTokens()
    {
        RegisteredUser user = await _client.RegisterUserAsync();

        TokenPair tokens = await _client.LoginWithTokensAsync(user.Username);

        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithUsernameInDifferentCasing_ReturnsTokens()
    {
        RegisteredUser user = await _client.RegisterUserAsync();

        TokenPair tokens = await _client.LoginWithTokensAsync(user.Username.ToUpperInvariant());

        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithUnknownIdentifier_ReturnsUnauthorized()
    {
        using HttpResponseMessage response = await _client.LoginAsync(
            ApiTestHelpers.UniqueUsername(), ApiTestHelpers.DefaultPassword);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        RegisteredUser user = await _client.RegisterUserAsync();

        using HttpResponseMessage response = await _client.LoginAsync(user.Email, "the-wrong-password");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_AfterFiveFailedAttempts_LocksAccount()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        await _client.FailLoginsAsync(user.Email, 5);

        using HttpResponseMessage response = await _client.LoginAsync(
            user.Email, ApiTestHelpers.DefaultPassword);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
