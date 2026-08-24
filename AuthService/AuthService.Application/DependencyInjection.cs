using AuthService.Application.Authentication;
using AuthService.Application.EmailConfirmations;
using AuthService.Application.PasswordResets;
using Common.Application.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IAuthTokenIssuer, AuthTokenIssuer>();
        services.AddScoped<IEmailConfirmationIssuer, EmailConfirmationIssuer>();
        services.AddScoped<IPasswordResetIssuer, PasswordResetIssuer>();
        services.AddScoped<IPasswordResetTokenRedeemer, PasswordResetTokenRedeemer>();
        services.AddScoped<ISessionRevoker, SessionRevoker>();

        return services;
    }
}
