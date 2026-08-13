using AuthService.Application.Abstractions;
using AuthService.Domain.EmailConfirmations;
using AuthService.Domain.PasswordResets;
using AuthService.Domain.RefreshTokens;
using AuthService.Domain.Users;
using AuthService.Domain.Users.Events;
using AuthService.Infrastructure.ExternalServices;
using AuthService.Infrastructure.Maintenance;
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
        services.AddBreachedPasswordChecker(configuration);
        services.AddConfirmationEmailSender(configuration);
        services.AddLoginThrottle(configuration);
        services.AddTermsPolicy(configuration);
        services.AddDataRetention(configuration);
        services.AddEventBus(configuration);

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddCommonOutbox<AuthDbContext>(configuration);

        services.AddAuthDbContext();
        services.AddRepositories();
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
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
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
        services.AddScoped<
            IDomainEventHandler<DuplicateRegistrationAttemptedDomainEvent>,
            DuplicateRegistrationAttemptedDomainEventHandler>();
        services.AddScoped<
            IDomainEventHandler<PasswordResetRequestedDomainEvent>,
            PasswordResetRequestedDomainEventHandler>();
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
        services.AddScoped<IPasswordResetTokenGenerator, PasswordResetTokenGenerator>();
        services.AddSingleton<IEmailPseudonymizer, EmailPseudonymizer>();
    }

    private static void AddAuthTokenOptions(this IServiceCollection services, IConfiguration configuration) =>
        services.AddOptions<AuthTokenOptions>()
            .Bind(configuration.GetSection(AuthTokenOptions.SectionName))
            .Validate<IHostEnvironment>(
                (options, environment) =>
                    environment.IsDevelopment() || !string.IsNullOrWhiteSpace(options.PrivateKeyPem),
                $"'{AuthTokenOptions.SectionName}:{nameof(AuthTokenOptions.PrivateKeyPem)}' es obligatorio fuera de " +
                "Development: sin él cada réplica firmaría con una clave distinta y efímera.")
            .Validate(
                options => options.Audiences.Contains(options.SelfAudience, StringComparer.Ordinal),
                $"'{AuthTokenOptions.SectionName}:{nameof(AuthTokenOptions.SelfAudience)}' debe ser una de las " +
                $"audiencias de '{AuthTokenOptions.SectionName}:{nameof(AuthTokenOptions.Audiences)}': si no, " +
                "auth-service rechazaría los tokens que él mismo emite.")
            .ValidateOnStart();

    private static void AddConfirmationEmailSender(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEmailConfirmationOptions(configuration);
        services.AddSmtpOptions(configuration);
        services.Configure<EmailQuotaOptions>(configuration.GetSection(EmailQuotaOptions.SectionName));

        services.AddScoped<SmtpMailer>();
        services.AddScoped<IConfirmationEmailSender, SmtpConfirmationEmailSender>();
        services.AddScoped<IExistingAccountEmailSender, SmtpExistingAccountEmailSender>();
        services.AddScoped<IPasswordResetEmailSender, SmtpPasswordResetEmailSender>();
        services.AddScoped<IEmailQuota, DatabaseEmailQuota>();
    }

    private static void AddTermsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TermsOptions>()
            .Bind(configuration.GetSection(TermsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ITermsPolicy>(provider =>
            provider.GetRequiredService<IOptions<TermsOptions>>().Value);
    }

    private static void AddDataRetention(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DataRetentionOptions>(configuration.GetSection(DataRetentionOptions.SectionName));
        services.AddScoped<IExpiredDataPurger, DatabaseExpiredDataPurger>();
        services.AddHostedService<ExpiredDataSweeper>();
    }

    private static void AddLoginThrottle(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LoginThrottleOptions>()
            .Bind(configuration.GetSection(LoginThrottleOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<ILoginThrottle, DatabaseLoginThrottle>();
    }

    private static void AddEmailConfirmationOptions(this IServiceCollection services, IConfiguration configuration) =>
        services.AddOptions<EmailConfirmationOptions>()
            .Bind(configuration.GetSection(EmailConfirmationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

    private static void AddSmtpOptions(this IServiceCollection services, IConfiguration configuration) =>
        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate<IHostEnvironment>(
                (options, environment) => environment.IsDevelopment() || options.IsEncrypted,
                $"'{SmtpOptions.SectionName}:{nameof(SmtpOptions.Security)}' debe ser StartTls o SslOnConnect fuera " +
                "de Development: el correo de confirmación de RF-AUT-02 no puede viajar en claro.")
            .Validate<IHostEnvironment>(
                (options, environment) => environment.IsDevelopment() || options.HasCredentials,
                $"'{SmtpOptions.SectionName}:{nameof(SmtpOptions.Username)}' y " +
                $"'{SmtpOptions.SectionName}:{nameof(SmtpOptions.Password)}' son obligatorios fuera de Development: " +
                "el relay SMTP rechaza los envíos sin autenticar.")
            .ValidateOnStart();

    private static void AddBreachedPasswordChecker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BreachedPasswordOptions>()
            .Bind(configuration.GetSection(BreachedPasswordOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IBreachedPasswordChecker, HibpBreachedPasswordChecker>(ConfigureBreachedPasswordClient)
            .AddStandardResilienceHandler();
    }

    private static void ConfigureBreachedPasswordClient(IServiceProvider provider, HttpClient client)
    {
        BreachedPasswordOptions options = provider.GetRequiredService<IOptions<BreachedPasswordOptions>>().Value;

        client.BaseAddress = options.BaseAddress;
        client.Timeout = options.RequestTimeout;
    }
}
