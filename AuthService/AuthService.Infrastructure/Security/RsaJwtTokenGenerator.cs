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

        string token = TokenHandler.CreateToken(CreateDescriptor(user, issuedAt, expiresAt));

        return new GeneratedAccessToken(token, expiresAt, (int)settings.AccessTokenLifetime.TotalSeconds);
    }

    private SecurityTokenDescriptor CreateDescriptor(User user, DateTime issuedAt, DateTime expiresAt)
    {
        AuthTokenOptions settings = options.Value;

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = settings.Issuer,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expiresAt,
            Claims = BuildClaims(user),
            SigningCredentials = signingKeyProvider.CreateSigningCredentials()
        };

        foreach (string audience in settings.Audiences)
        {
            descriptor.Audiences.Add(audience);
        }

        return descriptor;
    }

    private static Dictionary<string, object> BuildClaims(User user) => new()
    {
        [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
        [JwtRegisteredClaimNames.Email] = user.Email.Value,
        [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString(),
        [PeakerRoles.ClaimType] = user.Roles.Select(role => role.ToString()).ToArray()
    };
}
