using System.Net;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class RegisterEndpointTests(AuthServiceApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithNewCredentials_ReturnsAccepted()
    {
        using HttpResponseMessage response = await _client.RegisterAsync(ApiTestHelpers.NewUser());

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        RegisteredUser existing = await _client.RegisterUserAsync();

        using HttpResponseMessage duplicate = await _client.RegisterAsync(
            existing with { Username = ApiTestHelpers.UniqueUsername() });

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_NamesTheEmailInTheProblemDetail()
    {
        RegisteredUser existing = await _client.RegisterUserAsync();

        using HttpResponseMessage duplicate = await _client.RegisterAsync(
            existing with { Username = ApiTestHelpers.UniqueUsername() });

        string body = await duplicate.Content.ReadAsStringAsync();
        body.Should().Contain("User.EmailAlreadyRegistered");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_DoesNotCreateASecondAccount()
    {
        RegisteredUser existing = await _client.RegisterUserAsync();

        using HttpResponseMessage duplicate = await _client.RegisterAsync(
            existing with { Username = ApiTestHelpers.UniqueUsername() });

        int accounts = await factory.CountUsersByEmailAsync(existing.Email);
        accounts.Should().Be(1);
    }

    [Fact]
    public async Task Register_WithTheEmailOfADeletedAccount_ReturnsConflict()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);
        (await _client.DeleteAccountAsync(tokens.AccessToken)).EnsureSuccessStatusCode();

        using HttpResponseMessage response = await _client.RegisterAsync(
            new RegisteredUser(user.Email, ApiTestHelpers.UniqueUsername()));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsConflict()
    {
        RegisteredUser existing = await _client.RegisterUserAsync();

        using HttpResponseMessage response = await _client.RegisterAsync(
            existing with { Email = ApiTestHelpers.UniqueEmail() });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithUsernameDifferingOnlyInCasing_ReturnsConflict()
    {
        RegisteredUser existing = await _client.RegisterUserAsync();

        using HttpResponseMessage response = await _client.RegisterAsync(new RegisteredUser(
            ApiTestHelpers.UniqueEmail(),
            existing.Username.ToUpperInvariant()));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidUsername_ReturnsBadRequest()
    {
        using HttpResponseMessage response = await _client.RegisterAsync(
            ApiTestHelpers.NewUser() with { Username = "no" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        using HttpResponseMessage response = await _client.RegisterAsync(ApiTestHelpers.NewUser(), "short");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
