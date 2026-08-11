using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class EmailConfirmationEndpointTests(AuthServiceApiFactory factory)
{
    private readonly AuthServiceApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_SendsAConfirmationEmailToTheAccountAddress()
    {
        RegisteredUser user = await _client.RegisterUserAsync();

        string? token = await _factory.ConfirmationEmails.WaitForTokenAsync(user.Email);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ConfirmEmail_WithTheEmailedToken_ConfirmsTheAccount()
    {
        ConfirmableAccount account = await RegisterConfirmableAsync();

        using HttpResponseMessage response = await _client.ConfirmEmailAsync(account.Token);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _factory.IsEmailConfirmedAsync(account.UserId)).Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmEmail_PublishesUserEmailConfirmed_ViaOutbox()
    {
        ConfirmableAccount account = await RegisterConfirmableAsync();

        using HttpResponseMessage response = await _client.ConfirmEmailAsync(account.Token);
        response.EnsureSuccessStatusCode();

        bool published = await _factory.WaitForOutboxProcessedAsync(account.UserId, "UserEmailConfirmedDomainEvent");

        published.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmEmail_WithAnAlreadyUsedToken_ReturnsBadRequest()
    {
        ConfirmableAccount account = await RegisterConfirmableAsync();
        using HttpResponseMessage first = await _client.ConfirmEmailAsync(account.Token);
        first.EnsureSuccessStatusCode();

        using HttpResponseMessage response = await _client.ConfirmEmailAsync(account.Token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmEmail_WithAnExpiredToken_ReturnsBadRequest()
    {
        ConfirmableAccount account = await RegisterConfirmableAsync();
        await _factory.ExpireConfirmationTokensAsync(account.UserId);

        using HttpResponseMessage response = await _client.ConfirmEmailAsync(account.Token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmEmail_WithAnUnknownToken_ReturnsBadRequest()
    {
        using HttpResponseMessage response = await _client.ConfirmEmailAsync("not-a-real-token");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmEmail_WithAnEmptyToken_ReturnsBadRequest()
    {
        using HttpResponseMessage response = await _client.ConfirmEmailAsync(string.Empty);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithAnUnconfirmedAccount_StillSucceeds()
    {
        RegisteredUser user = await _client.RegisterUserAsync();

        using HttpResponseMessage response = await _client.LoginAsync(user.Email, ApiTestHelpers.DefaultPassword);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResendConfirmation_WithinTheCooldown_ReturnsTooManyRequests()
    {
        ConfirmableAccount account = await RegisterConfirmableAsync();
        TokenPair tokens = await _client.LoginWithTokensAsync(account.Email);

        using HttpResponseMessage response = await _client.ResendConfirmationAsync(tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ResendConfirmation_WhenTheEmailIsAlreadyConfirmed_ReturnsConflict()
    {
        ConfirmableAccount account = await RegisterConfirmableAsync();
        using HttpResponseMessage confirmation = await _client.ConfirmEmailAsync(account.Token);
        confirmation.EnsureSuccessStatusCode();
        TokenPair tokens = await _client.LoginWithTokensAsync(account.Email);

        using HttpResponseMessage response = await _client.ResendConfirmationAsync(tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ResendConfirmation_WithoutAccessToken_ReturnsUnauthorized()
    {
        using HttpResponseMessage response = await _client.ResendConfirmationAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<ConfirmableAccount> RegisterConfirmableAsync()
    {
        RegisteredUser user = ApiTestHelpers.NewUser();
        using HttpResponseMessage registration = await _client.RegisterAsync(user);
        registration.EnsureSuccessStatusCode();

        Guid userId = await _factory.FindUserIdByEmailAsync(user.Email);
        string? token = await _factory.ConfirmationEmails.WaitForTokenAsync(user.Email);

        token.Should().NotBeNull();

        return new ConfirmableAccount(userId, user.Email, token!);
    }

    private sealed record ConfirmableAccount(Guid UserId, string Email, string Token);
}
