using AuthService.Application.Abstractions;
using AuthService.Domain.EmailConfirmations;
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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddSecurity(configuration);
        services.AddBreachedPasswordChecker();
        services.AddConfirmationEmailSender(configuration);
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
        services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
        services.AddDomainEventHandlers();
    }

    private static void AddDomainEventHandlers(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventHandler<UserRegisteredDomainEvent>, UserRegisteredDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<UserDeletedDomainEvent>, UserDeletedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<UserEmailConfirmedDomainEvent>, UserEmailConfirmedDomainEventHandler>();
        services.AddScoped<
            IDomainEventHandler<EmailConfirmationRequestedDomainEvent>,
            EmailConfirmationRequestedDomainEventHandler>();
    }

    private static void AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthTokenOptions(configuration);
        services.AddSingleton<SigningKeyProvider>();
        services.AddSingleton<ITokenMetadataProvider, TokenMetadataProvider>();
        services.AddScoped<IPasswordHasher, Argon2IdPasswordHasher>();
        services.AddScoped<IAccessTokenGenerator, RsaJwtTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IEmailConfirmationTokenGenerator, EmailConfirmationTokenGenerator>();
    }

    private static void AddAuthTokenOptions(this IServiceCollection services, IConfiguration configuration) =>
        services.AddOptions<AuthTokenOptions>()
            .Bind(configuration.GetSection(AuthTokenOptions.SectionName))
            .Validate<IHostEnvironment>(
                (options, environment) =>
                    environment.IsDevelopment() || !string.IsNullOrWhiteSpace(options.PrivateKeyPem),
                $"'{AuthTokenOptions.SectionName}:{nameof(AuthTokenOptions.PrivateKeyPem)}' es obligatorio fuera de " +
                "Development: sin él cada réplica firmaría con una clave distinta y efímera.")
            .ValidateOnStart();

    private static void AddConfirmationEmailSender(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEmailConfirmationOptions(configuration);

        string? apiKey = configuration
            .GetSection(EmailConfirmationOptions.SectionName)
            .GetValue<string>(nameof(EmailConfirmationOptions.ApiKey));

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddScoped<IConfirmationEmailSender, LoggingConfirmationEmailSender>();
            return;
        }

        services.AddHttpClient<IConfirmationEmailSender, ResendConfirmationEmailSender>(ConfigureResendClient)
            .AddStandardResilienceHandler();
    }

    private static void AddEmailConfirmationOptions(this IServiceCollection services, IConfiguration configuration) =>
        services.AddOptions<EmailConfirmationOptions>()
            .Bind(configuration.GetSection(EmailConfirmationOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate<IHostEnvironment>(
                (options, environment) => environment.IsDevelopment() || options.HasApiKey,
                $"'{EmailConfirmationOptions.SectionName}:{nameof(EmailConfirmationOptions.ApiKey)}' es obligatorio " +
                "fuera de Development: sin él no se envía el correo de confirmación de RF-AUT-02.")
            .ValidateOnStart();

    private static void ConfigureResendClient(IServiceProvider provider, HttpClient client)
    {
        EmailConfirmationOptions options = provider.GetRequiredService<IOptions<EmailConfirmationOptions>>().Value;

        client.BaseAddress = new Uri("https://api.resend.com/");
        client.Timeout = options.RequestTimeout;
    }

    private static void AddBreachedPasswordChecker(this IServiceCollection services) =>
        services.AddHttpClient<IBreachedPasswordChecker, HibpBreachedPasswordChecker>(client =>
            {
                client.BaseAddress = new Uri("https://api.pwnedpasswords.com/");
                client.Timeout = TimeSpan.FromSeconds(5);
            })
            .AddStandardResilienceHandler();
}
