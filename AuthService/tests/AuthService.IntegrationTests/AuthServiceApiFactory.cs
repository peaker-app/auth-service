using System.Globalization;
using AuthService.Application.Abstractions;
using AuthService.Infrastructure.Persistence;
using AuthService.IntegrationTests.Fakes;
using Common.Infrastructure.Persistence.Outbox;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MySql;
using Testcontainers.RabbitMq;
using Xunit;

namespace AuthService.IntegrationTests;

public sealed class AuthServiceApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MySqlContainer _mySql = new MySqlBuilder("mysql:8.4")
        .WithDatabase("peaker_auth")
        .WithUsername("peaker")
        .WithPassword("peaker")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management-alpine").Build();

    internal RecordingConfirmationEmailSender ConfirmationEmails { get; } = new();

    public async Task ExpireConfirmationTokensAsync(Guid userId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        await context.EmailConfirmationTokens
            .Where(token => token.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                token => token.ExpiresAtUtc, DateTime.UtcNow.AddDays(-1)));
    }

    public async Task<bool> IsEmailConfirmedAsync(Guid userId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        return await context.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.EmailConfirmed)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> WaitForOutboxProcessedAsync(Guid subjectId, string eventTypeName)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            if (await IsOutboxProcessedAsync(subjectId, eventTypeName))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return false;
    }

    private async Task<bool> IsOutboxProcessedAsync(Guid subjectId, string eventTypeName)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        return await context.Set<OutboxMessage>()
            .AnyAsync(message =>
                message.ProcessedAtUtc != null &&
                message.Type.Contains(eventTypeName) &&
                message.Content.Contains(subjectId.ToString()));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(BuildSettings()));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBreachedPasswordChecker>();
            services.AddSingleton<IBreachedPasswordChecker, NeverBreachedPasswordChecker>();

            services.RemoveAll<IConfirmationEmailSender>();
            services.AddSingleton<IConfirmationEmailSender>(ConfirmationEmails);
        });
    }

    private Dictionary<string, string?> BuildSettings()
    {
        var rabbitUri = new Uri(_rabbitMq.GetConnectionString());
        string[] credentials = rabbitUri.UserInfo.Split(':');

        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:AuthDatabase"] = _mySql.GetConnectionString(),
            ["Messaging:Host"] = rabbitUri.Host,
            ["Messaging:Port"] = rabbitUri.Port.ToString(CultureInfo.InvariantCulture),
            ["Messaging:Username"] = credentials[0],
            ["Messaging:Password"] = credentials[1],
            ["Messaging:VirtualHost"] = "/",
            ["Outbox:PollingInterval"] = "00:00:01"
        };
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _mySql.StartAsync();
        await _rabbitMq.StartAsync();

        using IServiceScope scope = Services.CreateScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _mySql.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await base.DisposeAsync();
    }
}
