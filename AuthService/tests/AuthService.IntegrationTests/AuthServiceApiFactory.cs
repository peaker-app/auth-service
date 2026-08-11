using System.Globalization;
using AuthService.Application.Abstractions;
using AuthService.Domain.Users;
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

    internal RecordingExistingAccountEmailSender ExistingAccountEmailSender { get; } = new();

    internal RecordingPasswordResetEmailSender PasswordResetEmails { get; } = new();

    public async Task<int> CountUsersByEmailAsync(string email)
    {
        Email parsed = Email.Create(email).Value;

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        return await context.Users.AsNoTracking().CountAsync(user => user.Email == parsed);
    }

    public async Task<Guid> FindUserIdByEmailAsync(string email)
    {
        Email parsed = Email.Create(email).Value;

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        return await context.Users
            .AsNoTracking()
            .Where(user => user.Email == parsed)
            .Select(user => user.Id)
            .FirstAsync();
    }

    public async Task<bool> IsEmailPseudonymizedAsync(Guid userId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        User user = await context.Users.AsNoTracking().SingleAsync(candidate => candidate.Id == userId);

        return user.Email.Value.StartsWith("deleted+", StringComparison.Ordinal) &&
               user.Email.Value.EndsWith("@peaker.invalid", StringComparison.Ordinal);
    }

    public async Task GrantAdminAsync(Guid userId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        User user = await context.Users.SingleAsync(candidate => candidate.Id == userId);
        user.Grant(UserRole.Admin);

        await context.SaveChangesAsync();
    }

    public async Task LockAsync(Guid userId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        User user = await context.Users.SingleAsync(candidate => candidate.Id == userId);
        user.Lock();

        await context.SaveChangesAsync();
    }

    public async Task<bool> IsInRoleAsync(Guid userId, UserRole role)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        User? user = await context.Users.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == userId);

        return user is not null && user.IsInRole(role);
    }

    public async Task<string> GetStatusAsync(Guid userId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        return await context.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.Status.ToString())
            .FirstAsync();
    }

    public async Task<(string Version, DateTime AcceptedAtUtc)> GetTermsAcceptanceAsync(Guid userId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        User user = await context.Users.AsNoTracking().SingleAsync(candidate => candidate.Id == userId);

        return (user.AcceptedTerms.Version, user.AcceptedTerms.AcceptedAtUtc);
    }

    public async Task<int> CountLoginAttemptsAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        return await context.Set<LoginAttempt>().CountAsync();
    }

    public async Task SeedEmailDispatchesAsync(string email, int count)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AuthDbContext context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        string recipientHash = SubjectHash.Of(email);

        for (int index = 0; index < count; index++)
        {
            context.Set<EmailDispatch>().Add(new EmailDispatch
            {
                Id = Guid.CreateVersion7(),
                RecipientHash = recipientHash,
                SentAtUtc = DateTime.UtcNow.AddMinutes(-index)
            });
        }

        await context.SaveChangesAsync();
    }

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

            services.RemoveAll<IExistingAccountEmailSender>();
            services.AddSingleton<IExistingAccountEmailSender>(ExistingAccountEmailSender);

            services.RemoveAll<IPasswordResetEmailSender>();
            services.AddSingleton<IPasswordResetEmailSender>(PasswordResetEmails);
        });
    }

    private Dictionary<string, string?> BuildSettings()
    {
        Uri rabbitUri = new(_rabbitMq.GetConnectionString());
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
