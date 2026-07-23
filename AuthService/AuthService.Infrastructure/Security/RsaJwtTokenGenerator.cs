using AuthService.Application.Abstractions;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security;

internal sealed class RsaJwtTokenGenerator(
    SigningKeyProvider signingKeyProvider,
    IOptions<AuthTokenOptions> options,
    IDateTimeProvider dateTimeProvider) : IAccessTokenGenerator
{
    private static readonly JsonWebTokenHandler TokenHandler = new() { SetDefaultTimesOnTokenCreation = false };

    public GeneratedAccessToken Generate(User user)
    {
        AuthTokenOptions settings = options.Value;
        DateTime issuedAt = dateTimeProvider.UtcNow;
        DateTime expiresAt = issuedAt.Add(settings.AccessTokenLifetime);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expiresAt,
            Claims = BuildClaims(user),
            SigningCredentials = signingKeyProvider.CreateSigningCredentials()
        };

        string token = TokenHandler.CreateToken(descriptor);

        return new GeneratedAccessToken(token, expiresAt, (int)settings.AccessTokenLifetime.TotalSeconds);
    }

    private static Dictionary<string, object> BuildClaims(User user) => new()
    {
        [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
        [JwtRegisteredClaimNames.Email] = user.Email.Value,
        [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString(),
        ["roles"] = Array.Empty<string>()
    };
}
