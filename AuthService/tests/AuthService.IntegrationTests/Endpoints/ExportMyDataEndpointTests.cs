using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class ExportMyDataEndpointTests(AuthServiceApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Export_WithoutToken_ReturnsUnauthorized()
    {
        using HttpResponseMessage response = await _client.GetAsync(
            new Uri("/api/auth/me/export", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Export_WithASession_ReturnsTheAccountAndItsIdentifiers()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);

        JsonElement body = await ExportAsync(tokens.AccessToken);

        body.GetProperty("email").GetString().Should().Be(user.Email);
        body.GetProperty("username").GetString().Should().Be(user.Username);
        body.GetProperty("status").GetString().Should().Be("Active");
    }

    [Fact]
    public async Task Export_WithASession_ListsTheActiveSessionsWithoutAnyTokenValue()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);

        JsonElement body = await ExportAsync(tokens.AccessToken);
        string raw = body.GetRawText();

        body.GetProperty("activeSessions").GetArrayLength().Should().BeGreaterThan(0);
        raw.Should().NotContain(tokens.RefreshToken);
        raw.Should().NotContain(tokens.AccessToken);
    }

    [Fact]
    public async Task Export_OfAnAdminAccount_IncludesTheRole()
    {
        RegisteredUser admin = await _client.RegisterUserAsync();
        await factory.GrantAdminAsync(await factory.FindUserIdByEmailAsync(admin.Email));
        TokenPair tokens = await _client.LoginWithTokensAsync(admin.Email);

        JsonElement body = await ExportAsync(tokens.AccessToken);

        body.GetProperty("roles").EnumerateArray()
            .Select(role => role.GetString())
            .Should().Contain("Admin");
    }

    [Fact]
    public async Task Export_NeverExposesThePasswordHash()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);

        JsonElement body = await ExportAsync(tokens.AccessToken);

        body.GetRawText().Should().NotContain("passwordHash");
    }

    private async Task<JsonElement> ExportAsync(string accessToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/auth/me/export");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
