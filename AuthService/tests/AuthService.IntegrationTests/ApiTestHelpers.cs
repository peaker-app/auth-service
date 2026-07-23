using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AuthService.IntegrationTests;

internal sealed record TokenPair(string AccessToken, string RefreshToken, int ExpiresInSeconds, string TokenType);

internal sealed record RegisterResult(Guid Id);

internal static class ApiTestHelpers
{
    public const string DefaultPassword = "correct-horse-battery-staple";

    public static string UniqueEmail() => $"user-{Guid.CreateVersion7():N}@peaker.io";

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email, string? password = null) =>
        client.PostAsJsonAsync("/api/auth/register", new { email, password = password ?? DefaultPassword });

    public static async Task<string> RegisterUserAsync(this HttpClient client)
    {
        string email = UniqueEmail();
        using HttpResponseMessage response = await client.RegisterAsync(email);
        response.EnsureSuccessStatusCode();

        return email;
    }

    public static async Task<TokenPair> LoginAsync(this HttpClient client, string email, string? password = null)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = password ?? DefaultPassword });
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
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
        {
            Content = JsonContent.Create(new { refreshToken })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await client.SendAsync(request);
    }

    public static async Task FailLoginsAsync(this HttpClient client, string email, int count)
    {
        for (int attempt = 0; attempt < count; attempt++)
        {
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                "/api/auth/login", new { email, password = "definitely-the-wrong-password" });
        }
    }
}
