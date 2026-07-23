using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class LoginEndpointTests(AuthServiceApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        string email = await _client.RegisterUserAsync();

        TokenPair tokens = await _client.LoginAsync(email);

        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        string email = await _client.RegisterUserAsync();

        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = "the-wrong-password" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_AfterFiveFailedAttempts_LocksAccount()
    {
        string email = await _client.RegisterUserAsync();
        await _client.FailLoginsAsync(email, 5);

        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = ApiTestHelpers.DefaultPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
