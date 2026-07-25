using System.Net;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class DeleteAccountEndpointTests(AuthServiceApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Delete_WithValidToken_ReturnsNoContent()
    {
        TokenPair tokens = await RegisterAndLoginAsync();

        using HttpResponseMessage response = await _client.DeleteAccountAsync(tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithoutToken_ReturnsUnauthorized()
    {
        using HttpResponseMessage response = await _client.DeleteAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ThenLogin_ReturnsUnauthorized()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);
        using HttpResponseMessage deletion = await _client.DeleteAccountAsync(tokens.AccessToken);
        deletion.EnsureSuccessStatusCode();

        using HttpResponseMessage login = await _client.LoginAsync(user.Email, ApiTestHelpers.DefaultPassword);

        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_RevokesTheRefreshTokensInPlay()
    {
        TokenPair tokens = await RegisterAndLoginAsync();
        using HttpResponseMessage deletion = await _client.DeleteAccountAsync(tokens.AccessToken);
        deletion.EnsureSuccessStatusCode();

        using HttpResponseMessage refresh = await _client.RefreshAsync(tokens.RefreshToken);

        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_OnAnAlreadyDeletedAccount_ReturnsConflict()
    {
        TokenPair tokens = await RegisterAndLoginAsync();
        using HttpResponseMessage first = await _client.DeleteAccountAsync(tokens.AccessToken);
        first.EnsureSuccessStatusCode();

        using HttpResponseMessage second = await _client.DeleteAccountAsync(tokens.AccessToken);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_DoesNotFreeTheEmailNorTheUsernameForReuse()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);
        using HttpResponseMessage deletion = await _client.DeleteAccountAsync(tokens.AccessToken);
        deletion.EnsureSuccessStatusCode();

        using HttpResponseMessage sameEmail = await _client.RegisterAsync(
            new RegisteredUser(user.Email, ApiTestHelpers.UniqueUsername()));
        using HttpResponseMessage sameUsername = await _client.RegisterAsync(
            new RegisteredUser(ApiTestHelpers.UniqueEmail(), user.Username));

        sameEmail.StatusCode.Should().Be(HttpStatusCode.Conflict);
        sameUsername.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<TokenPair> RegisterAndLoginAsync()
    {
        RegisteredUser user = await _client.RegisterUserAsync();

        return await _client.LoginWithTokensAsync(user.Email);
    }
}
