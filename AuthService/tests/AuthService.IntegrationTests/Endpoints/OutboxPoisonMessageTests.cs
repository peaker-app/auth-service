using System.Net;
using Common.Infrastructure.Persistence.Outbox;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class OutboxPoisonMessageTests(AuthServiceApiFactory factory)
{
    private readonly AuthServiceApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Outbox_WithAPoisonedMessageAtTheHead_KeepsPublishingTheMessagesBehindIt()
    {
        await _factory.PoisonTheOutboxAsync();

        RegisteredUser user = await _client.RegisterUserAsync();
        Guid userId = await _factory.FindUserIdByEmailAsync(user.Email);

        bool processed = await _factory.WaitForOutboxProcessedAsync(userId, "UserRegisteredDomainEvent");

        processed.Should().BeTrue();
    }

    [Fact]
    public async Task Outbox_WithAPoisonedMessage_ParksItOnceItRunsOutOfAttempts()
    {
        Guid messageId = await _factory.PoisonTheOutboxAsync();

        OutboxMessage? parked = await _factory.WaitForParkedOutboxMessageAsync(messageId);

        parked.Should().NotBeNull();
        parked!.AttemptCount.Should().Be(2);
        parked.ProcessedAtUtc.Should().BeNull();
        parked.Error.Should().NotBeNullOrWhiteSpace();
        parked.NextAttemptAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ReadinessProbe_WithAParkedOutboxMessage_ReportsDegradedWithoutFailing()
    {
        Guid messageId = await _factory.PoisonTheOutboxAsync();
        await _factory.WaitForParkedOutboxMessageAsync(messageId);

        using HttpResponseMessage response = await _client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Degraded");
    }
}
