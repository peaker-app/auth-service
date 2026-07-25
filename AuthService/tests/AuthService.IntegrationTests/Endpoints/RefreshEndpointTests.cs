using System.Net;
using System.Security.Cryptography;
using System.Text;
using AuthService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class RefreshEndpointTests(AuthServiceApiFactory factory)
{
    private readonly AuthServiceApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Refresh_WithValidToken_RotatesTokens()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair initial = await _client.LoginWithTokensAsync(user.Email);

        TokenPair rotated = await _client.RefreshTokensAsync(initial.RefreshToken);

        rotated.RefreshToken.Should().NotBe(initial.RefreshToken);
    }

    [Fact]
    public async Task Refresh_ReusingRevokedToken_RevokesWholeChain()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair initial = await _client.LoginWithTokensAsync(user.Email);
        TokenPair rotated = await _client.RefreshTokensAsync(initial.RefreshToken);

        using HttpResponseMessage reuse = await _client.RefreshAsync(initial.RefreshToken);
        using HttpResponseMessage cascade = await _client.RefreshAsync(rotated.RefreshToken);

        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        cascade.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_PersistsTheRefreshTokenOnlyAsSha256Hash()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);

        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        List<string> storedHashes = await context.RefreshTokens
            .Select(token => token.TokenHash)
            .ToListAsync();

        storedHashes.Should().NotContain(tokens.RefreshToken);
        storedHashes.Should().Contain(Sha256Hex(tokens.RefreshToken));
    }

    private static string Sha256Hex(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
