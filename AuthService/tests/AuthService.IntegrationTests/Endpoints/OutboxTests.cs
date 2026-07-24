using System.Net.Http.Json;
using AuthService.Infrastructure.Persistence;
using Common.Infrastructure.Persistence.Outbox;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        bool processed = await WaitForOutboxProcessedAsync(created.Id);

        processed.Should().BeTrue();
    }

    private async Task<bool> WaitForOutboxProcessedAsync(Guid userId)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
            AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            bool processed = await context.Set<OutboxMessage>()
                .AnyAsync(message => message.ProcessedAtUtc != null && message.Content.Contains(userId.ToString()));

            if (processed)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return false;
    }
}
