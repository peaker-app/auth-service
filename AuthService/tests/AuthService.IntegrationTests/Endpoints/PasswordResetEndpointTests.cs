using System.Net;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class PasswordResetEndpointTests(AuthServiceApiFactory factory)
{
    private const string NewPassword = "brand-new-mountain-password";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Forgot_WithAnUnknownEmail_AnswersExactlyLikeAKnownOne()
    {
        RegisteredUser known = await _client.RegisterUserAsync();

        using HttpResponseMessage forKnown = await _client.ForgotPasswordAsync(known.Email);
        using HttpResponseMessage forUnknown = await _client.ForgotPasswordAsync(ApiTestHelpers.UniqueEmail());

        forKnown.StatusCode.Should().Be(HttpStatusCode.Accepted);
        forUnknown.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await forKnown.Content.ReadAsStringAsync()).Should().Be(await forUnknown.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Forgot_WithAMalformedEmail_ReturnsBadRequest()
    {
        using HttpResponseMessage response = await _client.ForgotPasswordAsync("not-an-email");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Forgot_WithAnUnknownEmail_SendsNoMail()
    {
        string unknown = ApiTestHelpers.UniqueEmail();

        using HttpResponseMessage response = await _client.ForgotPasswordAsync(unknown);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        factory.PasswordResetEmails.TokenFor(unknown).Should().BeNull();
    }

    [Fact]
    public async Task Reset_WithTheEmailedToken_ChangesThePassword()
    {
        RegisteredUser user = await RequestResetAsync();
        string token = (await factory.PasswordResetEmails.WaitForTokenAsync(user.Email))!;

        using HttpResponseMessage response = await _client.ResetPasswordAsync(token, NewPassword);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using HttpResponseMessage login = await _client.LoginAsync(user.Email, NewPassword);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reset_MakesTheOldPasswordStopWorking()
    {
        RegisteredUser user = await RequestResetAsync();
        string token = (await factory.PasswordResetEmails.WaitForTokenAsync(user.Email))!;
        await _client.ResetPasswordAsync(token, NewPassword);

        using HttpResponseMessage login = await _client.LoginAsync(user.Email, ApiTestHelpers.DefaultPassword);

        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reset_RevokesTheRefreshTokensIssuedBefore()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        TokenPair before = await _client.LoginWithTokensAsync(user.Email);
        await _client.ForgotPasswordAsync(user.Email);
        string token = (await factory.PasswordResetEmails.WaitForTokenAsync(user.Email))!;
        await _client.ResetPasswordAsync(token, NewPassword);

        using HttpResponseMessage refresh = await _client.RefreshAsync(before.RefreshToken);

        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reset_WithTheSameTokenTwice_IsRejected()
    {
        RegisteredUser user = await RequestResetAsync();
        string token = (await factory.PasswordResetEmails.WaitForTokenAsync(user.Email))!;
        await _client.ResetPasswordAsync(token, NewPassword);

        using HttpResponseMessage second = await _client.ResetPasswordAsync(token, "yet-another-password");

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reset_WithAnUnknownToken_ReturnsBadRequest()
    {
        using HttpResponseMessage response = await _client.ResetPasswordAsync("not-a-real-token", NewPassword);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reset_WithATooShortPassword_ReturnsBadRequest()
    {
        RegisteredUser user = await RequestResetAsync();
        string token = (await factory.PasswordResetEmails.WaitForTokenAsync(user.Email))!;

        using HttpResponseMessage response = await _client.ResetPasswordAsync(token, "short");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<RegisteredUser> RequestResetAsync()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        using HttpResponseMessage response = await _client.ForgotPasswordAsync(user.Email);
        response.EnsureSuccessStatusCode();

        return user;
    }
}
