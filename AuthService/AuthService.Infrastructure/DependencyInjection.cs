using AuthService.Application.Abstractions;
using AuthService.Domain.RefreshTokens;
using AuthService.Domain.Users;
using AuthService.Domain.Users.Events;
using AuthService.Infrastructure.ExternalServices;
using AuthService.Infrastructure.Messaging;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Persistence.Repositories;
using AuthService.Infrastructure.Security;
using Common.Application.Abstractions;
using Common.Infrastructure.Messaging;
using Common.Infrastructure.Persistence;
using Common.Infrastructure.Persistence.Outbox;
using Common.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddSecurity(configuration);
        services.AddBreachedPasswordChecker();
        services.AddEventBus(configuration);

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<OutboxInterceptor>();
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

        services.AddAuthDbContext();
        services.AddRepositories();
        services.AddHostedService<OutboxProcessor<AuthDbContext>>();
    }

    private static void AddAuthDbContext(this IServiceCollection services)
    {
        services.AddDbContext<AuthDbContext>((provider, options) => options
            .UseMySQL(ResolveConnectionString(provider))
            .AddInterceptors(
                provider.GetRequiredService<AuditableEntityInterceptor>(),
                provider.GetRequiredService<OutboxInterceptor>()));
    }

    private static string ResolveConnectionString(IServiceProvider provider)
    {
        string? connectionString = provider.GetRequiredService<IConfiguration>()
            .GetConnectionString("AuthDatabase");

        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException("Connection string 'AuthDatabase' is not configured.")
            : connectionString;
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AuthDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IDomainEventHandler<UserRegisteredDomainEvent>, UserRegisteredDomainEventHandler>();
    }

    private static void AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthTokenOptions>(configuration.GetSection(AuthTokenOptions.SectionName));
        services.AddSingleton<SigningKeyProvider>();
        services.AddSingleton<ITokenMetadataProvider, TokenMetadataProvider>();
        services.AddScoped<IPasswordHasher, Argon2IdPasswordHasher>();
        services.AddScoped<IAccessTokenGenerator, RsaJwtTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
    }

    private static void AddBreachedPasswordChecker(this IServiceCollection services) =>
        services.AddHttpClient<IBreachedPasswordChecker, HibpBreachedPasswordChecker>(client =>
            {
                client.BaseAddress = new Uri("https://api.pwnedpasswords.com/");
                client.Timeout = TimeSpan.FromSeconds(5);
            })
            .AddStandardResilienceHandler();
}
