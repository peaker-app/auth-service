using System.Net;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class EmailQuotaTests(AuthServiceApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Resend_BeyondTheRecipientQuota_ReturnsTooManyRequests()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        await factory.ConfirmationEmails.WaitForTokenAsync(user.Email);
        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);

        await ExhaustRecipientQuotaAsync(user.Email);

        using HttpResponseMessage response = await _client.ResendConfirmationAsync(tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Register_WhenTheRecipientQuotaIsExhausted_StillReturnsAccepted()
    {
        RegisteredUser user = ApiTestHelpers.NewUser();
        await ExhaustRecipientQuotaAsync(user.Email);

        using HttpResponseMessage response = await _client.RegisterAsync(user);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Register_WhenTheRecipientQuotaIsExhausted_DropsTheConfirmationEmail()
    {
        RegisteredUser user = ApiTestHelpers.NewUser();
        await ExhaustRecipientQuotaAsync(user.Email);

        using HttpResponseMessage response = await _client.RegisterAsync(user);
        await Task.Delay(TimeSpan.FromSeconds(3));

        factory.ConfirmationEmails.TokenFor(user.Email).Should().BeNull();
    }

    private Task ExhaustRecipientQuotaAsync(string email) =>
        factory.SeedEmailDispatchesAsync(email, count: 5);
}
