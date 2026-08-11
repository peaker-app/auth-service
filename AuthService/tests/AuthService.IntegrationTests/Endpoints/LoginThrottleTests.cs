using System.Net;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class LoginThrottleTests(AuthServiceApiFactory factory)
{
    private const int FreeAttempts = 5;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_AfterExhaustingTheFreeAttempts_ReturnsTooManyRequests()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        await _client.FailLoginsAsync(user.Username, FreeAttempts);

        using HttpResponseMessage response = await _client.LoginAsync(user.Username, "still-wrong");

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Login_WhenThrottled_RejectsEvenTheCorrectPassword()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        await _client.FailLoginsAsync(user.Username, FreeAttempts);

        using HttpResponseMessage response = await _client.LoginAsync(
            user.Username, ApiTestHelpers.DefaultPassword);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Login_ThrottlingAnAccount_NeverLocksIt()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        Guid userId = await factory.FindUserIdByEmailAsync(user.Email);

        await _client.FailLoginsAsync(user.Username, FreeAttempts);

        (await factory.GetStatusAsync(userId)).Should().Be("Active");
    }

    [Fact]
    public async Task Login_AnUnknownIdentifier_IsThrottledExactlyLikeAKnownOne()
    {
        RegisteredUser known = await _client.RegisterUserAsync();
        string unknown = ApiTestHelpers.UniqueUsername();

        await _client.FailLoginsAsync(known.Username, FreeAttempts);
        await _client.FailLoginsAsync(unknown, FreeAttempts);

        using HttpResponseMessage forKnown = await _client.LoginAsync(known.Username, "wrong");
        using HttpResponseMessage forUnknown = await _client.LoginAsync(unknown, "wrong");

        forUnknown.StatusCode.Should().Be(forKnown.StatusCode);
        forKnown.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Login_AfterASuccessfulSignIn_ClearsTheCounter()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        await _client.FailLoginsAsync(user.Username, FreeAttempts - 1);

        using HttpResponseMessage success = await _client.LoginAsync(
            user.Username, ApiTestHelpers.DefaultPassword);
        success.StatusCode.Should().Be(HttpStatusCode.OK);

        await _client.FailLoginsAsync(user.Username, FreeAttempts - 1);

        using HttpResponseMessage stillAllowed = await _client.LoginAsync(
            user.Username, ApiTestHelpers.DefaultPassword);
        stillAllowed.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reset_ClearsTheThrottleSoTheOwnerCanSignInImmediately()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        await _client.FailLoginsAsync(user.Username, FreeAttempts);
        await _client.ForgotPasswordAsync(user.Email);
        string token = (await factory.PasswordResetEmails.WaitForTokenAsync(user.Email))!;

        using HttpResponseMessage reset = await _client.ResetPasswordAsync(token, "a-fresh-summit-password");
        reset.EnsureSuccessStatusCode();

        using HttpResponseMessage login = await _client.LoginAsync(user.Username, "a-fresh-summit-password");
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
