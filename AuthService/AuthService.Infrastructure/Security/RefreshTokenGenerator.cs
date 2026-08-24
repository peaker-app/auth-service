using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security;

internal sealed class RefreshTokenGenerator(IOptions<AuthTokenOptions> options) : IRefreshTokenGenerator
{
    private const int TokenSizeInBytes = 32;

    public GeneratedRefreshToken Generate(DateTime utcNow)
    {
        string rawToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(TokenSizeInBytes));
        DateTime expiresAt = utcNow.Add(options.Value.RefreshTokenLifetime);

        return new GeneratedRefreshToken(rawToken, Hash(rawToken), expiresAt);
    }

    public string Hash(string rawToken)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }
}
