using AuthService.Infrastructure.Security;
using Common.API.Security;
using Common.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.API.Extensions;

internal static class AuthenticationExtensions
{
    public static IServiceCollection AddLocalJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<SigningKeyProvider, IOptions<AuthTokenOptions>>(ConfigureJwtBearer);

        services.AddCommonAuthorization();

        return services;
    }

    private static void ConfigureJwtBearer(
        JwtBearerOptions options,
        SigningKeyProvider signingKeyProvider,
        IOptions<AuthTokenOptions> tokenOptions)
    {
        AuthTokenOptions settings = tokenOptions.Value;

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKeyProvider.CreatePublicSecurityKey(),
            NameClaimType = "sub",
            RoleClaimType = PeakerRoles.ClaimType,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }
}
