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
    public async Task Register_WithDuplicateEmail_ReturnsTheSameAcceptedResponseAsANewAccount()
    {
        RegisteredUser existing = await _client.RegisterUserAsync();

        using HttpResponseMessage fresh = await _client.RegisterAsync(ApiTestHelpers.NewUser());
        using HttpResponseMessage duplicate = await _client.RegisterAsync(
            existing with { Username = ApiTestHelpers.UniqueUsername() });

        duplicate.StatusCode.Should().Be(fresh.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsAnEmptyBodyJustLikeANewAccount()
    {
        RegisteredUser existing = await _client.RegisterUserAsync();

        using HttpResponseMessage duplicate = await _client.RegisterAsync(
            existing with { Username = ApiTestHelpers.UniqueUsername() });

        string body = await duplicate.Content.ReadAsStringAsync();
        body.Should().BeEmpty();
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
    public async Task Register_WithDuplicateEmail_SendsTheExistingAccountNoticeInstead()
    {
        RegisteredUser existing = await _client.RegisterUserAsync();
        await factory.ConfirmationEmails.WaitForTokenAsync(existing.Email);
        factory.ExistingAccountEmailSender.Clear();

        using HttpResponseMessage duplicate = await _client.RegisterAsync(
            existing with { Username = ApiTestHelpers.UniqueUsername() });

        bool notified = await factory.ExistingAccountEmailSender.WaitForNoticeAsync(existing.Email);
        notified.Should().BeTrue();
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
