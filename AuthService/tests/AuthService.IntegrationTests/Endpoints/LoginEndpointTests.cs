using System.Net;
using Common.Application.Abstractions;
using FluentAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
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
    public async Task Login_AgainstAnAccountLockedByAnAdministrator_ReturnsTheGenericUnauthorized()
    {
        RegisteredUser victim = await _client.RegisterUserAsync();
        Guid victimId = await factory.FindUserIdByEmailAsync(victim.Email);
        await factory.LockAsync(victimId);

        using HttpResponseMessage response = await _client.LoginAsync(
            victim.Email, ApiTestHelpers.DefaultPassword);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownIdentifierOrWrongPassword_AnswersExactlyTheSame()
    {
        RegisteredUser user = await _client.RegisterUserAsync();

        using HttpResponseMessage unknownIdentifier = await _client.LoginAsync(
            ApiTestHelpers.UniqueUsername(), ApiTestHelpers.DefaultPassword);
        using HttpResponseMessage wrongPassword = await _client.LoginAsync(user.Email, "the-wrong-password");

        string unknownBody = await unknownIdentifier.Content.ReadAsStringAsync();
        string wrongPasswordBody = await wrongPassword.Content.ReadAsStringAsync();

        unknownIdentifier.StatusCode.Should().Be(wrongPassword.StatusCode);
        unknownBody.Should().Be(wrongPasswordBody);
    }

    [Fact]
    public async Task Login_AccessToken_CarriesTheAgreedClaims()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);

        JsonWebToken accessToken = new(tokens.AccessToken);

        accessToken.Claims.Select(claim => claim.Type)
            .Should().Contain(["sub", "email", "jti", "iat", "exp", "iss", "aud"]);
    }

    [Fact]
    public async Task Login_AccessTokenOfARegularAccount_CarriesNoRoles()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);

        JsonWebToken accessToken = new(tokens.AccessToken);

        accessToken.Claims.Where(claim => claim.Type == PeakerRoles.ClaimType).Should().BeEmpty();
    }

    [Fact]
    public async Task Login_AccessTokenOfAnAdminAccount_CarriesTheAdminRole()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        await factory.GrantAdminAsync(await factory.FindUserIdByEmailAsync(user.Email));

        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);

        JsonWebToken accessToken = new(tokens.AccessToken);

        accessToken.Claims
            .Where(claim => claim.Type == PeakerRoles.ClaimType)
            .Select(claim => claim.Value)
            .Should().Equal(PeakerRoles.Admin);
    }

    [Fact]
    public async Task Login_AccessToken_CarriesNoProfileData()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);

        JsonWebToken accessToken = new(tokens.AccessToken);

        accessToken.Claims.Select(claim => claim.Type).Should().NotContain("username");
        accessToken.Claims.Select(claim => claim.Value).Should().NotContain(user.Username);
    }

    [Fact]
    public async Task Login_AccessToken_IsSignedWithRs256()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);

        new JsonWebToken(tokens.AccessToken).Alg.Should().Be("RS256");
    }
}
