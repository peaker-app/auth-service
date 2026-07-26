using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class OutboxTests(AuthServiceApiFactory factory)
{
    private readonly AuthServiceApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_PublishesUserRegistered_ViaOutbox()
    {
        using HttpResponseMessage response = await _client.RegisterAsync(ApiTestHelpers.NewUser());
        RegisterResult created = (await response.Content.ReadFromJsonAsync<RegisterResult>())!;

        bool processed = await _factory.WaitForOutboxProcessedAsync(created.Id, "UserRegisteredDomainEvent");

        processed.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_PublishesUserDeleted_ViaOutbox()
    {
        RegisteredUser user = ApiTestHelpers.NewUser();
        using HttpResponseMessage registration = await _client.RegisterAsync(user);
        RegisterResult created = (await registration.Content.ReadFromJsonAsync<RegisterResult>())!;

        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);
        using HttpResponseMessage deletion = await _client.DeleteAccountAsync(tokens.AccessToken);
        deletion.EnsureSuccessStatusCode();

        bool processed = await _factory.WaitForOutboxProcessedAsync(created.Id, "UserDeletedDomainEvent");

        processed.Should().BeTrue();
    }
}
