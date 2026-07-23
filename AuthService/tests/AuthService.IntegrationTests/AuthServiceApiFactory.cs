using System.Globalization;
using AuthService.Application.Abstractions;
using AuthService.IntegrationTests.Fakes;
using AuthService.Infrastructure.Persistence;
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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(BuildSettings()));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBreachedPasswordChecker>();
            services.AddSingleton<IBreachedPasswordChecker, NeverBreachedPasswordChecker>();
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
