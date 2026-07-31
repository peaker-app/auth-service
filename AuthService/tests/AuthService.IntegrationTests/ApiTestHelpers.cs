using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AuthService.IntegrationTests;

internal sealed record TokenPair(string AccessToken, string RefreshToken, int ExpiresInSeconds, string TokenType);

internal sealed record RegisterResult(Guid Id);

internal sealed record RegisteredUser(string Email, string Username);

internal static class ApiTestHelpers
{
    public const string DefaultPassword = "correct-horse-battery-staple";

    public static string UniqueEmail() => $"user-{Guid.CreateVersion7():N}@peaker.io";

    public static string UniqueUsername() => $"hiker{Guid.CreateVersion7():N}"[..24];

    public static Task<HttpResponseMessage> RegisterAsync(
        this HttpClient client,
        RegisteredUser user,
        string? password = null) =>
        client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = user.Email, username = user.Username, password = password ?? DefaultPassword });

    public static RegisteredUser NewUser() => new(UniqueEmail(), UniqueUsername());

    public static async Task<RegisteredUser> RegisterUserAsync(this HttpClient client)
    {
        RegisteredUser user = NewUser();
        using HttpResponseMessage response = await client.RegisterAsync(user);
        response.EnsureSuccessStatusCode();

        return user;
    }

    public static Task<HttpResponseMessage> LoginAsync(
        this HttpClient client,
        string identifier,
        string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { identifier, password });

    public static async Task<TokenPair> LoginWithTokensAsync(this HttpClient client, string identifier)
    {
        using HttpResponseMessage response = await client.LoginAsync(identifier, DefaultPassword);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<TokenPair>())!;
    }

    public static Task<HttpResponseMessage> RefreshAsync(this HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

    public static async Task<TokenPair> RefreshTokensAsync(this HttpClient client, string refreshToken)
    {
        using HttpResponseMessage response = await client.RefreshAsync(refreshToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<TokenPair>())!;
    }

    public static async Task<HttpResponseMessage> LogoutAsync(
        this HttpClient client,
        string accessToken,
        string refreshToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/auth/logout")
        {
            Content = JsonContent.Create(new { refreshToken })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> ConfirmEmailAsync(this HttpClient client, string token) =>
        client.PostAsJsonAsync("/api/auth/email/confirm", new { token });

    public static async Task<HttpResponseMessage> ResendConfirmationAsync(
        this HttpClient client,
        string? accessToken = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/auth/email/resend");

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> DeleteAccountAsync(this HttpClient client, string accessToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Delete, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await client.SendAsync(request);
    }

    public static async Task FailLoginsAsync(this HttpClient client, string identifier, int count)
    {
        for (int attempt = 0; attempt < count; attempt++)
        {
            using HttpResponseMessage response = await client.LoginAsync(
                identifier, "definitely-the-wrong-password");
        }
    }
}
