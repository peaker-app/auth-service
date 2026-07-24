using System.Net;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class RegisterEndpointTests(AuthServiceApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithNewCredentials_ReturnsCreated()
    {
        using HttpResponseMessage response = await _client.RegisterAsync(ApiTestHelpers.NewUser());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        RegisteredUser existing = await _client.RegisterUserAsync();

        using HttpResponseMessage response = await _client.RegisterAsync(
            existing with { Username = ApiTestHelpers.UniqueUsername() });

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
