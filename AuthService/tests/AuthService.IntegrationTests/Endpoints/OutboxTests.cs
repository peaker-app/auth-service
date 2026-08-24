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
        RegisteredUser user = await _client.RegisterUserAsync();
        Guid userId = await _factory.FindUserIdByEmailAsync(user.Email);

        bool processed = await _factory.WaitForOutboxProcessedAsync(userId, "UserRegisteredDomainEvent");

        processed.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_PublishesUserDeleted_ViaOutbox()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        Guid userId = await _factory.FindUserIdByEmailAsync(user.Email);

        TokenPair tokens = await _client.LoginWithTokensAsync(user.Email);
        using HttpResponseMessage deletion = await _client.DeleteAccountAsync(tokens.AccessToken);
        deletion.EnsureSuccessStatusCode();

        bool processed = await _factory.WaitForOutboxProcessedAsync(userId, "UserDeletedDomainEvent");

        processed.Should().BeTrue();
    }
}
